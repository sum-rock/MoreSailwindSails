using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Blocks removal of mast sections supporting a loose-footed sail.

    [HarmonyPatch(typeof(BoatCustomParts), "CanUninstall")]
    internal static class LooseFootedSpritsailSupportRemovalPatch
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
                    .GetComponentsInChildren<LooseFootedSpritsailRigging>(includeInactive: true)
                    .Any(predicate: r => r.DependsOn(option: option))
            )
                return;
            __result = false;
            dependentOptionNames = ": supports a Loose-footed Spritsail; remove the sail first.";
        }
    }
}
