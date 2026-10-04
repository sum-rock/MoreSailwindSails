using HarmonyLib;
using ShipyardExpansion.Scripts;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Patches
{
    [HarmonyPatch(typeof(SailTextureChanger), "UpdateMaterial")]
    internal static class FishermansFlyingSailPlainTexturePatch
    {
        [HarmonyPrefix]
        private static void Prefix(SailTextureChanger __instance)
        {
            if (__instance.GetComponent<FishermansFlyingSailRig>())
                // Covers saved patterns, SetTexture and NextTexture using the
                // original material update and the existing plain texture.
                __instance.textureIndex = FishermansFlyingSailAppearance.PlainTextureName;
        }
    }

    [HarmonyPatch(typeof(ShipyardUI), "UpdateMoveButtons")]
    internal static class FishermansFlyingSailTextureButtonPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(GameObject ___moveUpButton)
        {
            var shipyard = GameState.currentShipyard;
            var sail = shipyard ? shipyard.sailInstaller.GetCurrentSail() : null;
            if (!sail || !sail.GetComponent<FishermansFlyingSailRig>())
                return;
            // SE restores the button for other sails on each refresh. Find the
            // component so either SE color-page layout is handled.
            foreach (
                var button in ___moveUpButton.transform.parent.GetComponentsInChildren<TextureButton>(
                    true
                )
            )
                button.gameObject.SetActive(false);
        }
    }
}
