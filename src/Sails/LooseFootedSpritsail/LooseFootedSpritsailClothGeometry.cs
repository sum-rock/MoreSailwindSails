// Poses fitted cloth and its rigid sprit from authored deployment poses with attachment correction.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal sealed class LooseFootedSpritsailClothGeometry
    {
        private readonly LooseFootedSpritsailAssetData data;
        private readonly LooseFootedSpritsailPose rest;
        private readonly Vector2[] coordinates;
        private readonly Vector4[] flutterWeights;
        private readonly float[] authoredDepth;
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
        internal bool TopsailVisible { get; private set; }
        internal float AreaFraction { get; private set; } = 1;
        internal Vector3 AreaCenter { get; private set; }
        private float deployedArea;
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
            authoredDepth = new float[Vertices.Length];
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
            Pose(deployment: 1);
            if (data.HasTopsail)
            {
                deployedArea = ProjectedArea(out var center);
                AreaCenter = center;
                AreaFraction = 1;
            }
        }

        // Read a visual position without modifying corners, edge budgets or aerodynamic inputs.
        internal Vector3 VisualVertex(int index, Vector4 flutter, float foldSide = 1)
        {
            var point = Vertices[index];
            // Mirror only authored depth relative to the attachment surface. Hardware,
            // physical geometry and visual flutter stay unchanged.
            point.x += authoredDepth[index] * (foldSide - 1);
            var weights = flutterWeights[index];
            point.x +=
                weights.x * flutter.x
                + weights.y * flutter.y
                + weights.z * flutter.z
                + weights.w * flutter.w;
            return point;
        }

        internal void Pose(float deployment)
        {
            float set = Mathf.Clamp01(value: deployment);
            TopsailVisible = data.HasTopsail && set > 0.8f;
            Samples(deployment: set, out int left, out int right, out float blend);
            Sprit = SpritPose(deployment: set);
            var posedPeak = Sprit.MultiplyPoint3x4(point: peakTie);
            var correction =
                posedPeak - Sample(index: data.Peak, left: left, right: right, blend: blend);
            var sampledTack = Sample(index: data.Tack, left: left, right: right, blend: blend);
            var sampledThroat = Sample(index: data.Throat, left: left, right: right, blend: blend);
            var sampledClew = Sample(index: data.Clew, left: left, right: right, blend: blend);
            for (int i = 0; i < Vertices.Length; i++)
            {
                float u = coordinates[i].x;
                float v = coordinates[i].y;
                // Retarget the authored cloth to the fitted rigid spar, without moving the luff.
                var point =
                    Sample(index: i, left: left, right: right, blend: blend) + correction * u * v;
                float attachmentDepth = Vector3
                    .Lerp(
                        a: Vector3.Lerp(a: sampledTack, b: sampledThroat, t: v),
                        b: Vector3.Lerp(a: sampledClew, b: posedPeak, t: v),
                        t: u
                    )
                    .x;
                authoredDepth[i] = point.x - attachmentDepth;
                Vertices[i] = point;
            }
            Vertices[data.Peak] = posedPeak;
            if (data.HasTopsail && deployedArea > 0)
            {
                AreaFraction = Mathf.Clamp01(value: ProjectedArea(out var center) / deployedArea);
                AreaCenter = center;
            }
            authoredDepth[data.Tack] =
                authoredDepth[data.Throat] =
                authoredDepth[data.Peak] =
                authoredDepth[data.Clew] =
                    0;
        }

        // Project both cloth pieces onto the rig plane so folds do not add sail area.
        private float ProjectedArea(out Vector3 center)
        {
            float area = 0;
            center = Vector3.zero;
            var triangles = data.Sail.SubmeshTriangles[0];
            int count = TopsailVisible ? triangles.Length : data.TopsailTriangleStart;
            for (int i = 0; i < count; i += 3)
            {
                var a = Vertices[triangles[i]];
                var b = Vertices[triangles[i + 1]];
                var c = Vertices[triangles[i + 2]];
                float weight = Math.Abs(Vector3.Cross(lhs: b - a, rhs: c - a).x) / 2;
                area += weight;
                center += (a + b + c) * (weight / 3);
            }
            center = area > 1e-8f ? center / area : (Throat + Tack + Peak + Clew) / 4;
            return area;
        }

        internal Matrix4x4 SparPose(float deployment)
        {
            Samples(deployment: deployment, out int left, out int right, out float blend);
            var a = data.ReefPoses[left].SparTransform;
            var b = data.ReefPoses[right].SparTransform;
            return LooseFootedSpritsailTransform.TRS(
                pos: Vector3.Lerp(a: a.GetColumn(3), b: b.GetColumn(3), t: blend),
                q: LooseFootedSpritsailTransform.InterpolateRotation(
                    a: LooseFootedSpritsailTransform.Rotation(matrix: a),
                    b: LooseFootedSpritsailTransform.Rotation(matrix: b),
                    t: blend
                ),
                s: Vector3.Lerp(
                    a: LooseFootedSpritsailTransform.Scale(matrix: a),
                    b: LooseFootedSpritsailTransform.Scale(matrix: b),
                    t: blend
                )
            );
        }

        private void Samples(float deployment, out int left, out int right, out float blend)
        {
            if (data.ReefFractions != null)
            {
                float reef = 1 - Mathf.Clamp01(value: deployment);
                left = 0;
                while (left < data.ReefFractions.Length - 2 && reef > data.ReefFractions[left + 1])
                    left++;
                right = left + 1;
                blend = Mathf.Clamp01(
                    value: (reef - data.ReefFractions[left])
                        / (data.ReefFractions[right] - data.ReefFractions[left])
                );
                return;
            }
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

        internal LooseFootedSpritsailClearance Clearance()
        {
            // Native gaff fitting checks spars, not cloth. Keep the deployed sprit
            // in its own axes so neither cloth nor reefing adds false obstructions.
            var scale = LooseFootedSpritsailTransform.Scale(matrix: rest.Sprit);
            var bounds = new Bounds(
                center: Vector3.Scale(a: data.Sprit.Vertices[0], b: scale),
                size: Vector3.zero
            );
            foreach (var point in data.Sprit.Vertices)
                bounds.Encapsulate(point: Vector3.Scale(a: point, b: scale));
            bounds.Expand(amount: 0.02f);
            var rotation = LooseFootedSpritsailTransform.Rotation(matrix: rest.Sprit);
            return new LooseFootedSpritsailClearance
            {
                Center = Pivot + rotation * bounds.center,
                Size = bounds.size,
                Rotation = rotation,
            };
        }
    }

    internal struct LooseFootedSpritsailClearance
    {
        internal Vector3 Center;
        internal Vector3 Size;
        internal Quaternion Rotation;
    }
}
