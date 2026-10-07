using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail
{
    // Raises two rigid spars about fixed mast pivots while retaining the boom-supported foot.
    internal static class BoomedSpritsailDeployment
    {
        internal static SpritsailDeploymentPose Evaluate(Vector3[] corners, float unroll)
        {
            if (corners == null || corners.Length != 4)
                throw new ArgumentException(message: "Expected four boomed spritsail corners.");
            foreach (var point in corners)
                if (
                    !SpritsailDeployment.Finite(value: point.x)
                    || !SpritsailDeployment.Finite(value: point.y)
                    || !SpritsailDeployment.Finite(value: point.z)
                )
                    throw new ArgumentException(message: "Non-finite boomed spritsail corner.");
            float width = corners[3].z - corners[2].z;
            if (width <= 0 || corners[0].x <= corners[2].x)
                throw new ArgumentException(message: "Expected positive span and luff.");
            float amount = SpritsailDeployment.Amount(unroll: unroll);
            var socket = SpritsailDeployment.WorkingSocket(corners: corners);
            // Both ends share the side offset, keeping the spar perpendicular to its bolt.
            // Only the foot changes: its fixed tack drives a rigid boom arc.
            var side = SpritsailDeployment.LashingOffset(corners: corners);
            var tip = Pivot(origin: socket, working: corners[1] + side, amount: amount);
            var clew = Pivot(origin: corners[2], working: corners[3], amount: amount);
            return new SpritsailDeploymentPose(
                throat: corners[0],
                peak: tip - side,
                tack: corners[2],
                clew: clew,
                heel: socket,
                tip: tip
            );
        }

        private static Vector3 Pivot(Vector3 origin, Vector3 working, float amount)
        {
            var direction = working - origin;
            float length = direction.magnitude;
            float angle = (float)Math.Acos(Math.Max(-1, Math.Min(1, direction.x / length)));
            var outward = new Vector3(0, direction.y, direction.z).normalized;
            return origin
                + length
                    * (
                        Vector3.right * (float)Math.Cos(angle * amount)
                        + outward * (float)Math.Sin(angle * amount)
                    );
        }
    }
}
