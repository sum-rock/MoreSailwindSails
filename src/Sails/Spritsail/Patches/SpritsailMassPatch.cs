using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Applies spritsail boat mass and the boom premium independently of propulsion tuning.
    [HarmonyPatch(typeof(BoatMass), "GetSailMass")]
    internal static class SpritsailMassPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail sail, ref float __result)
        {
            if (!SpritsailCategory.IsSpritsail(sail: sail))
                return true;
            __result = SpritsailRules.Mass(
                realPower: sail.GetRealSailPower(),
                prefabIndex: sail.prefabIndex
            );
            return false;
        }
    }
}
