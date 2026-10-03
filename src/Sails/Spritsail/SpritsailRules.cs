using System;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Defines independently testable category balance and browsing rules.
    internal static class SpritsailRules
    {
        internal const int CategoryId = 6;
        internal const float PriceMultiplier = 1.29f;
        internal const float DefaultForceMultiplier = 0.75f;
        internal const float MassPerPower = 40f;

        internal static float ValidForceMultiplier(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) || value < 0
                ? DefaultForceMultiplier
                : value;

        internal static float Price(float area, int prefabIndex = -1) =>
            area * 9f * PriceMultiplier * (IsBoomed(prefabIndex: prefabIndex) ? 1.1f : 1f);

        internal static float Mass(float realPower, int prefabIndex = -1) =>
            realPower * MassPerPower * (IsBoomed(prefabIndex: prefabIndex) ? 1.2f : 1f);

        private static bool IsBoomed(int prefabIndex) =>
            prefabIndex == BoomedSpritsail.MkA.BoomedSpritsailMkA.PrefabIndex
            || prefabIndex == BoomedSpritsail.MkB.BoomedSpritsailMkB.PrefabIndex;

        internal static int OverlapCategory(int category) => category == CategoryId ? 3 : category;

        internal static int PageCount(int count, int size) =>
            size <= 0 ? 0 : count / size + (count % size > 0 ? 1 : 0);

        internal static int ClampPage(int page, int count, int size) =>
            Math.Max(0, Math.Min(page, PageCount(count: count, size: size) - 1));
    }
}
