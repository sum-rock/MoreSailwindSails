using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Scopes shipyard fallback patch behavior to the spritsail family.

    [HarmonyPatch(typeof(Shipyard), "ActivateDocuments")]
    internal static class LooseFootedSpritsailShipyardFallbackPatch
    {
        // Covers shipyards that awakened before PrefabsDirectory.Start.
        [HarmonyPrefix]
        private static void Prefix(Shipyard __instance) =>
            LooseFootedSpritsail.AddToShipyard(shipyard: __instance);
    }
}
