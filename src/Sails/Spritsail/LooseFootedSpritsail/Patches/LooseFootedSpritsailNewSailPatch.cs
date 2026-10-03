using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Scopes new sail patch behavior to the spritsail family.

    [HarmonyPatch(typeof(ShipyardSailInstaller), "AddNewSail")]
    internal static class LooseFootedSpritsailNewSailPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(ShipyardSailInstaller __instance, GameObject sailObject)
        {
            var rig = sailObject.GetComponent<LooseFootedSpritsailRig>();
            if (!rig)
                return;
            var mast = __instance.GetCurrentMast();
            var sail = rig.Sail;
            // Native AddNewSail chooses the shipyard's first palette entry.
            // Use the existing white swatch for this sail's initial selection.
            sail.ChangeSailColor(newColor: LooseFootedSpritsailAppearance.WhiteColorIndex);
            // Start at 100% of the smaller base mesh.
            sail.GetComponent<SailScaler>().SetScaleAbs(width: 1f, height: 1f);
            // The family-level final postfix places every make at the mast base after sizing.
            sail.currentUnroll = 1f;
            rig.RefreshMastFrame();
            mast.UpdateControllerAttachments();
            ShipyardUI.instance.UpdateDescriptionText();
            GameState.currentShipyard.UpdateOrder();
        }
    }
}
