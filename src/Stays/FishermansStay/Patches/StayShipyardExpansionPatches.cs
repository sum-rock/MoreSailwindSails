// Groups Stay patches for Shipyard Expansion APIs and supporting native hooks.
using System;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Stays.FishermansStay.Patches
{
    // Registers custom stay mounts after Shipyard Expansion initializes boat customization.
    [HarmonyPatch(typeof(SaveableBoatCustomization), "Awake")]
    internal static class FishermansStayRegistrationPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(SaveableBoatCustomization __instance) =>
            FishermansStayRegistry.Register(__instance);
    }

    // Extends saved mast slots while preserving existing customization data.
    [HarmonyPatch(typeof(SaveBoatCustomizationData), MethodType.Constructor)]
    internal static class FishermansStaySaveCapacityPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(SaveBoatCustomizationData __instance)
        {
            if (__instance.masts.Length < FishermansStayGeometry.MountCapacity)
                Array.Resize(ref __instance.masts, FishermansStayGeometry.MountCapacity);
        }
    }

    // Prepares stay snapshots and mast capacity before Shipyard Expansion loads them.
    [HarmonyPatch(typeof(SaveableBoatCustomization), "LoadData")]
    internal static class FishermansStayLoadPatch
    {
        [HarmonyPrefix]
        [HarmonyBefore("com.nandbrew.shipyardexpansion")]
        private static void Prefix(
            SaveableBoatCustomization __instance,
            SaveBoatCustomizationData data
        )
        {
            var registry = __instance.GetComponent<FishermansStayRegistry>();
            if (!registry || data == null)
                return;
            var refs = __instance.GetComponent<BoatRefs>();
            // LoadData removes existing sails using this array's length, including
            // when a shipyard cancellation restores an older customization snapshot.
            if (data.masts == null || data.masts.Length < refs.masts.Length)
                Array.Resize(ref data.masts, refs.masts.Length);
            registry.PrepareSnapshot(data.partActiveOptions);
            registry.Refresh();
        }
    }

    // Extends Shipyard Expansion mast buttons for the custom stay mount range.
    [HarmonyPatch(typeof(ShipyardUI), "Awake")]
    internal static class FishermansStayMountButtonsPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(ref GameObject[] ___mastButtons)
        {
            // Shipyard Expansion supplies the first 128 buttons. Add the same
            // native buttons for the reserved fisherman mount range.
            if (___mastButtons == null || ___mastButtons.Length == 0 || !___mastButtons[0])
                return;
            int start = ___mastButtons.Length;
            if (start >= FishermansStayGeometry.MountCapacity)
                return;
            Array.Resize(ref ___mastButtons, FishermansStayGeometry.MountCapacity);
            for (int i = start; i < ___mastButtons.Length; i++)
            {
                var button = Object.Instantiate(
                    ___mastButtons[0],
                    ___mastButtons[0].transform.parent,
                    false
                );
                button.name = $"FishermansStay mount button {i}";
                button.GetComponent<ShipyardButton>().index = i;
                button.SetActive(false);
                ___mastButtons[i] = button;
            }
        }
    }
}
