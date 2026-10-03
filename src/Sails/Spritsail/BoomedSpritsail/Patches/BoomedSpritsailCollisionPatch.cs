using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.Patches
{
    // Scopes collision patch behavior to the spritsail family.
    [HarmonyPatch(typeof(ShipyardSailColChecker), "UpdateRotation")]
    internal static class BoomedSpritsailCollisionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(
            ShipyardSailColChecker __instance,
            Sail ___sail,
            int ___checkDelay,
            ref UnityEngine.Quaternion ___initialRot
        )
        {
            var rig = ___sail ? ___sail.GetComponent<BoomedSpritsailRig>() : null;
            if (!rig || !rig.RefreshMastFrame())
                return true;
            // The native checker first waits above the ship for contacts to
            // clear. After that, sweep around the same mast axis as the sail.
            if (___checkDelay <= 2)
            {
                // The final step ends the sweep. Return both position and
                // rotation to neutral before native code publishes the result.
                float angle =
                    __instance.currentAngle > __instance.startMaxAngle
                        ? 0
                        : __instance.currentAngle;
                if (
                    !rig.PositionCollisionChecker(
                        checker: __instance.transform,
                        angle: angle,
                        neutralRotation: out var neutral
                    )
                )
                    return true;
                // FixedUpdate restores initialRot after the final sweep. Preserve
                // our aligned neutral panel instead of the donor's old axes.
                ___initialRot = neutral;
            }
            return false;
        }
    }
}
