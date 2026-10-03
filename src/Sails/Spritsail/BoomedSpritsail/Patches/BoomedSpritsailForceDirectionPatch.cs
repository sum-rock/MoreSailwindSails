using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.Patches
{
    // Uses the posed boomed aerodynamic normal for native force direction.

    [HarmonyPatch(typeof(Sail), "GetSailForceDirection")]
    internal static class BoomedSpritsailForceDirectionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail __instance, ref Vector3 __result)
        {
            var rig = __instance.GetComponent<BoomedSpritsailRig>();
            if (!rig || !rig.RefreshAerodynamics())
                return true;
            __result = BoomedSpritsailAerodynamics.ForceDirection(
                wind: __instance.apparentWind,
                normal: __instance.windcenter.up
            );
            return false;
        }
    }
}
