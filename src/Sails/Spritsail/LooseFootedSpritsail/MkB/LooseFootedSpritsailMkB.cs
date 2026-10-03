using System;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.MkB
{
    // Stretches the 135-degree/1.25 base cut horizontally by 30% without changing corner heights.
    internal static class LooseFootedSpritsailMkB
    {
        internal const int PrefabIndex = 405;
        internal const string DisplayName = "Loose-footed Spritsail Mk.B";
        internal const float ThroatAngleDegrees = 135;
        internal const float HorizontalScale = 1.3f;
        internal const float FootToHeadRatio = 1.25f;
        private static readonly float HeadLength =
            (float)Math.Sqrt(1 + 0.08f * 0.08f) / FootToHeadRatio;
        internal static readonly LooseFootedSpritsailDefinition Definition =
            new LooseFootedSpritsailDefinition(
                prefabIndex: PrefabIndex,
                displayName: DisplayName,
                peakReach: HeadLength * (float)Math.Cos((ThroatAngleDegrees - 90) * Math.PI / 180),
                peakRise: HeadLength * (float)Math.Sin((ThroatAngleDegrees - 90) * Math.PI / 180),
                horizontalScale: HorizontalScale
            );
    }
}
