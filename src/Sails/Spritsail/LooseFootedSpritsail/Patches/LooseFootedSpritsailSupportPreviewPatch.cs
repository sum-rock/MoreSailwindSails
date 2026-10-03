using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Scopes support preview patch behavior to the spritsail family.

    [HarmonyPatch(typeof(BoatPart), "SetOptionEnabled")]
    internal static class LooseFootedSpritsailSupportPreviewPatch
    {
        [HarmonyPrefix]
        private static void Prefix(BoatPart __instance, int i, ref bool state)
        {
            if (state)
                return;
            var option = __instance.partOptions[i];
            var boat = option.GetComponentInParent<BoatRefs>();
            if (
                boat
                && boat.GetComponentsInChildren<LooseFootedSpritsailRigging>(includeInactive: true)
                    .Any(predicate: r => r.DependsOn(option: option))
            )
                state = true;
        }
    }
}
