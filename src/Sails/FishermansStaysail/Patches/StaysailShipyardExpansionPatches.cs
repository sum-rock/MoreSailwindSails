// Groups Staysail patches for Shipyard Expansion APIs and supporting native hooks.
using HarmonyLib;
using MoreSailwindSails.Sails.FishermansStaysail.MkA;
using MoreSailwindSails.Sails.FishermansStaysail.MkB;
using MoreSailwindSails.Sails.FishermansStaysail.MkC;
using ShipyardExpansion;
using ShipyardExpansion.Scripts;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail.Patches
{
    // Keeps custom sails on the plain texture in Shipyard Expansion material updates.
    [HarmonyPatch(typeof(SailTextureChanger), "UpdateMaterial")]
    internal static class FishermansStaysailPlainTexturePatch
    {
        [HarmonyPrefix]
        private static void Prefix(SailTextureChanger __instance)
        {
            if (__instance.GetComponent<FishermansStaysailRig>())
                // Covers saved patterns, SetTexture and NextTexture using the
                // original material update and the existing plain texture.
                __instance.textureIndex = FishermansStaysailAppearance.PlainTextureName;
        }
    }

    // Hides Shipyard Expansion pattern controls for custom sails.
    [HarmonyPatch(typeof(ShipyardUI), "UpdateMoveButtons")]
    internal static class FishermansStaysailTextureButtonPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(GameObject ___moveUpButton)
        {
            var shipyard = GameState.currentShipyard;
            var sail = shipyard ? shipyard.sailInstaller.GetCurrentSail() : null;
            if (!sail || !sail.GetComponent<FishermansStaysailRig>())
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

    // Configures Shipyard Expansion scaling and fitting controls for staysails.
    [HarmonyPatch(typeof(SailScaler), "Awake")]
    internal static class FishermansStaysailScalerPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SailScaler __instance)
        {
            if (!__instance.GetComponent<FishermansStaysailRig>())
                return;
            __instance.scaleType = ScaleType.Uniform;
            __instance.flippable = false;
        }
    }

    // Applies initial custom sail size and placement after Shipyard Expansion initialization.
    [HarmonyPatch(typeof(ShipyardSailInstaller), "AddNewSail")]
    internal static class FishermansStaysailNewSailPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(ShipyardSailInstaller __instance, GameObject sailObject)
        {
            var rig = sailObject.GetComponent<FishermansStaysailRig>();
            if (!rig)
                return;
            var mast = __instance.GetCurrentMast();
            var sail = rig.Sail;
            // Native AddNewSail chooses the shipyard's first palette entry.
            // Use the existing white swatch for this sail's initial selection.
            sail.ChangeSailColor(FishermansStaysailAppearance.WhiteColorIndex);
            // Only new shipyard selections get the smaller initial size.
            // Loaded sails continue to restore their saved SE dimensions.
            sail.GetComponent<SailScaler>().SetScaleAbs(0.5f, 0.5f);
            sail.ChangeInstallHeight(mast.mastHeight - sail.GetCurrentInstallHeight());
            sail.UpdateInstallPosition();
            sail.currentUnroll = 1f;
            rig.RefreshFrame();
            mast.UpdateControllerAttachments();
            ShipyardUI.instance.UpdateDescriptionText();
            GameState.currentShipyard.UpdateOrder();
        }
    }

    // Registers custom prefabs after Shipyard Expansion prepares their source components.
    [HarmonyPatch(typeof(PrefabsDirectory), "Start")]
    internal static class FishermansStaysailRegistrationPatch
    {
        // Clone after SE has configured the source components, but before
        // All Sails captures the prefab array for its cached menu pages.
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        [HarmonyBefore("NatoriusG.AllSailsAllShipyards")]
        private static void Postfix(PrefabsDirectory __instance)
        {
            FishermansStaysailMkA.Register(__instance);
            FishermansStaysailMkB.Register(__instance);
            FishermansStaysailMkC.Register(__instance);
        }
    }
}
