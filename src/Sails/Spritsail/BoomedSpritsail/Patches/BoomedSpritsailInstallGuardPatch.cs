using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.Patches
{
    // Scopes install guard patch behavior to the spritsail family.

    [HarmonyPatch(typeof(ShipyardSailInstaller), "InstallSail")]
    internal static class BoomedSpritsailInstallGuardPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Mast mast, Sail sail)
        {
            if (!sail.GetComponent<BoomedSpritsailRig>())
                return true;
            string error = BoomedSpritsailRigging.InstallError(sail: sail, mast: mast);
            if (error == null)
                return true;
            Plugin.Log.LogWarning(data: "BoomedSpritsail installation rejected: " + error);
            return false;
        }
    }
}
