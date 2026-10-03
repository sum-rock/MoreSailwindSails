using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShipyardExpansion;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.Patches
{
    // Scopes install error patch behavior to the spritsail family.

    [HarmonyPatch(typeof(ShipyardSailInstaller), "GetInstallError")]
    internal static class BoomedSpritsailInstallErrorPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail sail, Mast mast, ref bool error, ref string __result)
        {
            if (!sail.GetComponent<BoomedSpritsailRig>())
                return true;
            string reason = BoomedSpritsailRigging.InstallError(sail: sail, mast: mast);
            if (reason == null)
                return true;
            error = true;
            __result = reason;
            return false;
        }
    }
}
