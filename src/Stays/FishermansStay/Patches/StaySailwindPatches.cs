// Groups Stay patches for native Sailwind behavior.
using System.Linq;
using HarmonyLib;

namespace MoreSailwindSails.Stays.FishermansStay.Patches
{
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
