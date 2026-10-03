using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Applies the family price without changing sail area or other categories.
    [HarmonyPatch(typeof(Sail), "GetSailPrice")]
    internal static class SpritsailPricePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail __instance, ref float __result)
        {
            if (!SpritsailCategory.IsSpritsail(sail: __instance))
                return true;
            __result = SpritsailRules.Price(
                area: __instance.GetSailArea(),
                prefabIndex: __instance.prefabIndex
            );
            return false;
        }
    }
}
