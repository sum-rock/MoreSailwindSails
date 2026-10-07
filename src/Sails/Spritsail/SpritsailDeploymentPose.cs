using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // A complete deployment sample, independent of Unity objects and prior frames.
    internal readonly struct SpritsailDeploymentPose
    {
        // Heel is the bolt pivot in the rotating sail frame; the rendered spar extends forward of it.
        internal readonly Vector3 Throat,
            Peak,
            Tack,
            Clew,
            Heel,
            Tip;

        internal SpritsailDeploymentPose(
            Vector3 throat,
            Vector3 peak,
            Vector3 tack,
            Vector3 clew,
            Vector3 heel,
            Vector3 tip
        )
        {
            Throat = throat;
            Peak = peak;
            Tack = tack;
            Clew = clew;
            Heel = heel;
            Tip = tip;
        }
    }
}
