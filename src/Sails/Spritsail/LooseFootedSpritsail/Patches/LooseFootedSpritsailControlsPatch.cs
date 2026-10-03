using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Let native binding process only native-controlled sails. Restore the actual
    // list even on exceptions; saves, mast capacity and overlap checks see all sails.
    [HarmonyPatch(typeof(Mast), "UpdateControllerAttachments")]
    internal static class LooseFootedSpritsailControlsPatch
    {
        // Wrap other families' filters: retain the full outer list and restore it last.
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Mast __instance, out List<GameObject> __state)
        {
            __state = null;
            if (
                __instance.sails == null
                || !__instance.sails.Any(predicate: s =>
                    s && s.GetComponent<LooseFootedSpritsailRig>()
                )
            )
                return;
            __state = __instance.sails;
            __instance.sails = __state
                .Where(predicate: s => s && !s.GetComponent<LooseFootedSpritsailRig>())
                .ToList();
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static void Finalizer(
            Mast __instance,
            List<GameObject> __state,
            Exception __exception
        )
        {
            if (__state == null)
                return;
            __instance.sails = __state;
            __instance.UpdateSailOrder();
            if (__exception != null)
                return;
            foreach (var item in __state)
                if (item && item.GetComponent<LooseFootedSpritsailRig>())
                    LooseFootedSpritsailRigging
                        .For(sail: item.GetComponent<Sail>())
                        .AttachControls();
        }
    }
}
