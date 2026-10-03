using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Scopes cloth refresh patch behavior to the spritsail family.
    [HarmonyPatch(typeof(ReefEffectAnimUniversal), "RefreshCloth")]
    internal static class LooseFootedSpritsailClothRefreshPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ReefEffectAnimUniversal __instance)
        {
            var rig = __instance.GetComponent<LooseFootedSpritsailRig>();
            if (!rig)
                return true;
            rig.RefreshCloth();
            return false;
        }
    }
}
