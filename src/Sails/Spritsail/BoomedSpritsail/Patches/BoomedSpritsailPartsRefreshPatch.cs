using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.Patches
{
    // Invalidates boomed support bindings after boat parts change.

    [HarmonyPatch(typeof(BoatCustomParts), "RefreshParts")]
    internal static class BoomedSpritsailPartsRefreshPatch
    {
        [HarmonyPostfix]
        internal static void Postfix(BoatCustomParts __instance)
        {
            foreach (
                var rig in __instance.GetComponentsInChildren<BoomedSpritsailRigging>(
                    includeInactive: true
                )
            )
                rig.Invalidate();
        }
    }
}
