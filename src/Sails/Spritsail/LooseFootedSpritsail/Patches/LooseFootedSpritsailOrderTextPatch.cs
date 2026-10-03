using System.Collections.Generic;
using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Scopes order text patch behavior to the spritsail family.
    [HarmonyPatch(typeof(ShipyardUIOrderText), "AddLine")]
    internal static class LooseFootedSpritsailOrderTextPatch
    {
        [HarmonyPrefix]
        [HarmonyBefore("com.nandbrew.nandfixes")]
        internal static bool Prefix(ref string line, List<string> ___lines)
        {
            if (!LooseFootedSpritsailOrderText.NeedsWrapping(line: line))
                return true;
            // NANDFixes 1.4.3 recursively calls AddLine with the prefix ending
            // in "->". When that prefix exceeds 45 characters it calls itself
            // with identical input forever. Append our wrapped lines directly
            // to the native list. HarmonyX still runs later prefixes even when
            // we return false, so consume their input as well. The original
            // AddLine is skipped; no blank line or recursive call is added.
            ___lines.AddRange(collection: LooseFootedSpritsailOrderText.Wrap(line: line));
            line = string.Empty;
            return false;
        }
    }
}
