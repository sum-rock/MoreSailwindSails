// Groups FlyingSail patches for Shipyard Expansion APIs and supporting native hooks.
using HarmonyLib;
using ShipyardExpansion;
using ShipyardExpansion.Scripts;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Patches
{
    // Keeps custom sails on the plain texture in Shipyard Expansion material updates.
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

    // Hides Shipyard Expansion pattern controls for custom sails.
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

    // Applies initial custom sail size and placement after Shipyard Expansion initialization.
    [HarmonyPatch(typeof(ShipyardSailInstaller), "AddNewSail")]
    internal static class FishermansFlyingSailNewSailPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(ShipyardSailInstaller __instance, GameObject sailObject)
        {
            var rig = sailObject.GetComponent<FishermansFlyingSailRig>();
            if (!rig)
                return;
            var mast = __instance.GetCurrentMast();
            var sail = rig.Sail;
            // Native AddNewSail chooses the shipyard's first palette entry.
            // Use the existing white swatch for this sail's initial selection.
            sail.ChangeSailColor(FishermansFlyingSailAppearance.WhiteColorIndex);
            // Start at 100% of the smaller base mesh.
            sail.GetComponent<SailScaler>().SetScaleAbs(1f, 1f);
            sail.ChangeInstallHeight(mast.mastHeight - sail.GetCurrentInstallHeight());
            sail.UpdateInstallPosition();
            sail.currentUnroll = 1f;
            rig.RefreshFlyingFrame();
            mast.UpdateControllerAttachments();
            ShipyardUI.instance.UpdateDescriptionText();
            GameState.currentShipyard.UpdateOrder();
        }
    }

    // Registers custom prefabs after Shipyard Expansion prepares their source components.
    [HarmonyPatch(typeof(PrefabsDirectory), "Start")]
    internal static class FishermansFlyingSailRegistrationPatch
    {
        // Clone after SE has configured the source components, but before
        // All Sails captures the prefab array for its cached menu pages.
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        [HarmonyBefore("NatoriusG.AllSailsAllShipyards")]
        private static void Postfix(PrefabsDirectory __instance) =>
            FishermansFlyingSail.Register(__instance);
    }
}
