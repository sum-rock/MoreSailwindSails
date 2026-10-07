using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Pivots a rigid sprit about its fixed socket and gathers the lower sail upward.
    internal static class SpritsailDeployment
    {
        internal const float SocketLuffFraction = 0.25f;
        internal const float PurchaseFraction = 0.9f;

        internal static float Amount(float unroll) =>
            Finite(value: unroll)
                ? Math.Max(val1: 0, val2: Math.Min(val1: 1, val2: (unroll - 0.02f) / 0.96f))
                : 0;

        internal static Vector3 WorkingSocket(Vector3[] corners)
        {
            if (corners == null || corners.Length != 4)
                throw new ArgumentException(message: "Expected four spritsail corners.");
            return Vector3.Lerp(a: corners[2], b: corners[0], t: SocketLuffFraction)
                + LashingOffset(corners: corners);
        }

        internal static Vector3 LashingOffset(Vector3[] corners) =>
            Vector3.up
            * (
                (corners[3].z - corners[2].z)
                * SpritsailSpritGeometry.RadiusFraction
                * SpritsailSpritGeometry.ThicknessMultiplier
            );

        internal static SpritsailDeploymentPose Evaluate(Vector3[] corners, float unroll) =>
            Evaluate(corners: corners, socket: WorkingSocket(corners: corners), unroll: unroll);

        // Scale-applied sail frame: X along the mast, Y normal, Z aft. The socket
        // is fixed in this rotating frame, offset from the luff along the bolt axis.
        internal static SpritsailDeploymentPose Evaluate(
            Vector3[] corners,
            Vector3 socket,
            float unroll
        )
        {
            if (corners == null || corners.Length != 4 || !Finite(point: socket))
                throw new ArgumentException(
                    message: "Expected four finite corners and a fixed socket."
                );
            foreach (var point in corners)
                if (!Finite(point: point))
                    throw new ArgumentException(message: "Non-finite spritsail corner.");
            float width = corners[3].z - corners[2].z;
            if (width <= 0 || corners[0].x <= corners[2].x)
                throw new ArgumentException(
                    message: "Expected a positive spritsail span and luff."
                );
            float amount = Amount(unroll: unroll);
            var side = LashingOffset(corners: corners);
            var working = corners[1] + side - socket;
            float length = working.magnitude;
            float angle = (float)Math.Acos(Math.Max(-1, Math.Min(1, working.x / length)));
            var outward = new Vector3(0, working.y, working.z).normalized;
            var tip =
                socket
                + length
                    * (
                        Vector3.right * (float)Math.Cos(angle * amount)
                        + outward * (float)Math.Sin(angle * amount)
                    );
            var peak = tip - side;
            var tack = Vector3.Lerp(a: socket - side, b: corners[2], t: amount);
            var clew = GatheredClew(
                requested: Vector3.Lerp(a: socket - side, b: corners[3], t: amount),
                peak: peak,
                tack: tack,
                leech: (corners[1] - corners[3]).magnitude,
                foot: (corners[2] - corners[3]).magnitude
            );
            return new SpritsailDeploymentPose(
                throat: corners[0],
                peak: peak,
                tack: tack,
                clew: clew,
                heel: socket,
                tip: tip
            );
        }

        // Slack edges may shorten, but neither edge may stretch. Project the
        // requested clew into the intersection of the foot/leech length balls.
        private static Vector3 GatheredClew(
            Vector3 requested,
            Vector3 peak,
            Vector3 tack,
            float leech,
            float foot
        )
        {
            Vector3 Limit(Vector3 center, float radius)
            {
                var offset = requested - center;
                return center + offset * Math.Min(1, radius / Math.Max(1e-8f, offset.magnitude));
            }
            var nearPeak = Limit(center: peak, radius: leech);
            if ((nearPeak - tack).magnitude <= foot)
                return nearPeak;
            var nearTack = Limit(center: tack, radius: foot);
            if ((nearTack - peak).magnitude <= leech)
                return nearTack;
            float distance = (tack - peak).magnitude;
            var axis = (tack - peak) / distance;
            float along = (leech * leech - foot * foot + distance * distance) / (2 * distance);
            var lensCenter = peak + axis * along;
            var radial = requested - lensCenter;
            radial -= axis * Vector3.Dot(radial, axis);
            if (radial.sqrMagnitude < 1e-12f)
                radial = Vector3.Cross(axis, Vector3.up);
            if (radial.sqrMagnitude < 1e-12f)
                radial = Vector3.Cross(axis, Vector3.forward);
            return lensCenter
                + radial.normalized * (float)Math.Sqrt(Math.Max(0, leech * leech - along * along));
        }

        internal static Vector3 PurchasePoint(Vector3 heel, Vector3 tip) =>
            Vector3.Lerp(a: heel, b: tip, t: PurchaseFraction);

        // Measure the unpleated outline projected onto the sail plane. Bundle
        // thickness, lash offset and folds must never count as exposed sail area.
        internal static float ExposedArea(SpritsailDeploymentPose pose) =>
            ProjectedTriangle(a: pose.Throat, b: pose.Tack, c: pose.Peak)
            + ProjectedTriangle(a: pose.Tack, b: pose.Clew, c: pose.Peak);

        private static float ProjectedTriangle(Vector3 a, Vector3 b, Vector3 c) =>
            Math.Abs((b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x)) * 0.5f;

        internal static float Area(Vector3 throat, Vector3 peak, Vector3 tack, Vector3 clew) =>
            (
                Vector3.Cross(tack - throat, peak - throat).magnitude
                + Vector3.Cross(tack - peak, clew - peak).magnitude
            ) * 0.5f;

        internal static bool Finite(float value) =>
            !float.IsNaN(f: value) && !float.IsInfinity(f: value);

        private static bool Finite(Vector3 point) =>
            Finite(value: point.x) && Finite(value: point.y) && Finite(value: point.z);
    }
}
