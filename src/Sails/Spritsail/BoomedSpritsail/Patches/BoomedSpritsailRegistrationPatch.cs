using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.Patches
{
    // Scopes registration patch behavior to the spritsail family.
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
