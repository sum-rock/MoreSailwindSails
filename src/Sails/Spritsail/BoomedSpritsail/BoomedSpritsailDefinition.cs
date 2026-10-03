using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail
{
    // Supplies only a mark's identity and planar cut; all marks share the same rig and controls.
    internal sealed class BoomedSpritsailDefinition
    {
        internal readonly int PrefabIndex;
        internal readonly string DisplayName;
        internal readonly float PeakReach,
            PeakRise,
            HorizontalScale;

        internal BoomedSpritsailDefinition(
            int prefabIndex,
            string displayName,
            float peakReach,
            float peakRise,
            float horizontalScale = 1
        )
        {
            PrefabIndex = prefabIndex;
            DisplayName = displayName;
            PeakReach = peakReach;
            PeakRise = peakRise;
            HorizontalScale = horizontalScale;
        }

        internal Vector3[] Corners(float width) =>
            new[]
            {
                new Vector3(0, 0, -width * HorizontalScale),
                new Vector3(width * PeakRise, 0, -width * HorizontalScale * (1 - PeakReach)),
                new Vector3(-width * 1.6f, 0, -width * HorizontalScale),
                new Vector3(-width * (1.6f - 0.08f), 0, 0),
            };
    }
}
