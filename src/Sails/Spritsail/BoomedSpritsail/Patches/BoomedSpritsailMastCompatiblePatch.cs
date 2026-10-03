using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.Patches
{
    // Restricts boomed fitting to supported active physical masts.

    [HarmonyPatch(typeof(ShipyardUI), "SailMastCompatible")]
    internal static class BoomedSpritsailMastCompatiblePatch
    {
        [HarmonyPostfix]
        private static void Postfix(GameObject sailPrefab, ref bool __result)
        {
            if (__result && sailPrefab.GetComponent<BoomedSpritsailRig>())
                __result = BoomedSpritsailRigging.TryResolve(
                    fore: GameState.currentShipyard.sailInstaller.GetCurrentMast(),
                    pair: out _
                );
        }
    }
}
