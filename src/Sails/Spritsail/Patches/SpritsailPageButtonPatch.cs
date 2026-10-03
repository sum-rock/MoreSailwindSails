using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Handles family page buttons and resets category selection without intercepting other controls.
    [HarmonyPatch(typeof(ShipyardButton), "OnActivate")]
    internal static class SpritsailPageButtonPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ShipyardButton __instance)
        {
            var page = __instance.GetComponent<SpritsailPageButton>();
            if (page)
            {
                page.Menu.ChangePage(direction: page.Direction);
                return false;
            }
            if (
                __instance.function == ShipyardButton.ButtonFunction.selectSailCategory
                && __instance.index == SpritsailRules.CategoryId
            )
            {
                var menu = ShipyardUI.instance.GetComponent<SpritsailShipyardMenu>();
                if (menu)
                    menu.ResetPage();
            }
            return true;
        }
    }
}
