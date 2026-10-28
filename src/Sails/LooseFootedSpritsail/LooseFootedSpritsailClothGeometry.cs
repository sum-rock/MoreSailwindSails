// Poses fitted cloth and its rigid sprit from native deployment and signed wind pressure.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal sealed class LooseFootedSpritsailClothGeometry
    {
        internal const float BillowDepth = 0.16f;
        private readonly LooseFootedSpritsailAssetData data;
        private readonly LooseFootedSpritsailPose rest;
        private readonly Vector2[] coordinates;
        private readonly Vector3 throat,
            tack,
            peak,
            clew,
            peakTie;
        private readonly float rotation,
            footLength,
            leechLength,
            snotterFraction;
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
            throat = rest.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[data.Throat]);
            tack = rest.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[data.Tack]);
            peak = rest.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[data.Peak]);
            clew = rest.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[data.Clew]);
            Pivot = rest.Sprit.MultiplyPoint3x4(point: Vector3.zero);
            peakTie = LooseFootedSpritsailTransform.InversePoint(matrix: rest.Sprit, point: peak);
            var direction = peak - Pivot;
            float angle = (float)(Math.Atan2(-direction.z, direction.y) * 180 / Math.PI);
            float radius = (float)Math.Sqrt(direction.y * direction.y + direction.z * direction.z);
            // Small cuts on thick masts must stop before their peak crosses the luff plane.
            float mastAngle = (float)(
                Math.Asin(Math.Min(1, Math.Max(0, (Pivot.z - throat.z) / radius))) * 180 / Math.PI
            );
            rotation = Math.Max(0, angle - Math.Max(5, mastAngle));
            footLength = (clew - tack).magnitude;
            leechLength = (peak - clew).magnitude;
            snotterFraction = Mathf.Clamp01(value: (Pivot.y - tack.y) / (throat.y - tack.y));
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
            }
            Pose(deployment: 1, load: 0);
        }

        internal void Pose(float deployment, float load)
        {
            float set = Mathf.Clamp01(value: deployment);
            float gathered = 1 - set;
            float pressure = Math.Max(-1, Math.Min(1, load));
            float shaping = Math.Abs(pressure) * set;
            Sprit = SpritPose(deployment: set);
            var posedPeak = Sprit.MultiplyPoint3x4(point: peakTie);
            var posedTack = Vector3.Lerp(a: tack, b: SnotterTie, t: gathered);
            var desiredClew = Vector3.Lerp(
                a: clew,
                b: SnotterTie + new Vector3(x: 0, y: 0, z: -0.03f),
                t: gathered
            );
            // Shorten the foot's reach toward the mast, bringing the clew inboard on
            // either tack while the peak remains tied to the rigid sprit.
            desiredClew.z += (tack.z - clew.z) * 0.1f * shaping;
            desiredClew.z = Math.Min(desiredClew.z, posedTack.z - 0.03f);
            float leechBow = Math.Min(footLength, leechLength) * 0.02f * shaping;
            // Reserve cloth for the quadratic leech arc. Its control polygon is no
            // longer than sqrt(chord² + 16 * bow²), so this also bounds the curve.
            float leechBudget = (float)
                Math.Sqrt(leechLength * leechLength - 16 * leechBow * leechBow);
            var posedClew = ConstrainClew(
                desired: desiredClew,
                tack: posedTack,
                peak: posedPeak,
                foot: footLength,
                leech: leechBudget
            );
            for (int i = 0; i < Vertices.Length; i++)
            {
                float u = coordinates[i].x;
                float v = coordinates[i].y;
                // Keep the upper luff tied to the mast while the bottom gathers to the snotter.
                var luff =
                    v < snotterFraction && snotterFraction > 0
                        ? Vector3.Lerp(a: posedTack, b: SnotterTie, t: v / snotterFraction)
                        : Vector3.Lerp(
                            a: SnotterTie,
                            b: throat,
                            t: (v - snotterFraction) / (1 - snotterFraction)
                        );
                var leech = Vector3.Lerp(a: posedClew, b: posedPeak, t: v);
                leech.z += 4 * v * (1 - v) * leechBow;
                var point = Vector3.Lerp(a: luff, b: leech, t: u);
                float across = 4 * u * (1 - u);
                float vertical = 4 * v * (1 - v);
                // The head and loose foot bow between their corner ties rather than
                // pinning whole rows flat. Blend their softer arcs into the deeper belly.
                // Build the belly sooner behind the luff, then taper toward the leech.
                float billowEnvelope = across * (1 + 0.6f * (1 - u)) * (0.55f + 0.45f * vertical);
                float chord = (leech - luff).magnitude;
                point.x -= BillowDepth * chord * pressure * set * billowEnvelope;
                // Folds vanish on every supported edge and leave a visible, reversible bundle.
                point.x +=
                    Math.Min(0.08f, footLength * 0.025f)
                    * gathered
                    * across
                    * vertical
                    * (float)Math.Sin(u * Math.PI * 8 + v * Math.PI * 4);
                Vertices[i] = point;
            }
            Vertices[data.Throat] = throat;
            Vertices[data.Tack] = posedTack;
            Vertices[data.Peak] = posedPeak;
            Vertices[data.Clew] = posedClew;
        }

        // Closest point inside both length-budget balls; slack is allowed when gathering.
        private static Vector3 ConstrainClew(
            Vector3 desired,
            Vector3 tack,
            Vector3 peak,
            float foot,
            float leech
        )
        {
            var fromTack = desired - tack;
            var fromPeak = desired - peak;
            if (fromTack.magnitude <= foot && fromPeak.magnitude <= leech)
                return desired;
            var onFoot = tack + Vector3.ClampMagnitude(vector: fromTack, maxLength: foot);
            if ((onFoot - peak).magnitude <= leech)
                return onFoot;
            var onLeech = peak + Vector3.ClampMagnitude(vector: fromPeak, maxLength: leech);
            if ((onLeech - tack).magnitude <= foot)
                return onLeech;
            var between = peak - tack;
            float distance = between.magnitude;
            var axis = between / distance;
            float along = (foot * foot - leech * leech + distance * distance) / (2 * distance);
            var center = tack + axis * along;
            float radius = (float)Math.Sqrt(Math.Max(0, foot * foot - along * along));
            var radial = desired - center;
            radial -= axis * Vector3.Dot(lhs: radial, rhs: axis);
            if (radial.sqrMagnitude < 1e-10f)
                radial = Vector3.Cross(lhs: axis, rhs: Vector3.right);
            return center + radial.normalized * radius;
        }

        internal static float SmoothLoad(float current, float target, float deltaTime) =>
            current
            + (target - current)
                * (1 - (float)Math.Exp(-3 * Math.Max(0, Math.Min(0.1f, deltaTime))));

        internal Matrix4x4 SpritPose(float deployment) =>
            LooseFootedSpritsailSailingGeometry.PivotRotation(
                center: Pivot,
                axis: Vector3.right,
                degrees: rotation * (1 - Mathf.Clamp01(value: deployment))
            ) * rest.Sprit;

        internal bool CanRoute(Vector3 guide)
        {
            if (guide.y <= Pivot.y)
                return false;
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

        internal Bounds[] Clearance()
        {
            const int steps = 64;
            int strips = LooseFootedSpritsailFittingGeometry.CollisionStrips;
            var bounds = new Bounds[strips + 1];
            var initialized = new bool[bounds.Length];
            for (int step = 0; step <= steps; step++)
                foreach (float wind in new[] { -1f, 0f, 1f })
                {
                    Pose(deployment: step / (float)steps, load: wind);
                    for (int i = 0; i < Vertices.Length; i++)
                    {
                        // Overlap adjacent authored columns so every triangle is covered.
                        int column = (int)Math.Round(coordinates[i].x * (strips - 1));
                        Include(
                            bounds: bounds,
                            initialized: initialized,
                            index: column,
                            point: Vertices[i]
                        );
                        if (column > 0)
                            Include(
                                bounds: bounds,
                                initialized: initialized,
                                index: column - 1,
                                point: Vertices[i]
                            );
                    }
                    foreach (var point in data.Sprit.Vertices)
                        Include(
                            bounds: bounds,
                            initialized: initialized,
                            index: strips,
                            point: Sprit.MultiplyPoint3x4(point: point)
                        );
                }
            // Pad the sweep between samples, including billow/fold extrema between grid vertices.
            float travel = (peak - Pivot).magnitude * (float)Math.PI / steps;
            float padding = travel + footLength * BillowDepth / (strips - 1) + 0.02f;
            for (int i = 0; i < bounds.Length; i++)
            {
                bounds[i].Expand(amount: 2 * padding);
                if (i < strips)
                {
                    var maximum = bounds[i].max;
                    maximum.z = Math.Min(maximum.z, throat.z);
                    bounds[i].SetMinMax(min: bounds[i].min, max: maximum);
                }
            }
            Pose(deployment: 1, load: 0);
            return bounds;
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
}
