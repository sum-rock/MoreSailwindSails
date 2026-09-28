using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    internal static class LargeDhow
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            boatName: "BOAT dhow large (30)",
            supports: Supports(),
            stays: Stays(),
            mastParents: MastParents(),
            sheetCategories: SheetCategories(),
            halyardGroups: HalyardGroups()
        );

        // Installed rig identities and support audit: Controls/HalyardMounts.txt.
        private static HalyardWinchGroup[] HalyardGroups() =>
            new[]
            {
                // Foremast 1; stay reef seat parented to mast_fore_0.
                new HalyardWinchGroup(mast: 0, sources: new[] { 0, 10 }),
                // Foremast 2; stay reef seat parented to mast_fore_1__raked_.
                new HalyardWinchGroup(mast: 1, sources: new[] { 1, 11 }),
                // Mainmast 1 and topmast; stay seats parented to mast_main_0.
                new HalyardWinchGroup(
                    mast: 2,
                    sources: new[] { 2, 12, 13, 14, 15, 20, 21, 22, 23, 24, 25 }
                ),
                new HalyardWinchGroup(
                    mast: 3,
                    sources: new[] { 3, 12, 13, 14, 15, 20, 21, 22, 23, 24, 25 }
                ),
                // Mainmast 2 and topmast; stay seats parented to mast_main_1.
                new HalyardWinchGroup(
                    mast: 4,
                    sources: new[] { 4, 16, 17, 18, 19, 26, 27, 28, 29, 30, 31 }
                ),
                new HalyardWinchGroup(
                    mast: 5,
                    sources: new[] { 5, 16, 17, 18, 19, 26, 27, 28, 29, 30, 31 }
                ),
                // Mizzenmast 1; stay seat parented to mast_mizzen_0.
                new HalyardWinchGroup(mast: 6, sources: new[] { 6, 32, 33, 34, 35 }),
                // Mizzenmast 2; stay seat parented to mast_mizzen_1__raked.
                new HalyardWinchGroup(mast: 7, sources: new[] { 7, 36, 37, 38, 39 }),
                // Mizzenmast 3; stay seat parented to mast_mizzen_2.
                new HalyardWinchGroup(mast: 8, sources: new[] { 8, 40, 41, 42, 43 }),
            };

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 0, 1 },
                    sources: new[] { 0, 1, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 10, 11 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 2, 4 },
                    sources: new[]
                    {
                        2,
                        4,
                        3,
                        5,
                        32,
                        33,
                        34,
                        35,
                        36,
                        37,
                        38,
                        39,
                        40,
                        41,
                        42,
                        43,
                        12,
                        13,
                        14,
                        15,
                        16,
                        17,
                        18,
                        19,
                    },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 6, 7, 8 },
                    sources: new[] { 6, 7, 8 },
                    fallback: null
                ),
            };

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 20,
                    foreSections: new[] { 0 },
                    aftSections: new[] { 3, 2 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 23,
                    foreSections: new[] { 1 },
                    aftSections: new[] { 3, 2 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 26,
                    foreSections: new[] { 0 },
                    aftSections: new[] { 5, 4 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 29,
                    foreSections: new[] { 1 },
                    aftSections: new[] { 5, 4 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 32,
                    foreSections: new[] { 3, 2 },
                    aftSections: new[] { 6 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 34,
                    foreSections: new[] { 5, 4 },
                    aftSections: new[] { 6 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 36,
                    foreSections: new[] { 3, 2 },
                    aftSections: new[] { 7 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 38,
                    foreSections: new[] { 5, 4 },
                    aftSections: new[] { 7 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 40,
                    foreSections: new[] { 3, 2 },
                    aftSections: new[] { 8 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 42,
                    foreSections: new[] { 5, 4 },
                    aftSections: new[] { 8 }
                ),
            };

        // Native topmast options require their matching mainmast despite being siblings.
        private static Dictionary<int, int> MastParents() =>
            new Dictionary<int, int>
            {
                { 0, -1 },
                { 1, -1 },
                { 2, -1 },
                { 3, 2 },
                { 4, -1 },
                { 5, 4 },
                { 6, -1 },
                { 7, -1 },
                { 8, -1 },
            };

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
