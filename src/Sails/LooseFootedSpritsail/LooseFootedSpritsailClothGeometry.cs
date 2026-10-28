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
            var direction = peak - Pivot;
            // Rotation about the mounting rod (X) sweeps the peak through a Y/Z circle.
            // atan2 measures its current angle away from vertical; radius is that circle's radius.
            float angle = (float)(Math.Atan2(-direction.z, direction.y) * 180 / Math.PI);
            float radius = (float)Math.Sqrt(direction.y * direction.y + direction.z * direction.z);
            // Small cuts on thick masts must stop before their peak crosses the luff plane.
            // At that limit, sin(angle) = horizontal mast clearance / sweep radius.
            // Keep at least five degrees from vertical, and never lower an already steeper spar.
            float mastAngle = (float)(
                Math.Asin(Math.Min(1, Math.Max(0, (Pivot.z - throat.z) / radius))) * 180 / Math.PI
            );
            rotation = Math.Max(0, angle - Math.Max(5, mastAngle));
            footLength = (clew - tack).magnitude;
            leechLength = (peak - clew).magnitude;
            // The snotter divides the luff into a fixed upper span and a gathering lower span.
            snotterFraction = Mathf.Clamp01(value: (Pivot.y - tack.y) / (throat.y - tack.y));
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
            }
            Pose(deployment: 1, load: 0);
        }

        internal void Pose(float deployment, float load)
        {
            // Deployment is one when fully set and zero when brailed. Keep the wind's
            // sign for billow direction, but use its magnitude for symmetric inward gathering.
            float set = Mathf.Clamp01(value: deployment);
            float gathered = 1 - set;
            float pressure = Math.Max(-1, Math.Min(1, load));
            float shaping = Math.Abs(pressure) * set;
            Sprit = SpritPose(deployment: set);
            var posedPeak = Sprit.MultiplyPoint3x4(point: peakTie);
            var posedTack = Vector3.Lerp(a: tack, b: SnotterTie, t: gathered);
            // Gather toward the snotter while retaining a three-centimetre clew gap;
            // ConstrainClew below corrects this desired position to respect edge lengths.
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
            // A midpoint bow of b requires a quadratic control-point offset of 2b.
            // Solving that length bound for the chord leaves room for the curved edge.
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
                // Remap v separately on either side of the snotter so both spans meet
                // at the same fixed tie even as the lower span becomes shorter.
                var luff =
                    v < snotterFraction && snotterFraction > 0
                        ? Vector3.Lerp(a: posedTack, b: SnotterTie, t: v / snotterFraction)
                        : Vector3.Lerp(
                            a: SnotterTie,
                            b: throat,
                            t: (v - snotterFraction) / (1 - snotterFraction)
                        );
                var leech = Vector3.Lerp(a: posedClew, b: posedPeak, t: v);
                // 4*v*(1-v) is zero at both corners and one halfway up the leech.
                // Adding it in +Z bows the edge mastward without moving its endpoints.
                leech.z += 4 * v * (1 - v) * leechBow;
                var point = Vector3.Lerp(a: luff, b: leech, t: u);
                // These parabolas taper deformation to zero at each pair of opposite edges.
                float across = 4 * u * (1 - u);
                float vertical = 4 * v * (1 - v);
                // The head and loose foot bow between their corner ties rather than
                // pinning whole rows flat. Blend their softer arcs into the deeper belly.
                // Build the belly sooner behind the luff, then taper toward the leech.
                // The 0.6 gain adds fullness near the mast; the 0.55 baseline keeps
                // the head and foot bowed even where the vertical parabola is zero.
                float billowEnvelope = across * (1 + 0.6f * (1 - u)) * (0.55f + 0.45f * vertical);
                float chord = (leech - luff).magnitude;
                point.x -= BillowDepth * chord * pressure * set * billowEnvelope;
                // Folds vanish on every supported edge and leave a visible, reversible bundle.
                // Cap their amplitude at 8 cm (or 2.5% of a small sail's foot). The sine
                // gives four cycles across and two up the cloth, growing as it gathers.
                point.x +=
                    Math.Min(0.08f, footLength * 0.025f)
                    * gathered
                    * across
                    * vertical
                    * (float)Math.Sin(u * Math.PI * 8 + v * Math.PI * 4);
                Vertices[i] = point;
            }
            // Enforce the exact attachment positions after interpolating and deforming the surface.
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
            // Each edge length defines a solid sphere of allowed clew positions:
            // one around the tack for the foot, and one around the peak for the leech.
            var fromTack = desired - tack;
            var fromPeak = desired - peak;
            if (fromTack.magnitude <= foot && fromPeak.magnitude <= leech)
                return desired;
            // Try the nearest point inside each sphere independently. If it is also
            // inside the other sphere, only one length constraint needs to be active.
            var onFoot = tack + Vector3.ClampMagnitude(vector: fromTack, maxLength: foot);
            if ((onFoot - peak).magnitude <= leech)
                return onFoot;
            var onLeech = peak + Vector3.ClampMagnitude(vector: fromPeak, maxLength: leech);
            if ((onLeech - tack).magnitude <= foot)
                return onLeech;
            // Both constraints are active, so the clew lies on the spheres' intersection
            // circle. Subtracting their squared-distance equations gives the circle's
            // distance along the tack-to-peak axis; Pythagoras then gives its radius.
            var between = peak - tack;
            float distance = between.magnitude;
            var axis = between / distance;
            float along = (foot * foot - leech * leech + distance * distance) / (2 * distance);
            var center = tack + axis * along;
            float radius = (float)Math.Sqrt(Math.Max(0, foot * foot - along * along));
            // Remove the desired point's axial component to project it into the circle's
            // plane, then extend to the rim. An axial request needs a perpendicular fallback.
            var radial = desired - center;
            radial -= axis * Vector3.Dot(lhs: radial, rhs: axis);
            if (radial.sqrMagnitude < 1e-10f)
                radial = Vector3.Cross(lhs: axis, rhs: Vector3.right);
            return center + radial.normalized * radius;
        }

        // Exponential easing has a 1/3-second time constant. Cap the time step at 0.1 s
        // so a long frame cannot abruptly flip the billow when the wind changes sides.
        internal static float SmoothLoad(float current, float target, float deltaTime) =>
            current
            + (target - current)
                * (1 - (float)Math.Exp(-3 * Math.Max(0, Math.Min(0.1f, deltaTime))));

        // Premultiply the fitted pose by a rotation around its mounting pivot. This
        // changes the spar's angle without changing its fitted length or thickness.
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

        internal Bounds[] Clearance()
        {
            const int steps = 64;
            int strips = LooseFootedSpritsailFittingGeometry.CollisionStrips;
            var bounds = new Bounds[strips + 1];
            var initialized = new bool[bounds.Length];
            // Sweep deployment under both wind directions and calm conditions, accumulating
            // cloth strips plus one spar box. Calm matters because the clew reaches farther aft.
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
            // The angular sweep margin scales with spar reach; the cloth margin scales
            // with billow depth and strip spacing. Bounds.Expand takes total size growth,
            // so twice the padding adds that margin on each side of the box.
            float travel = (peak - Pivot).magnitude * (float)Math.PI / steps;
            float padding = travel + footLength * BillowDepth / (strips - 1) + 0.02f;
            for (int i = 0; i < bounds.Length; i++)
            {
                bounds[i].Expand(amount: 2 * padding);
                if (i < strips)
                {
                    // Cloth stays aft of the luff plane; padding must not invent an
                    // obstruction inside the mast. The spar has its own unrestricted box.
                    var maximum = bounds[i].max;
                    maximum.z = Math.Min(maximum.z, throat.z);
                    bounds[i].SetMinMax(min: bounds[i].min, max: maximum);
                }
            }
            // Sampling overwrites the working vertices; restore the neutral fitted pose.
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
