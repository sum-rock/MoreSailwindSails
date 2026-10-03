using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Scopes shipyard patch behavior to the spritsail family.

    [HarmonyPatch(typeof(Shipyard), "Awake")]
    internal static class LooseFootedSpritsailShipyardPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Shipyard __instance) =>
            LooseFootedSpritsail.AddToShipyard(shipyard: __instance);
    }
}
