using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.Patches
{
    // Routes native Cloth refresh requests to the boomed rig lifecycle.
    [HarmonyPatch(typeof(ReefEffectAnimUniversal), "RefreshCloth")]
    internal static class BoomedSpritsailClothRefreshPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ReefEffectAnimUniversal __instance)
        {
            var rig = __instance.GetComponent<BoomedSpritsailRig>();
            if (!rig)
                return true;
            rig.RefreshCloth();
            return false;
        }
    }
}
