// Groups Stay patches for native Sailwind behavior.
using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Stays.FishermansStay.Patches
{
    // Extends the native parts panel only when SE is not supplying its own pages.
    [HarmonyPatch(typeof(ShipyardUI), "RefreshPartsPanel")]
    internal static class FishermansStayPartsPagesPatch
    {
        [HarmonyPrefix]
        private static void Prefix(
            ShipyardUI __instance,
            int category,
            GameObject ___newPartsMenu,
            ref ShipyardButton[] ___partOptionsLeftButtons,
            ref ShipyardButton[] ___partOptionsRightButtons,
            ref TextMesh[] ___partOptionsTexts
        )
        {
            if (
                BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(
                    "com.nandbrew.shipyardexpansion"
                )
            )
                return;
            var shipyard = GameState.currentShipyard;
            var boat = shipyard ? shipyard.GetCurrentBoat() : null;
            var parts = boat ? boat.GetComponent<BoatCustomParts>() : null;
            if (!parts)
                return;
            var pages = __instance.GetComponent<FishermansStayPartsPages>();
            if (!pages)
            {
                if (
                    !boat.GetComponent<FishermansStayRegistry>()
                    || parts.availableParts.Count(p => p.category == category)
                        <= ___partOptionsTexts.Length
                )
                    return;
                pages = __instance.gameObject.AddComponent<FishermansStayPartsPages>();
            }
            pages.Prepare(
                parent: ___newPartsMenu.transform,
                parts: parts,
                requestedCategory: category,
                left: ref ___partOptionsLeftButtons,
                right: ref ___partOptionsRightButtons,
                texts: ref ___partOptionsTexts
            );
        }

        [HarmonyPostfix]
        private static void Postfix(ShipyardUI __instance)
        {
            var pages = __instance.GetComponent<FishermansStayPartsPages>();
            if (pages)
                pages.Show();
        }
    }

    // Routes owned page arrows without invoking the native change-part action.
    [HarmonyPatch(typeof(ShipyardButton), "OnActivate")]
    internal static class FishermansStayPageButtonPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ShipyardButton __instance)
        {
            var button = __instance.GetComponent<FishermansStayPageButton>();
            if (!button)
                return true;
            button.Owner.Show(delta: button.Delta);
            return false;
        }
    }

    // Refreshes registered stays after native boat parts change.
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

    // Refreshes feature state when native shipyard order previews change.
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

    // Keeps occupied stays and their supports active during order previews.
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

    // Rejects stay installation when supports are unavailable or options conflict.
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
            // Native CanInstall checks prerequisites, not BoatPartOption.canInstall.
            // Reserved unavailable options must remain visible but cannot be ordered.
            if (
                stay == null
                && optionIndex != 0
                && registry.Parts.Contains(__instance.availableParts[partIndex])
            )
            {
                __result = false;
                requiredOptionNames = "requires: rigging parts that are not installed.";
                return;
            }
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

    // Registers custom stay mounts after native and optional SE boat customization.
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

    // Prepares stay snapshots and mast capacity before native and optional SE loading.
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

    // Extends native mast buttons for the custom stay mount range.
    [HarmonyPatch(typeof(ShipyardUI), "Awake")]
    internal static class FishermansStayMountButtonsPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(ref GameObject[] ___mastButtons)
        {
            // Start from whichever native/optional-mod capacity is present.
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
