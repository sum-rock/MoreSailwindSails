// Owns Gloriana's authored stay variants and attachment measurements.
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay.Configuration.Boats
{
    internal static class GlorianaStays
    {
        internal static readonly FishermansStayDefinition Definition = new FishermansStayDefinition(
            boat: BoatConfiguration.Boats.Gloriana.Definition,
            stays: Stays()
        );

        // Endpoints use installed spar frames and native gaff-guide heights.
        private static FishermansStayGroupDefinition[] Stays() =>
            new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast",
                    variants: new[]
                    {
                        // 67.195 degrees; meets the rendered foremast tip.
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 5,
                            label: "Foremast / Mainmast",
                            fore: 1,
                            forePoint: new Vector3(0f, 0f, 1.60888767f),
                            aft: 2,
                            aftPoint: new Vector3(0f, 0f, 0.865f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 1, 2 },
                            forbidden: new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Mainmast / mizzenmast",
                    variants: new[]
                    {
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 6,
                            label: "Mainmast / Mizenmast Main",
                            fore: 2,
                            forePoint: new Vector3(0f, 0f, -15.65632243f),
                            aft: 3,
                            aftPoint: new Vector3(0f, 0f, 1.269f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 3 },
                            forbidden: new[] { 4 }
                        ),
                        new FishermansStayVariantDefinition(
                            mountIndex: 130,
                            donor: 6,
                            label: "Mainmast / Mizenmast Top",
                            fore: 2,
                            forePoint: new Vector3(0f, 0f, -9.09545912f),
                            aft: 4,
                            aftPoint: new Vector3(0f, 0f, 0.517f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 3, 4 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            };
    }
}
