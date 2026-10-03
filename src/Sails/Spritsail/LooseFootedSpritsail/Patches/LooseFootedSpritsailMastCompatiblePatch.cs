using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Restricts loose-footed fitting to supported active physical masts.

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
