using System;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.MkA
{
    // Preserves the original Mk.A identity and 105-degree, full-span head.
    internal static class LooseFootedSpritsailMkA
    {
        internal const int PrefabIndex = 404;
        internal const string DisplayName = "Loose-footed Spritsail Mk.A";
        internal const float ThroatAngleDegrees = 105;
        internal const float PeakReach = 1;
        internal static readonly float PeakRise =
            PeakReach * (float)Math.Tan((ThroatAngleDegrees - 90) * Math.PI / 180);
        internal static readonly LooseFootedSpritsailDefinition Definition =
            new LooseFootedSpritsailDefinition(
                prefabIndex: PrefabIndex,
                displayName: DisplayName,
                peakReach: PeakReach,
                peakRise: PeakRise
            );
    }
}
