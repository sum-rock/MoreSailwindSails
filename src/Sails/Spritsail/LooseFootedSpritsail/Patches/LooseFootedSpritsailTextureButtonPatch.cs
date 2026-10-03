using HarmonyLib;
using ShipyardExpansion.Scripts;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Restricts shipyard texture selection for loose-footed sails.

    [HarmonyPatch(typeof(ShipyardUI), "UpdateMoveButtons")]
    internal static class LooseFootedSpritsailTextureButtonPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(GameObject ___moveUpButton)
        {
            var shipyard = GameState.currentShipyard;
            var sail = shipyard ? shipyard.sailInstaller.GetCurrentSail() : null;
            if (!sail || !sail.GetComponent<LooseFootedSpritsailRig>())
                return;
            // SE restores the button for other sails on each refresh. Find the
            // component so either SE color-page layout is handled.
            foreach (
                var button in ___moveUpButton.transform.parent.GetComponentsInChildren<TextureButton>(
                    includeInactive: true
                )
            )
                button.gameObject.SetActive(value: false);
        }
    }
}
