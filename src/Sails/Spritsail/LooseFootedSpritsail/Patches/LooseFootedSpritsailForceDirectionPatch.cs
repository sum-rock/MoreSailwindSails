using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Scopes force direction patch behavior to the spritsail family.

    [HarmonyPatch(typeof(Sail), "GetSailForceDirection")]
    internal static class LooseFootedSpritsailForceDirectionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail __instance, ref Vector3 __result)
        {
            var rig = __instance.GetComponent<LooseFootedSpritsailRig>();
            if (!rig || !rig.RefreshAerodynamics())
                return true;
            __result = LooseFootedSpritsailAerodynamics.ForceDirection(
                wind: __instance.apparentWind,
                normal: __instance.windcenter.up
            );
            return false;
        }
    }
}
