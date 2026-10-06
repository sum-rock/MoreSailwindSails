using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Tests physical spar alignment in the boat frame, independent of heel and pitch.
    internal static class SpritsailMastAlignment
    {
        internal static bool IsUpright(Vector3 boatLocalAxis)
        {
            if (
                !SpritsailDeployment.Finite(value: boatLocalAxis.x)
                || !SpritsailDeployment.Finite(value: boatLocalAxis.y)
                || !SpritsailDeployment.Finite(value: boatLocalAxis.z)
                || boatLocalAxis.sqrMagnitude < 0.000001f
            )
                return false;
            // Allow 0.1 degrees of transform noise around the boat's vertical axis.
            return boatLocalAxis.x * boatLocalAxis.x + boatLocalAxis.z * boatLocalAxis.z
                <= boatLocalAxis.y * boatLocalAxis.y * 0.00000304618f;
        }
    }
}
