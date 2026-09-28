using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Controls.Patches
{
    // Installed native callbacks invalidate discovery; the one-second sweep also
    // catches delayed/inactive SE parts that have not run their Unity callbacks.
    [HarmonyPatch]
    internal static class WinchInventoryInvalidationPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            yield return typeof(Mast).GetMethod(name: "Awake", bindingAttr: flags);
            yield return typeof(Mast).GetMethod(name: "OnEnable", bindingAttr: flags);
            yield return typeof(Mast).GetMethod(name: "OnDisable", bindingAttr: flags);
            yield return typeof(BoatCustomParts).GetMethod(
                name: "RefreshParts",
                bindingAttr: flags
            );
            yield return typeof(BoatCustomParts).GetMethod(
                name: "RefreshPartsWithOrder",
                bindingAttr: flags
            );
            yield return typeof(SaveableBoatCustomization).GetMethod(
                name: "Awake",
                bindingAttr: flags
            );
        }

        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(Component __instance) =>
            FishermanWinchControls.InvalidateInventory(component: __instance);
    }
}
