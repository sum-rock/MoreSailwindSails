using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Adds all registered spritsail types after native shipyard initialization.

    [HarmonyPatch(typeof(Shipyard), "Awake")]
    internal static class SpritsailShipyardPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Shipyard __instance) =>
            SpritsailCatalog.AddToShipyard(shipyard: __instance);
    }
}
