using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Scopes mast compatible patch behavior to the spritsail family.

    [HarmonyPatch(typeof(ShipyardUI), "SailMastCompatible")]
    internal static class LooseFootedSpritsailMastCompatiblePatch
    {
        [HarmonyPostfix]
        private static void Postfix(GameObject sailPrefab, ref bool __result)
        {
            if (__result && sailPrefab.GetComponent<LooseFootedSpritsailRig>())
                __result = LooseFootedSpritsailRigging.TryResolve(
                    fore: GameState.currentShipyard.sailInstaller.GetCurrentMast(),
                    pair: out _
                );
        }
    }
}
