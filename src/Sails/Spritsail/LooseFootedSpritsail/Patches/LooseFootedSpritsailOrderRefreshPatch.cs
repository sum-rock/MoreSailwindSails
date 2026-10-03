using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Scopes order refresh patch behavior to the spritsail family.

    [HarmonyPatch(typeof(BoatCustomParts), "RefreshPartsWithOrder")]
    internal static class LooseFootedSpritsailOrderRefreshPatch
    {
        [HarmonyPostfix]
        private static void Postfix(BoatCustomParts __instance) =>
            LooseFootedSpritsailPartsRefreshPatch.Postfix(__instance: __instance);
    }
}
