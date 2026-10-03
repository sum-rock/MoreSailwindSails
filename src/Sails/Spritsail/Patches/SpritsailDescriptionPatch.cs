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
                "Spritsails are supported by a diagonal spar.\n"
                + "Loose-footed sails use port and starboard sheets;\n"
                + "boomed sails use a single sheet at the boom's end.\n"
                + "Reefing lifts the spars and gathers cloth at the mast.\n"
                + "Starboard tack performs worse than port tack.";
            return false;
        }
    }
}
