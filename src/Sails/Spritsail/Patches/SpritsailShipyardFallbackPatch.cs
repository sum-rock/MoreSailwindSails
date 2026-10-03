using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Adds all registered spritsail types when opening an already initialized shipyard.

    [HarmonyPatch(typeof(Shipyard), "ActivateDocuments")]
    internal static class SpritsailShipyardFallbackPatch
    {
        // Covers shipyards that awakened before PrefabsDirectory.Start.
        [HarmonyPrefix]
        private static void Prefix(Shipyard __instance) =>
            SpritsailCatalog.AddToShipyard(shipyard: __instance);
    }
}
