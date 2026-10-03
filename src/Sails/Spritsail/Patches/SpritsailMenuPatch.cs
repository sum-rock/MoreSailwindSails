using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Adds the family menu lazily after native and optional-mod UI construction.
    [HarmonyPatch(typeof(ShipyardUI), "OpenSailCategoryMenu")]
    internal static class SpritsailMenuPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            ShipyardUI __instance,
            GameObject ___sailCategoryMenu,
            GameObject ___addSailMenu,
            GameObject[] ___addSailButtons
        )
        {
            if (!SpritsailCatalog.HasMembers)
                return;
            SpritsailShipyardMenu
                .Ensure(
                    ui: __instance,
                    categoryMenu: ___sailCategoryMenu,
                    addMenu: ___addSailMenu,
                    buttons: ___addSailButtons
                )
                .ResetPage();
        }
    }
}
