using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    internal static class FishermansStaysailReefingGeometry
    {
        // Normalize sampled native animation channels, not time. Reversing the
        // halyard samples the same pose without a separate transition state.
        internal static float Progress(Vector3 sample, Vector3 closed, Vector3 open)
        {
            var delta = open - closed;
            if (delta.sqrMagnitude < 1e-8f)
                throw new ArgumentException("Native reef channel has no motion.");
            return Math.Max(
                0,
                Math.Min(1, Vector3.Dot(sample - closed, delta) / delta.sqrMagnitude)
            );
        }

        internal static Vector3 Pose(Vector3 point, Vector3 foreHead, Vector3 aftHead, float lift)
        {
            lift = Math.Max(0, Math.Min(1, lift));
            // Keep the entire head span in place. Each column gathers upward
            // to the sloping head, rather than shrinking toward a single corner.
            float along = (point.z - foreHead.z) / (aftHead.z - foreHead.z);
            var head = Vector3.Lerp(foreHead, aftHead, along);
            return new Vector3(
                head.x + (point.x - head.x) * Math.Max(0.015f, lift),
                point.y,
                point.z
            );
        }
    }
}
