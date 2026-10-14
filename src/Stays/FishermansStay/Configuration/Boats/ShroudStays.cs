// Owns Shroud's authored stay variants and attachment measurements.
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay.Configuration.Boats
{
    internal static class ShroudStays
    {
        internal static readonly FishermansStayDefinition Definition = new FishermansStayDefinition(
            boat: BoatConfiguration.Boats.Shroud.Definition,
            stays: Stays()
        );

        // Authored from installed native and boat-mod assets.
        private static FishermansStayGroupDefinition[] Stays() =>
            new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 24,
                            label: "Foremast 1 / Mainmast 1",
                            fore: 6,
                            forePoint: new Vector3(0.00000f, -0.00682f, -1.26976f),
                            aft: 8,
                            aftPoint: new Vector3(0.00000f, -0.00928f, -0.42400f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 6, 8 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 24,
                            label: "Foremast 1 Tall / Mainmast 1",
                            fore: 5,
                            forePoint: new Vector3(-0.00291f, 0.00006f, -11.85458f),
                            aft: 8,
                            aftPoint: new Vector3(0.00000f, -0.00928f, -0.42400f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 5, 8 },
                            forbidden: new int[0]
                        ),
                        // 42.20 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 130,
                            donor: 24,
                            label: "Foremast 1 / Mainmast 1 Tall",
                            fore: 6,
                            forePoint: new Vector3(0.00000f, -0.00682f, 0.50000f),
                            aft: 7,
                            aftPoint: new Vector3(-0.00537f, -0.00391f, -0.46400f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 6, 7 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 131,
                            donor: 24,
                            label: "Foremast 1 Tall / Mainmast 1 Tall",
                            fore: 5,
                            forePoint: new Vector3(-0.00291f, 0.00006f, -2.44452f),
                            aft: 7,
                            aftPoint: new Vector3(-0.00537f, -0.00391f, -0.46400f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 5, 7 },
                            forbidden: new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Mainmast / mizzenmast",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 132,
                            donor: 25,
                            label: "Mainmast 1 / Mizzen 1",
                            fore: 8,
                            forePoint: new Vector3(0.00000f, -0.00928f, -6.67603f),
                            aft: 10,
                            aftPoint: new Vector3(0.00000f, -0.00365f, -0.51800f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 8, 10 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 133,
                            donor: 25,
                            label: "Mainmast 1 Tall / Mizzen 1",
                            fore: 7,
                            forePoint: new Vector3(-0.00537f, -0.00391f, -16.12609f),
                            aft: 10,
                            aftPoint: new Vector3(0.00000f, -0.00365f, -0.51800f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 7, 10 },
                            forbidden: new int[0]
                        ),
                        // 55.88 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 134,
                            donor: 25,
                            label: "Mainmast 1 / Mizzenmast 1 Tall",
                            fore: 8,
                            forePoint: new Vector3(0.00000f, -0.00928f, 0.50000f),
                            aft: 9,
                            aftPoint: new Vector3(0.00025f, -0.00391f, -0.12789f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 8, 9 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 135,
                            donor: 25,
                            label: "Mainmast 1 Tall / Mizzenmast 1 Tall",
                            fore: 7,
                            forePoint: new Vector3(-0.00537f, -0.00391f, -5.67900f),
                            aft: 9,
                            aftPoint: new Vector3(0.00025f, -0.00391f, -0.12789f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 7, 9 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            };
    }
}
