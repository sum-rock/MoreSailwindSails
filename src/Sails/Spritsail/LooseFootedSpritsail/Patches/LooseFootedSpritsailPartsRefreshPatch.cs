using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Scopes parts refresh patch behavior to the spritsail family.

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
