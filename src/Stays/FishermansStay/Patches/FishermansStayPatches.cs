using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Stays.FishermansStay.Patches
{
    [HarmonyPatch(typeof(SaveableBoatCustomization), "Awake")]
    internal static class FishermansStayRegistrationPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(SaveableBoatCustomization __instance) =>
            FishermansStayRegistry.Register(__instance);
    }

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

    [HarmonyPatch(typeof(BoatCustomParts), "RefreshParts")]
    internal static class FishermansStayRefreshPatch
    {
        [HarmonyPostfix]
        internal static void Postfix(BoatCustomParts __instance)
        {
            var registry = __instance.GetComponent<FishermansStayRegistry>();
            if (!registry)
                return;
            registry.Refresh();
        }
    }

    [HarmonyPatch(typeof(BoatCustomParts), "RefreshPartsWithOrder")]
    internal static class FishermansStayOrderRefreshPatch
    {
        [HarmonyPrefix]
        private static void Prefix(BoatCustomParts __instance, out bool __state)
        {
            __state = false;
            var registry = __instance.GetComponent<FishermansStayRegistry>();
            if (registry)
            {
                __state = registry.PreviewingOrder;
                registry.PreviewingOrder = true;
            }
        }

        [HarmonyPostfix]
        private static void Postfix(BoatCustomParts __instance) =>
            FishermansStayRefreshPatch.Postfix(__instance);

        [HarmonyFinalizer]
        private static void Finalizer(BoatCustomParts __instance, bool __state)
        {
            var registry = __instance.GetComponent<FishermansStayRegistry>();
            if (registry)
                registry.PreviewingOrder = __state;
        }
    }

    [HarmonyPatch(typeof(BoatPart), "SetOptionEnabled")]
    internal static class FishermansStayOccupiedPreviewPatch
    {
        [HarmonyPrefix]
        internal static void Prefix(BoatPart __instance, int i, ref bool state)
        {
            if (state || i != __instance.activeOption)
                return;
            var option = __instance.partOptions[i];
            var registry = option.GetComponentInParent<FishermansStayRegistry>();
            if (!registry || !registry.PreviewingOrder)
                return;
            if (!registry.Protects(option))
                return;
            // Keep both the occupied mount and its walking collision root alive.
            // Native CanUninstall still rejects the requested change, but its
            // sail's collision check can finish and the order remains editable.
            state = true;
        }
    }

    [HarmonyPatch(typeof(BoatCustomParts), "CanInstall")]
    internal static class FishermansStayCanInstallPatch
    {
        [HarmonyPrefix]
        private static void Prefix(BoatCustomParts __instance, int partIndex, int optionIndex)
        {
            var registry = __instance.GetComponent<FishermansStayRegistry>();
            if (!registry)
                return;
            var option = __instance.availableParts[partIndex].partOptions[optionIndex];
            registry.Stays.FirstOrDefault(s => s.Option == option)?.Refresh();
        }

        [HarmonyPostfix]
        private static void Postfix(
            BoatCustomParts __instance,
            int partIndex,
            int optionIndex,
            ref bool __result,
            ref string requiredOptionNames
        )
        {
            var registry = __instance.GetComponent<FishermansStayRegistry>();
            if (!registry)
                return;
            var option = __instance.availableParts[partIndex].partOptions[optionIndex];
            var stay = registry.Stays.FirstOrDefault(s => s.Option == option);
            var conflicts = registry
                .Stays.Where(s =>
                    s.Option.gameObject.activeInHierarchy
                    && s.Option.requiresDisabled.Contains(option)
                )
                .Select(s => s.Option.optionName)
                .Concat(
                    stay == null
                        ? Enumerable.Empty<string>()
                        : stay
                            .Option.requiresDisabled.Where(o => o && o.gameObject.activeInHierarchy)
                            .Select(o => o.optionName)
                )
                .Distinct()
                .ToArray();
            if (conflicts.Length > 0)
            {
                __result = false;
                requiredOptionNames = ConflictText(names: conflicts);
                return;
            }
            if (stay == null)
                return;
            if (!stay.Available)
            {
                __result = false;
                requiredOptionNames = "requires: matching active masts and halyard guide.";
            }
        }

        // Native UpdateOrder marks the option red and sends this text to its hover help.
        private static string ConflictText(string[] names) =>
            "<color=red>incompatible with: " + string.Join(", ", names) + ".</color>";
    }
}
