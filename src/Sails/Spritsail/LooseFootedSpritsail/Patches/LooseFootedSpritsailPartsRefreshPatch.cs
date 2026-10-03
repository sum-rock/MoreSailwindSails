using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Invalidates loose-footed support bindings after boat parts change.

    [HarmonyPatch(typeof(BoatCustomParts), "RefreshParts")]
    internal static class LooseFootedSpritsailPartsRefreshPatch
    {
        [HarmonyPostfix]
        internal static void Postfix(BoatCustomParts __instance)
        {
            foreach (
                var rig in __instance.GetComponentsInChildren<LooseFootedSpritsailRigging>(
                    includeInactive: true
                )
            )
                rig.Invalidate();
        }
    }
}
