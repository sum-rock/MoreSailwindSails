// Owns Caelanor's authored stay variants and attachment measurements.
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay.Configuration.Boats
{
    internal static class CaelanorStays
    {
        internal static readonly FishermansStayDefinition Definition = new FishermansStayDefinition(
            boat: BoatConfiguration.Boats.Caelanor.Definition,
            stays: new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast",
                    variants: new[]
                    {
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 14,
                            label: "Foremast Top / Mainmast Top",
                            fore: 2,
                            forePoint: new Vector3(0f, 0f, 1.0960133f),
                            aft: 6,
                            aftPoint: new Vector3(0f, 0f, 1.0960107f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 1, 2, 5, 6 },
                            forbidden: new int[0],
                            alignGuideHeightToAftAnchor: true
                        ),
                    }
                ),
            }
        );
    }
}
