// Poses fitted cloth and its rigid sprit from native deployment and signed wind pressure.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal sealed class LooseFootedSpritsailClothGeometry
    {
        // Base depth relative to the local chord, before the mast-side profile adds fullness.
        internal const float BillowDepth = 0.16f;
        private readonly LooseFootedSpritsailAssetData data;
        private readonly LooseFootedSpritsailPose rest;
        private readonly Vector2[] coordinates;
        private readonly Vector4[] flutterWeights;
        private readonly Vector3 throat,
            tack,
            peak,
            clew,
            peakTie;
        private readonly float footLength,
            rotationScale;
        internal readonly Vector3[] Vertices;
        internal Matrix4x4 Sprit { get; private set; }
        internal Vector3 Pivot { get; }
        internal Vector3 Throat => Vertices[data.Throat];
        internal Vector3 Tack => Vertices[data.Tack];
        internal Vector3 Peak => Vertices[data.Peak];
        internal Vector3 Clew => Vertices[data.Clew];
        internal Vector3 SnotterTie => new Vector3(x: throat.x, y: Pivot.y, z: throat.z);

        internal LooseFootedSpritsailClothGeometry(
            LooseFootedSpritsailAssetData data,
            LooseFootedSpritsailPose rest
        )
        {
            this.data = data;
            this.rest = rest;
            Vertices = new Vector3[data.Sail.Vertices.Length];
            coordinates = new Vector2[Vertices.Length];
            flutterWeights = new Vector4[Vertices.Length];
            // Work in the fitted assembly frame: +Y runs up the mast, -Z runs aft
            // across the sail, and X measures billow out of its neutral plane.
            throat = rest.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[data.Throat]);
            tack = rest.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[data.Tack]);
            peak = rest.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[data.Peak]);
            clew = rest.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[data.Clew]);
            Pivot = rest.Sprit.MultiplyPoint3x4(point: Vector3.zero);
            // Store the peak in sprit-local coordinates so it follows the wooden spar
            // exactly as the spar rotates, including the authored gap from its physical tip.
            peakTie = LooseFootedSpritsailTransform.InversePoint(matrix: rest.Sprit, point: peak);
            footLength = (clew - tack).magnitude;
            // Tall/narrow fittings start closer to the mast than the authored cut.
            // Scale the authored sweep down only when needed to keep the spar aft of it.
            var direction = peak - Pivot;
            double angle = Math.Atan2(-direction.z, direction.y);
            double radius = Math.Sqrt(direction.y * direction.y + direction.z * direction.z);
            double mastAngle = Math.Asin(Math.Min(1, Math.Max(0, (Pivot.z - throat.z) / radius)));
            double available = Math.Max(0, angle - Math.Max(5 * Math.PI / 180, mastAngle));
            var finalRotation = data.ReefPoses[data.ReefPoses.Length - 1].SpritRotation;
            double authored = 2 * Math.Acos(Math.Min(1, Math.Abs(finalRotation.w)));
            rotationScale = authored < 1e-6 ? 1 : (float)Math.Min(1, available / authored);
            // These are cloth-shaping coordinates, not texture UVs: u goes from luff
            // to leech, and v goes from foot to head at that u. Interpolating each row's
            // top and bottom accounts for the trapezoid's sloping head.
            for (int i = 0; i < Vertices.Length; i++)
            {
                var vertex = rest.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[i]);
                float u = Mathf.Clamp01(value: (vertex.z - tack.z) / (clew.z - tack.z));
                float bottom = tack.y + (clew.y - tack.y) * u;
                float top = throat.y + (peak.y - throat.y) * u;
                coordinates[i] = new Vector2(
                    x: u,
                    y: Mathf.Clamp01(value: (vertex.y - bottom) / (top - bottom))
                );
                flutterWeights[i] = LooseFootedSpritsailFlutter.Weights(
                    u: u,
                    v: coordinates[i].y,
                    footLength: footLength
                );
            }
            Pose(deployment: 1, load: 0);
        }

        // Read a visual position without modifying corners, edge budgets or aerodynamic inputs.
        internal Vector3 VisualVertex(int index, Vector4 flutter)
        {
            var point = Vertices[index];
            var weights = flutterWeights[index];
            point.x +=
                weights.x * flutter.x
                + weights.y * flutter.y
                + weights.z * flutter.z
                + weights.w * flutter.w;
            return point;
        }

        internal void Pose(float deployment, float load)
        {
            float set = Mathf.Clamp01(value: deployment);
            float pressure = Math.Max(-1, Math.Min(1, load));
            Samples(deployment: set, out int left, out int right, out float blend);
            Sprit = SpritPose(deployment: set);
            var posedPeak = Sprit.MultiplyPoint3x4(point: peakTie);
            var correction =
                posedPeak - Sample(index: data.Peak, left: left, right: right, blend: blend);
            for (int i = 0; i < Vertices.Length; i++)
            {
                float u = coordinates[i].x;
                float v = coordinates[i].y;
                // Retarget the authored cloth to the fitted rigid spar, without moving the luff.
                var point =
                    Sample(index: i, left: left, right: right, blend: blend) + correction * u * v;
                float across = 4 * u * (1 - u);
                float vertical = 4 * v * (1 - v);
                float envelope = across * (1 + 0.6f * (1 - u)) * (0.55f + 0.45f * vertical);
                float chord = (
                    Vector3.Lerp(a: clew, b: peak, t: v) - Vector3.Lerp(a: tack, b: throat, t: v)
                ).magnitude;
                point.x -= BillowDepth * chord * pressure * set * envelope;
                Vertices[i] = point;
            }
            Vertices[data.Peak] = posedPeak;
        }

        private void Samples(float deployment, out int left, out int right, out float blend)
        {
            float sample = (1 - Mathf.Clamp01(value: deployment)) * (data.ReefPoses.Length - 1);
            left = (int)sample;
            right = Math.Min(left + 1, data.ReefPoses.Length - 1);
            blend = sample - left;
        }

        private Vector3 Sample(int index, int left, int right, float blend) =>
            rest.Sail.MultiplyPoint3x4(
                point: Vector3.Lerp(
                    a: data.ReefPoses[left].Vertices[index],
                    b: data.ReefPoses[right].Vertices[index],
                    t: blend
                )
            );

        internal static float WindLoad(Vector3 wind, Vector3 normal)
        {
            float pressure = Vector3.Dot(lhs: wind, rhs: normal);
            return Math.Abs(pressure) < 0.6f ? 0 : Math.Max(-1, Math.Min(1, pressure / 8));
        }

        // Exponential easing has a 1/3-second time constant. Cap the time step at 0.1 s
        // so a long frame cannot abruptly flip the billow when the wind changes sides.
        internal static float SmoothLoad(float current, float target, float deltaTime) =>
            current
            + (target - current)
                * (1 - (float)Math.Exp(-3 * Math.Max(0, Math.Min(0.1f, deltaTime))));

        // Premultiply the fitted pose by a rotation around its mounting pivot. This
        // changes the spar's angle without changing its fitted length or thickness.
        internal Matrix4x4 SpritPose(float deployment)
        {
            Samples(deployment: deployment, out int left, out int right, out float blend);
            var rotation = LooseFootedSpritsailTransform.InterpolateRotation(
                a: data.ReefPoses[left].SpritRotation,
                b: data.ReefPoses[right].SpritRotation,
                t: blend
            );
            rotation = LooseFootedSpritsailTransform.InterpolateRotation(
                a: Quaternion.identity,
                b: rotation,
                t: rotationScale
            );
            return Matrix4x4.Translate(vector: Pivot)
                * LooseFootedSpritsailTransform.TRS(pos: Vector3.zero, q: rotation, s: Vector3.one)
                * Matrix4x4.Translate(vector: -Pivot)
                * rest.Sprit;
        }

        internal bool CanRoute(Vector3 guide)
        {
            if (guide.y <= Pivot.y)
                return false;
            // Winding shortens the rope: sample the full raising motion and reject
            // guides that would require the guide-to-sprit segment to lengthen anywhere.
            float previous = float.PositiveInfinity;
            for (int i = 0; i <= 32; i++)
            {
                var end = SpritPose(deployment: 1 - i / 32f)
                    .MultiplyPoint3x4(point: data.ReefLineAttachment);
                float length = (guide - end).magnitude;
                if (length > previous + 0.0001f)
                    return false;
                previous = length;
            }
            return true;
        }

        internal LooseFootedSpritsailClearance[] Clearance()
        {
            int strips = LooseFootedSpritsailFittingGeometry.CollisionStrips;
            var bounds = new Bounds[strips + 1];
            var initialized = new bool[bounds.Length];
            // Shipyard fitting uses the deployed neutral sail, like native gaffs.
            // Billow and reefing envelopes create false obstructions at zero trim.
            for (int i = 0; i < data.Sail.Vertices.Length; i++)
            {
                var point = rest.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[i]);
                int column = (int)Math.Round(coordinates[i].x * (strips - 1));
                Include(bounds: bounds, initialized: initialized, index: column, point: point);
                if (column > 0)
                    Include(
                        bounds: bounds,
                        initialized: initialized,
                        index: column - 1,
                        point: point
                    );
            }
            // Bound the sprit in its own axes instead of filling the triangle
            // between the diagonal spar and the mast with an axis-aligned box.
            var scale = LooseFootedSpritsailTransform.Scale(matrix: rest.Sprit);
            foreach (var point in data.Sprit.Vertices)
                Include(
                    bounds: bounds,
                    initialized: initialized,
                    index: strips,
                    point: Vector3.Scale(a: point, b: scale)
                );
            var result = new LooseFootedSpritsailClearance[bounds.Length];
            for (int i = 0; i < bounds.Length; i++)
            {
                bounds[i].Expand(amount: 0.02f);
                var rotation =
                    i == strips
                        ? LooseFootedSpritsailTransform.Rotation(matrix: rest.Sprit)
                        : Quaternion.identity;
                var center = bounds[i].center;
                if (i == strips)
                    center = Pivot + rotation * center;
                else
                {
                    // Do not pad the luff forward into its mounting mast.
                    var maximum = bounds[i].max;
                    maximum.z = Math.Min(maximum.z, throat.z);
                    bounds[i].SetMinMax(min: bounds[i].min, max: maximum);
                    center = bounds[i].center;
                }
                result[i] = new LooseFootedSpritsailClearance
                {
                    Center = center,
                    Size = bounds[i].size,
                    Rotation = rotation,
                };
            }
            return result;
        }

        private static void Include(Bounds[] bounds, bool[] initialized, int index, Vector3 point)
        {
            if (!initialized[index])
            {
                bounds[index] = new Bounds(center: point, size: Vector3.zero);
                initialized[index] = true;
            }
            else
                bounds[index].Encapsulate(point: point);
        }
    }

    internal struct LooseFootedSpritsailClearance
    {
        internal Vector3 Center;
        internal Vector3 Size;
        internal Quaternion Rotation;
    }
}
