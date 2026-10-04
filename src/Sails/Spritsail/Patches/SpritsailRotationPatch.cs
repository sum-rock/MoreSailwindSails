using HarmonyLib;
using ShipyardExpansion;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Prevents SE's root-rotation fallback from rotating registered spritsails, including on load.
    [HarmonyPatch(typeof(SailScaler), "SetAngle")]
    internal static class SpritsailRotationPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail ___sail) => !SpritsailCategory.IsSpritsail(sail: ___sail);
    }
}
