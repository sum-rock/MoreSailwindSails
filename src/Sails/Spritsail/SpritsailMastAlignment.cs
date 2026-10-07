using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Validates a finite mast axis while allowing rake and lean in the boat frame.
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
    }
}
