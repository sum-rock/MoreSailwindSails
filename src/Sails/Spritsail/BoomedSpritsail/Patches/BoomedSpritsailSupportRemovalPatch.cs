using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.Patches
{
    // Blocks removal of mast sections supporting a boomed sail.

    [HarmonyPatch(typeof(BoatCustomParts), "CanUninstall")]
    internal static class BoomedSpritsailSupportRemovalPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            BoatCustomParts __instance,
            int partIndex,
            int optionIndex,
            ref bool __result,
            ref string dependentOptionNames
        )
        {
            var option = __instance.availableParts[partIndex].partOptions[optionIndex];
            if (
                !__instance
                    .GetComponentsInChildren<BoomedSpritsailRigging>(includeInactive: true)
                    .Any(predicate: r => r.DependsOn(option: option))
            )
                return;
            __result = false;
            dependentOptionNames = ": supports a Boomed Spritsail; remove the sail first.";
        }
    }
}
