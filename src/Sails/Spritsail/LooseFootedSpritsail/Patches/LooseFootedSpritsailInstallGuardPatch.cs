using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Scopes install guard patch behavior to the spritsail family.

    [HarmonyPatch(typeof(ShipyardSailInstaller), "InstallSail")]
    internal static class LooseFootedSpritsailInstallGuardPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Mast mast, Sail sail)
        {
            if (!sail.GetComponent<LooseFootedSpritsailRig>())
                return true;
            string error = LooseFootedSpritsailRigging.InstallError(sail: sail, mast: mast);
            if (error == null)
                return true;
            Plugin.Log.LogWarning(data: "LooseFootedSpritsail installation rejected: " + error);
            return false;
        }
    }
}
