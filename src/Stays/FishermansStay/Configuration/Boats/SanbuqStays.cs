// Owns Sanbuq's authored stay variants and attachment measurements.
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay.Configuration.Boats
{
    internal static class SanbuqStays
    {
        internal static readonly FishermansStayDefinition Definition = new FishermansStayDefinition(
            boat: BoatConfiguration.Boats.Sanbuq.Definition,
            stays: Stays()
        );

        // Authored from installed native and boat-mod assets.
        private static FishermansStayGroupDefinition[] Stays() =>
            new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Mainmast / mizzenmast",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 9,
                            label: "mast 1 / mizzen mast",
                            fore: 10,
                            forePoint: new Vector3(0.00116f, 0.00513f, -4.94033f),
                            aft: 12,
                            aftPoint: new Vector3(0.00116f, 0.00513f, 1.48931f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 10, 12 },
                            forbidden: new[] { 13, 80 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 9,
                            label: "mast 1 / mizzen topmast 3",
                            fore: 10,
                            forePoint: new Vector3(0.00116f, 0.00513f, 0.97319f),
                            aft: 80,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.38279f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 10, 12, 80 },
                            forbidden: new[] { 13 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 130,
                            donor: 9,
                            label: "topmast 1 / mizzen mast",
                            fore: 10,
                            forePoint: new Vector3(0.00116f, 0.00513f, -4.94033f),
                            aft: 12,
                            aftPoint: new Vector3(0.00116f, 0.00513f, 1.48931f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 10, 12, 13 },
                            forbidden: new[] { 80 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 131,
                            donor: 9,
                            label: "topmast 1 / mizzen topmast 3",
                            fore: 13,
                            forePoint: new Vector3(-0.00000f, 0.00000f, -8.42753f),
                            aft: 80,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.38279f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 10, 12, 13, 80 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 132,
                            donor: 54,
                            label: "mast 1 / mizzen mast 2",
                            fore: 10,
                            forePoint: new Vector3(0.00116f, 0.00513f, -2.46726f),
                            aft: 55,
                            aftPoint: new Vector3(0.00116f, 0.00513f, 2.18743f),
                            extendedGuide: false,
                            guideIndex: 2,
                            required: new[] { 10, 55 },
                            forbidden: new[] { 13, 59 }
                        ),
                        // 63.23 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 133,
                            donor: 54,
                            label: "mast 1 / mizzen topmast 1",
                            fore: 10,
                            forePoint: new Vector3(0.00116f, 0.00513f, 3.43000f),
                            aft: 59,
                            aftPoint: new Vector3(0.00000f, 0.00000f, 0.37763f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 10, 55, 59 },
                            forbidden: new[] { 13 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 134,
                            donor: 54,
                            label: "topmast 1 / mizzen mast 2",
                            fore: 10,
                            forePoint: new Vector3(0.00116f, 0.00513f, -2.46726f),
                            aft: 55,
                            aftPoint: new Vector3(0.00116f, 0.00513f, 2.18743f),
                            extendedGuide: false,
                            guideIndex: 2,
                            required: new[] { 10, 13, 55 },
                            forbidden: new[] { 59 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 135,
                            donor: 54,
                            label: "topmast 1 / mizzen topmast 1",
                            fore: 13,
                            forePoint: new Vector3(0.00000f, 0.00000f, -4.27195f),
                            aft: 59,
                            aftPoint: new Vector3(0.00000f, 0.00000f, 0.37763f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 10, 13, 55, 59 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 136,
                            donor: 66,
                            label: "mast 2 / mizzen mast 2",
                            fore: 11,
                            forePoint: new Vector3(0.00116f, 0.00513f, -0.70023f),
                            aft: 55,
                            aftPoint: new Vector3(0.00116f, 0.00513f, 2.18743f),
                            extendedGuide: false,
                            guideIndex: 2,
                            required: new[] { 11, 55 },
                            forbidden: new[] { 14, 59 }
                        ),
                        // 50.32 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 137,
                            donor: 66,
                            label: "mast 2 / mizzen topmast 1",
                            fore: 11,
                            forePoint: new Vector3(0.00116f, 0.00513f, 3.43000f),
                            aft: 59,
                            aftPoint: new Vector3(0.00000f, 0.00000f, 0.37763f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 55, 59 },
                            forbidden: new[] { 14 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 138,
                            donor: 66,
                            label: "topmast 2 / mizzen mast 2",
                            fore: 11,
                            forePoint: new Vector3(0.00116f, 0.00513f, -0.70023f),
                            aft: 55,
                            aftPoint: new Vector3(0.00116f, 0.00513f, 2.18743f),
                            extendedGuide: false,
                            guideIndex: 2,
                            required: new[] { 11, 14, 55 },
                            forbidden: new[] { 59 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 139,
                            donor: 66,
                            label: "topmast 2 / mizzen topmast 1",
                            fore: 14,
                            forePoint: new Vector3(0.00000f, 0.00000f, -2.50331f),
                            aft: 59,
                            aftPoint: new Vector3(0.00000f, 0.00000f, 0.37763f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 14, 55, 59 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 140,
                            donor: 61,
                            label: "mast 1 / mizzen mast 3",
                            fore: 10,
                            forePoint: new Vector3(0.00116f, 0.00513f, -1.37176f),
                            aft: 69,
                            aftPoint: new Vector3(0.00116f, 0.00513f, 2.18743f),
                            extendedGuide: false,
                            guideIndex: 2,
                            required: new[] { 10, 69 },
                            forbidden: new[] { 13, 70 }
                        ),
                        // 56.31 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 141,
                            donor: 61,
                            label: "mast 1 / mizzen topmast 2",
                            fore: 10,
                            forePoint: new Vector3(0.00116f, 0.00513f, 3.43000f),
                            aft: 70,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.38279f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 10, 69, 70 },
                            forbidden: new[] { 13 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 142,
                            donor: 61,
                            label: "topmast 1 / mizzen mast 3",
                            fore: 10,
                            forePoint: new Vector3(0.00116f, 0.00513f, -1.37176f),
                            aft: 69,
                            aftPoint: new Vector3(0.00116f, 0.00513f, 2.18743f),
                            extendedGuide: false,
                            guideIndex: 2,
                            required: new[] { 10, 13, 69 },
                            forbidden: new[] { 70 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 143,
                            donor: 61,
                            label: "topmast 1 / mizzen topmast 2",
                            fore: 13,
                            forePoint: new Vector3(-0.00000f, -0.00000f, -3.17129f),
                            aft: 70,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.38279f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 10, 13, 69, 70 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 144,
                            donor: 81,
                            label: "mast 2 / mizzen mast",
                            fore: 11,
                            forePoint: new Vector3(0.00116f, 0.00513f, -3.67355f),
                            aft: 12,
                            aftPoint: new Vector3(0.00116f, 0.00513f, 1.48931f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 12 },
                            forbidden: new[] { 14, 80 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 145,
                            donor: 81,
                            label: "mast 2 / mizzen topmast 3",
                            fore: 11,
                            forePoint: new Vector3(0.00116f, 0.00513f, 2.23960f),
                            aft: 80,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.38279f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 12, 80 },
                            forbidden: new[] { 14 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 146,
                            donor: 81,
                            label: "topmast 2 / mizzen mast",
                            fore: 11,
                            forePoint: new Vector3(0.00116f, 0.00513f, -3.67355f),
                            aft: 12,
                            aftPoint: new Vector3(0.00116f, 0.00513f, 1.48931f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 12, 14 },
                            forbidden: new[] { 80 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 147,
                            donor: 81,
                            label: "topmast 2 / mizzen topmast 3",
                            fore: 14,
                            forePoint: new Vector3(-0.00000f, -0.00000f, -7.15996f),
                            aft: 80,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.38279f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 12, 14, 80 },
                            forbidden: new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 148,
                            donor: 57,
                            label: "foremast 1 / mast 2",
                            fore: 51,
                            forePoint: new Vector3(0.00116f, 0.00513f, -0.79138f),
                            aft: 11,
                            aftPoint: new Vector3(0.00116f, 0.00513f, 2.18743f),
                            extendedGuide: false,
                            guideIndex: 2,
                            required: new[] { 11, 51 },
                            forbidden: new[] { 14, 64 }
                        ),
                        // 51.31 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 149,
                            donor: 57,
                            label: "foremast 1 / topmast 2",
                            fore: 51,
                            forePoint: new Vector3(0.00116f, 0.00513f, 3.43000f),
                            aft: 14,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.38279f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 14, 51 },
                            forbidden: new[] { 64 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 150,
                            donor: 57,
                            label: "fore topmast / mast 2",
                            fore: 51,
                            forePoint: new Vector3(0.00116f, 0.00513f, -0.79138f),
                            aft: 11,
                            aftPoint: new Vector3(0.00116f, 0.00513f, 2.18743f),
                            extendedGuide: false,
                            guideIndex: 2,
                            required: new[] { 11, 51, 64 },
                            forbidden: new[] { 14 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 151,
                            donor: 57,
                            label: "fore topmast / topmast 2",
                            fore: 64,
                            forePoint: new Vector3(-0.00000f, -0.00000f, -2.59788f),
                            aft: 14,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.38279f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 14, 51, 64 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 152,
                            donor: 68,
                            label: "raked foremast / mast 2",
                            fore: 62,
                            forePoint: new Vector3(0.00116f, 0.00513f, -1.39570f),
                            aft: 11,
                            aftPoint: new Vector3(0.00116f, 0.00513f, 2.18743f),
                            extendedGuide: false,
                            guideIndex: 2,
                            required: new[] { 11, 62 },
                            forbidden: new[] { 14 }
                        ),
                        // 61.21 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 153,
                            donor: 68,
                            label: "raked foremast / topmast 2",
                            fore: 62,
                            forePoint: new Vector3(0.00116f, 0.00513f, 3.43000f),
                            aft: 14,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.38279f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 14, 62 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            };
    }
}
