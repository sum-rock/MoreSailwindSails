// Owns LargeDhow's authored stay variants and attachment measurements.
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay.Configuration.Boats
{
    internal static class LargeDhowStays
    {
        internal static readonly FishermansStayDefinition Definition = new FishermansStayDefinition(
            boat: BoatConfiguration.Boats.LargeDhow.Definition,
            stays: Stays()
        );

        // Measured in installed 0.39 level24, in boat-local space.
        // Four-control masts use the upper port fitting (reefWinch[2]): all
        // Authored from installed native and boat-mod assets.
        private static FishermansStayGroupDefinition[] Stays() =>
            new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast",
                    variants: new[]
                    {
                        // 64.63 degrees at aft mast; visible foremast head fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 20,
                            label: "foremast 1 / main mast 1",
                            fore: 0,
                            forePoint: new Vector3(-0.006836f, 0.000000f, 2.223290f),
                            aft: 2,
                            aftPoint: new Vector3(-0.006836f, 0.000000f, 2.083000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 0, 2 },
                            forbidden: new[] { 3 }
                        ),
                        // 63.86 degrees at aft mast; visible foremast head fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 20,
                            label: "foremast 1 / main topmast 1",
                            fore: 0,
                            forePoint: new Vector3(-0.006836f, 0.000000f, 2.223290f),
                            aft: 3,
                            aftPoint: new Vector3(-0.006836f, 0.000000f, -4.563908f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 0, 2, 3 },
                            forbidden: new int[0]
                        ),
                        // 67.70 degrees at aft mast; visible foremast head fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 130,
                            donor: 23,
                            label: "foremast 2 / main mast 1",
                            fore: 1,
                            forePoint: new Vector3(-0.006836f, 0.000000f, 2.223290f),
                            aft: 2,
                            aftPoint: new Vector3(-0.006836f, 0.000000f, 2.083000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 1, 2 },
                            forbidden: new[] { 3 }
                        ),
                        // 67.12 degrees at aft mast; visible foremast head fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 131,
                            donor: 23,
                            label: "foremast 2 / main topmast 1",
                            fore: 1,
                            forePoint: new Vector3(-0.006836f, 0.000000f, 2.223290f),
                            aft: 3,
                            aftPoint: new Vector3(-0.006836f, 0.000000f, -4.563908f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 1, 2, 3 },
                            forbidden: new int[0]
                        ),
                        // 56.32 degrees at aft mast; visible foremast head fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 132,
                            donor: 26,
                            label: "foremast 1 / main mast 2",
                            fore: 0,
                            forePoint: new Vector3(-0.006836f, 0.000000f, 2.223290f),
                            aft: 4,
                            aftPoint: new Vector3(-0.006836f, 0.000000f, 2.101000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 0, 4 },
                            forbidden: new[] { 5 }
                        ),
                        // 55.00 degrees at aft mast; visible foremast head fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 133,
                            donor: 26,
                            label: "foremast 1 / main topmast 2",
                            fore: 0,
                            forePoint: new Vector3(-0.006836f, 0.000000f, 2.223290f),
                            aft: 5,
                            aftPoint: new Vector3(-0.006836f, 0.000000f, -4.561601f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 0, 4, 5 },
                            forbidden: new int[0]
                        ),
                        // 61.63 degrees at aft mast; visible foremast head fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 134,
                            donor: 29,
                            label: "foremast 2 / main mast 2",
                            fore: 1,
                            forePoint: new Vector3(-0.006836f, 0.000000f, 2.223290f),
                            aft: 4,
                            aftPoint: new Vector3(-0.006836f, 0.000000f, 2.101000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 1, 4 },
                            forbidden: new[] { 5 }
                        ),
                        // 60.71 degrees at aft mast; visible foremast head fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 135,
                            donor: 29,
                            label: "foremast 2 / main topmast 2",
                            fore: 1,
                            forePoint: new Vector3(-0.006836f, 0.000000f, 2.223290f),
                            aft: 5,
                            aftPoint: new Vector3(-0.006836f, 0.000000f, -4.561601f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 1, 4, 5 },
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
                            mountIndex: 136,
                            donor: 32,
                            label: "main mast 1 / mizzen mast 1",
                            fore: 2,
                            forePoint: new Vector3(-0.006836f, 0.000000f, -5.322307f),
                            aft: 6,
                            aftPoint: new Vector3(-0.006836f, 0.000000f, 0.873000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 6 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 137,
                            donor: 34,
                            label: "main mast 2 / mizzen mast 1",
                            fore: 4,
                            forePoint: new Vector3(-0.006836f, 0.000000f, -6.402065f),
                            aft: 6,
                            aftPoint: new Vector3(-0.006836f, 0.000000f, 0.873000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 6 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 138,
                            donor: 36,
                            label: "main mast 1 / mizzen mast 2",
                            fore: 2,
                            forePoint: new Vector3(-0.006836f, 0.000000f, -4.704098f),
                            aft: 7,
                            aftPoint: new Vector3(-0.006836f, 0.000000f, 0.775951f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 7 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 139,
                            donor: 38,
                            label: "main mast 2 / mizzen mast 2",
                            fore: 4,
                            forePoint: new Vector3(-0.006836f, 0.000000f, -5.436457f),
                            aft: 7,
                            aftPoint: new Vector3(-0.006836f, 0.000000f, 0.775951f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 7 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 140,
                            donor: 40,
                            label: "main mast 1 / mizzen mast 3",
                            fore: 2,
                            forePoint: new Vector3(-0.006836f, 0.000000f, -4.257566f),
                            aft: 8,
                            aftPoint: new Vector3(-0.006836f, 0.000000f, 0.738000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 8 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 141,
                            donor: 42,
                            label: "main mast 2 / mizzen mast 3",
                            fore: 4,
                            forePoint: new Vector3(-0.006836f, 0.000000f, -5.337324f),
                            aft: 8,
                            aftPoint: new Vector3(-0.006836f, 0.000000f, 0.738000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 8 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            };
    }
}
