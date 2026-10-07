using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Validates spritsail slots after custom-family filters, leaving valid sails on ordinary native binding.
    [HarmonyPatch(typeof(Mast), "UpdateControllerAttachments")]
    internal static class SpritsailNativeControlsPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(Mast __instance, out List<SpritsailControlBindingState> __state)
        {
            __state = new List<SpritsailControlBindingState>();
            if (
                __instance.sails == null
                || !__instance.sails.Any(predicate: item =>
                    item && SpritsailNativeBinding.IsSpritsail(sail: item.GetComponent<Sail>())
                )
            )
                return true;
            __instance.UpdateSailOrder();
            var sails = __instance
                .sails.Select(selector: item => item.GetComponent<Sail>())
                .ToArray();
            var slots = new GPButtonRopeWinch[sails.Length][];
            // Native DetachAllRopes/UpdateWinchesEnabled dereference every array entry,
            // even unused slots. Do not run them against malformed modded mast data.
            bool arraysValid = new[]
            {
                __instance.reefWinch,
                __instance.midAngleWinch,
                __instance.leftAngleWinch,
                __instance.rightAngleWinch,
            }.All(predicate: items => items != null && items.All(predicate: item => item));
            bool squareBelow = false;
            for (int i = 0; i < sails.Length; i++)
            {
                slots[i] = Controls(
                    mast: __instance,
                    sail: sails[i],
                    index: i,
                    topsail: squareBelow && sails[i].squareSail
                );
                squareBelow |= sails[i].squareSail;
            }
            for (int i = 0; i < sails.Length; i++)
            {
                var sail = sails[i];
                if (!SpritsailNativeBinding.IsSpritsail(sail: sail))
                    continue;
                var state = new SpritsailControlBindingState(sail: sail);
                __state.Add(item: state);
                var lower = SpritsailNativeBinding.Attachment(
                    items: __instance.mastReefAtt,
                    index: i
                );
                var upper = SpritsailNativeBinding.Attachment(
                    items: __instance.mastReefAttExtension,
                    index: i
                );
                var connections = sail.GetComponent<SailConnections>();
                string error =
                    !arraysValid
                    || slots[i]
                        .Any(predicate: control =>
                            !control
                            || (
                                control.transform.parent
                                && !control.transform.parent.gameObject.activeInHierarchy
                            )
                        )
                        ? "(NATIVE MAST CONTROL SLOT UNAVAILABLE)"
                    : __instance.mastReefAttExtension == null
                    || (connections.midRopeAttachment && __instance.midRopeAtt == null)
                    || !lower
                    || !lower.gameObject.activeInHierarchy
                    || (upper && !upper.gameObject.activeInHierarchy)
                        ? "(NATIVE MAST GUIDE UNAVAILABLE)"
                    : SpritsailControlSlots.Conflict(
                        requested: slots[i],
                        others: slots
                            .Where((_, index) => index != i)
                            .SelectMany(selector: controls => controls)
                    )
                        ? "(NATIVE MAST CONTROLS ALREADY REQUIRED BY ANOTHER SAIL)"
                    : null;
                state.Binding.Capture(mast: __instance, slot: i, error: error);
                if (error != null)
                    state.Suppress();
            }
            return arraysValid;
        }

        private static GPButtonRopeWinch[] Controls(Mast mast, Sail sail, int index, bool topsail)
        {
            var connections = sail.GetComponent<SailConnections>();
            var result = new List<GPButtonRopeWinch>();
            if (connections.reefController)
                result.Add(item: SpritsailControlSlots.At(items: mast.reefWinch, index: index));
            if (connections.angleControllerMid)
                result.Add(item: SpritsailControlSlots.At(items: mast.midAngleWinch, index: index));
            int sheet = SpritsailControlSlots.SheetIndex(square: sail.squareSail, order: index);
            if (!topsail && connections.angleControllerLeft)
                result.Add(
                    item: SpritsailControlSlots.At(items: mast.leftAngleWinch, index: sheet)
                );
            if (!topsail && connections.angleControllerRight)
                result.Add(
                    item: SpritsailControlSlots.At(items: mast.rightAngleWinch, index: sheet)
                );
            return result.ToArray();
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.First)]
        private static void Finalizer(
            List<SpritsailControlBindingState> __state,
            Exception __exception
        )
        {
            if (__state == null)
                return;
            foreach (var state in __state)
            {
                if (__exception != null)
                    state.Binding.Capture(
                        mast: state.Binding.Mast,
                        slot: state.Binding.Slot,
                        error: "(NATIVE CONTROL BINDING FAILED)"
                    );
                state.Restore(failed: __exception != null);
            }
        }
    }
}
