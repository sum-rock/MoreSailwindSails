using HarmonyLib;
using ShipyardExpansion.Scripts;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.Patches
{
    // Preserves plain-texture selection for boomed sails.
    [HarmonyPatch(typeof(SailTextureChanger), "UpdateMaterial")]
    internal static class BoomedSpritsailPlainTexturePatch
    {
        [HarmonyPrefix]
        private static void Prefix(SailTextureChanger __instance)
        {
            if (__instance.GetComponent<BoomedSpritsailRig>())
                // Covers saved patterns, SetTexture and NextTexture using the
                // original material update and the existing plain texture.
                __instance.textureIndex = BoomedSpritsailAppearance.PlainTextureIndex;
        }
    }
}
