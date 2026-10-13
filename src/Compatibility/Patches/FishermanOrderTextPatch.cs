// Protects all Fisherman sail and stay order text before optional NANDFixes wrapping.
using System.Collections.Generic;
using HarmonyLib;

namespace MoreSailwindSails.Compatibility.Patches
{
    // Wraps feature order lines before NANDFixes can recurse on long prefixes.
    [HarmonyPatch(typeof(ShipyardUIOrderText), "AddLine")]
    internal static class FishermanOrderTextPatch
    {
        [HarmonyPrefix]
        [HarmonyBefore("com.nandbrew.nandfixes")]
        internal static bool Prefix(ref string line, List<string> ___lines)
        {
            if (!FishermanOrderText.NeedsWrapping(line))
                return true;
            // NANDFixes 1.4.4 recursively calls AddLine with the prefix ending
            // in "->". When that prefix exceeds 45 characters it calls itself
            // with identical input forever. Append our wrapped lines directly
            // to the native list. HarmonyX still runs later prefixes even when
            // we return false, so consume their input as well. The original
            // AddLine is skipped; no blank line or recursive call is added.
            ___lines.AddRange(FishermanOrderText.Wrap(line));
            line = string.Empty;
            return false;
        }
    }
}
