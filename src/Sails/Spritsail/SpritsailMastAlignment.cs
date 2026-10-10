using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Defines boat-frame mast eligibility and orders native guides along its upward axis.
    internal static class SpritsailMastAlignment
    {
        internal static bool IsUsable(Vector3 boatLocalAxis)
        {
            if (
                !SpritsailDeployment.Finite(value: boatLocalAxis.x)
                || !SpritsailDeployment.Finite(value: boatLocalAxis.y)
                || !SpritsailDeployment.Finite(value: boatLocalAxis.z)
                || boatLocalAxis.sqrMagnitude < 0.000001f
            )
                return false;
            // Horizontal spars cannot define the existing height-based installation frame.
            return Mathf.Abs(f: boatLocalAxis.normalized.y) > 0.001f;
        }

        internal static Vector3 LocalAxis(int direction) =>
            direction == 0 ? Vector3.right
            : direction == 1 ? Vector3.up
            : Vector3.forward;

        // Native attachment arrays describe routing roles, not guaranteed vertical order.
        // Compare in boat space and orient the usable mast axis upward, including reversed axes.
        internal static bool ExtensionIsLower(
            Vector3 primary,
            Vector3 extension,
            Vector3 boatLocalAxis
        ) =>
            Vector3.Dot(lhs: primary - extension, rhs: boatLocalAxis)
                * (boatLocalAxis.y < 0 ? -1 : 1)
            > 0;
    }
}
