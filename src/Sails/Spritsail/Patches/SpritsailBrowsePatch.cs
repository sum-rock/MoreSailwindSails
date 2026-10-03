using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Seeds optional pages before All Sails handles browsing, or renders the native family pager.
    [HarmonyPatch(typeof(ShipyardUI), "ShowNewSailButtons")]
    internal static class SpritsailBrowsePatch
    {
        [HarmonyPrefix]
        [HarmonyBefore(SpritsailAllSails.HarmonyId)]
        private static bool Prefix(
            ShipyardUI __instance,
            SailCategory category,
            GameObject ___sailCategoryMenu,
            GameObject ___addSailMenu,
            GameObject[] ___addSailButtons
        )
        {
            var menu = __instance.GetComponent<SpritsailShipyardMenu>();
            if (menu)
                menu.HidePager();
            if (category != SpritsailCategory.Value || !SpritsailCatalog.HasMembers)
                return true;
            if (SpritsailAllSails.Prepare(reset: false))
                return true;
            menu = SpritsailShipyardMenu.Ensure(
                ui: __instance,
                categoryMenu: ___sailCategoryMenu,
                addMenu: ___addSailMenu,
                buttons: ___addSailButtons
            );
            menu.ShowNative();
            return false;
        }
    }
}
