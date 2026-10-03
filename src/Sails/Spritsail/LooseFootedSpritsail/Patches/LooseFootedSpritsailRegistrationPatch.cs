using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Registers loose-footed templates after SE initialization and before All Sails caching.
    [HarmonyPatch(typeof(PrefabsDirectory), "Start")]
    internal static class LooseFootedSpritsailRegistrationPatch
    {
        // Clone after SE has configured the source components, but before
        // All Sails captures the prefab array for its cached menu pages.
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        [HarmonyBefore("NatoriusG.AllSailsAllShipyards")]
        private static void Postfix(PrefabsDirectory __instance) =>
            LooseFootedSpritsail.Register(directory: __instance);
    }
}
