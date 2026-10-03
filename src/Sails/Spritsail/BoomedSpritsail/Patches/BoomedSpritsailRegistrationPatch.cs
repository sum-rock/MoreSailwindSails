using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.Patches
{
    // Registers boomed templates after SE initialization and before All Sails caching.
    [HarmonyPatch(typeof(PrefabsDirectory), "Start")]
    internal static class BoomedSpritsailRegistrationPatch
    {
        // Clone after SE has configured the source components, but before
        // All Sails captures the prefab array for its cached menu pages.
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        [HarmonyBefore("NatoriusG.AllSailsAllShipyards")]
        private static void Postfix(PrefabsDirectory __instance) =>
            BoomedSpritsail.Register(directory: __instance);
    }
}
