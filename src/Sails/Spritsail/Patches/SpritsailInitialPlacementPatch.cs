using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Places new spritsails with their lower edge at the mast base after make-specific sizing.
    [HarmonyPatch(typeof(ShipyardSailInstaller), "AddNewSail")]
    internal static class SpritsailInitialPlacementPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(ShipyardSailInstaller __instance, GameObject sailObject)
        {
            var sail = sailObject.GetComponent<Sail>();
            if (!SpritsailCategory.IsSpritsail(sail: sail))
                return;
            // Native installation height locates the sail's head; zero would bury its foot.
            sail.ChangeInstallHeight(
                changeValue: sail.GetScaledHeight() - sail.GetCurrentInstallHeight()
            );
            // Refresh position, controller attachments, ordering and collision checks natively.
            __instance.MoveHeldSail(distance: 0f);
            ShipyardUI.instance.UpdateDescriptionText();
        }
    }
}
