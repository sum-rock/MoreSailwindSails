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
                            foreCollarRadius: 0.15315f,
                            foreCollarOffset: new Vector3(0.00000f, 0.00000f, -0.65099f),
                            aft: 2,
                            aftPoint: new Vector3(0f, 0f, 0.865f),
                            aftCollarRadius: 0.15371f,
                            aftCollarOffset: new Vector3(-0.00027f, 0.00000f, 0.00000f),
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
                            foreCollarRadius: 0.22231f,
                            foreCollarOffset: new Vector3(-0.00684f, 0.00000f, 0.00000f),
                            aft: 3,
                            aftPoint: new Vector3(0f, 0f, 1.269f),
                            aftCollarRadius: 0.17794f,
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
                            foreCollarRadius: 0.20147f,
                            foreCollarOffset: new Vector3(-0.01064f, 0.00000f, 0.00000f),
                            aft: 4,
                            aftPoint: new Vector3(0f, 0f, 0.517f),
                            aftCollarRadius: 0.07557f,
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
