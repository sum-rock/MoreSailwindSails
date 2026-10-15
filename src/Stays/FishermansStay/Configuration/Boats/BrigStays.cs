// Owns Brig's authored stay variants and attachment measurements.
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay.Configuration.Boats
{
    internal static class BrigStays
    {
        internal static readonly FishermansStayDefinition Definition = new FishermansStayDefinition(
            boat: BoatConfiguration.Boats.Brig.Definition,
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
                            donor: 15,
                            label: "foremast 1 / main mast 1",
                            fore: 3,
                            forePoint: new Vector3(0.00000f, -0.00000f, -2.39540f),
                            aft: 5,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.93843f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 3, 5 },
                            forbidden: new[] { 55, 56 }
                        ),
                        // 61.15 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 15,
                            label: "foremast 1 / main topmast 1",
                            fore: 3,
                            forePoint: new Vector3(0.00000f, -0.00000f, 3.08320f),
                            aft: 56,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.75600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 3, 5, 56 },
                            forbidden: new[] { 55 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 130,
                            donor: 15,
                            label: "fore topmast 1 / main mast 1",
                            fore: 3,
                            forePoint: new Vector3(0.00000f, -0.00000f, -2.39540f),
                            aft: 5,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.93843f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 3, 5, 55 },
                            forbidden: new[] { 56 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 131,
                            donor: 15,
                            label: "fore topmast 1 / main topmast 1",
                            fore: 55,
                            forePoint: new Vector3(0.00000f, -0.00000f, -2.44076f),
                            aft: 56,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.75600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 3, 5, 55, 56 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 132,
                            donor: 16,
                            label: "foremast 1 / main mast 2",
                            fore: 3,
                            forePoint: new Vector3(0.00000f, -0.00000f, -0.64295f),
                            aft: 4,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.93843f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 3, 4 },
                            forbidden: new[] { 55, 58 }
                        ),
                        // 46.74 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 133,
                            donor: 16,
                            label: "foremast 1 / main topmast 2",
                            fore: 3,
                            forePoint: new Vector3(0.00000f, -0.00000f, 3.08320f),
                            aft: 58,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.75600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 3, 4, 58 },
                            forbidden: new[] { 55 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 134,
                            donor: 16,
                            label: "fore topmast 1 / main mast 2",
                            fore: 3,
                            forePoint: new Vector3(0.00000f, -0.00000f, -0.64295f),
                            aft: 4,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.93843f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 3, 4, 55 },
                            forbidden: new[] { 58 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 135,
                            donor: 16,
                            label: "fore topmast 1 / main topmast 2",
                            fore: 55,
                            forePoint: new Vector3(0.00000f, -0.00000f, -0.72370f),
                            aft: 58,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.75600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 3, 4, 55, 58 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 136,
                            donor: 18,
                            label: "foremast 2 / main mast 1",
                            fore: 2,
                            forePoint: new Vector3(0.00000f, -0.00000f, -2.75301f),
                            aft: 5,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.93843f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 5 },
                            forbidden: new[] { 56, 57 }
                        ),
                        // 64.44 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 137,
                            donor: 18,
                            label: "foremast 2 / main topmast 1",
                            fore: 2,
                            forePoint: new Vector3(0.00000f, -0.00000f, 3.08320f),
                            aft: 56,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.75600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 5, 56 },
                            forbidden: new[] { 57 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 138,
                            donor: 18,
                            label: "fore topmast 2 / main mast 1",
                            fore: 2,
                            forePoint: new Vector3(0.00000f, -0.00000f, -2.75301f),
                            aft: 5,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.93843f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 5, 57 },
                            forbidden: new[] { 56 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 139,
                            donor: 18,
                            label: "fore topmast 2 / main topmast 1",
                            fore: 57,
                            forePoint: new Vector3(-0.01325f, 0.00000f, -2.71105f),
                            aft: 56,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.75600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 5, 56, 57 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 140,
                            donor: 20,
                            label: "foremast 2 / main mast 2",
                            fore: 2,
                            forePoint: new Vector3(0.00000f, -0.00000f, -1.00056f),
                            aft: 4,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.93843f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 4 },
                            forbidden: new[] { 57, 58 }
                        ),
                        // 55.52 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 141,
                            donor: 20,
                            label: "foremast 2 / main topmast 2",
                            fore: 2,
                            forePoint: new Vector3(0.00000f, -0.00000f, 3.08320f),
                            aft: 58,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.75600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 4, 58 },
                            forbidden: new[] { 57 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 142,
                            donor: 20,
                            label: "fore topmast 2 / main mast 2",
                            fore: 2,
                            forePoint: new Vector3(0.00000f, -0.00000f, -1.00056f),
                            aft: 4,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.93843f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 4, 57 },
                            forbidden: new[] { 58 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 143,
                            donor: 20,
                            label: "fore topmast 2 / main topmast 2",
                            fore: 57,
                            forePoint: new Vector3(-0.01325f, 0.00000f, -0.99399f),
                            aft: 58,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.75600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 4, 57, 58 },
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
                            mountIndex: 144,
                            donor: 22,
                            label: "main mast 2 / mizzen mast 1",
                            fore: 4,
                            forePoint: new Vector3(0.00000f, -0.00000f, -6.01788f),
                            aft: 7,
                            aftPoint: new Vector3(0.00000f, 0.00391f, 1.37491f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 7 },
                            forbidden: new[] { 58, 59 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 145,
                            donor: 22,
                            label: "main mast 2 / mizzen topmast 1",
                            fore: 4,
                            forePoint: new Vector3(0.00000f, -0.00000f, 0.16585f),
                            aft: 59,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.75600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 7, 59 },
                            forbidden: new[] { 58 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 146,
                            donor: 22,
                            label: "main topmast 2 / mizzen mast 1",
                            fore: 4,
                            forePoint: new Vector3(0.00000f, -0.00000f, -6.01788f),
                            aft: 7,
                            aftPoint: new Vector3(0.00000f, 0.00391f, 1.37491f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 7, 58 },
                            forbidden: new[] { 59 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 147,
                            donor: 22,
                            label: "main topmast 2 / mizzen topmast 1",
                            fore: 4,
                            forePoint: new Vector3(0.00000f, -0.00000f, 0.16585f),
                            aft: 59,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.75600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 7, 58, 59 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 148,
                            donor: 24,
                            label: "main mast 2 / mizzen mast 2",
                            fore: 4,
                            forePoint: new Vector3(0.00000f, -0.00000f, -5.12171f),
                            aft: 6,
                            aftPoint: new Vector3(0.00000f, 0.00391f, 1.37491f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 6 },
                            forbidden: new[] { 58, 60 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 149,
                            donor: 24,
                            label: "main mast 2 / mizzen topmast 2",
                            fore: 4,
                            forePoint: new Vector3(0.00000f, -0.00000f, 1.05939f),
                            aft: 60,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.75600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 6, 60 },
                            forbidden: new[] { 58 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 150,
                            donor: 24,
                            label: "main topmast 2 / mizzen mast 2",
                            fore: 4,
                            forePoint: new Vector3(0.00000f, -0.00000f, -5.12171f),
                            aft: 6,
                            aftPoint: new Vector3(0.00000f, 0.00391f, 1.37491f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 6, 58 },
                            forbidden: new[] { 60 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 151,
                            donor: 24,
                            label: "main topmast 2 / mizzen topmast 2",
                            fore: 58,
                            forePoint: new Vector3(0.00000f, -0.00000f, -6.71642f),
                            aft: 60,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.75600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 6, 58, 60 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            };
    }
}
