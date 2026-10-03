using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Supplies the category description absent from the native enum switch.
    [HarmonyPatch(typeof(ShipyardUI), "ShowSailTypeDescription")]
    internal static class SpritsailDescriptionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(SailCategory category, TextMesh ___descText)
        {
            if (category != SpritsailCategory.Value || !SpritsailCatalog.HasMembers)
                return true;
            ___descText.text =
                "Spritsails are supported by a diagonal spar.\nLoose-footed sails use separate port and starboard\nclew sheets. The hoist control raises the sail and\nadjusts the spar's snotter along the mast.";
            return false;
        }
    }
}
