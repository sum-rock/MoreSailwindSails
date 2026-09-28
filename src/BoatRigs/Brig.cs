using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    internal static class Brig
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            boatName: "BOAT medi medium (50)",
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
                // Foremast 1; F0 jib seats and matching topmast stays.
                new HalyardWinchGroup(mast: 3, sources: new[] { 3, 8, 9, 10, 11, 68, 72, 75, 77 }),
                new HalyardWinchGroup(
                    mast: 55,
                    sources: new[] { 55, 8, 9, 10, 11, 68, 72, 75, 77 }
                ),
                // Foremast 2; F1 jib seats; excludes unregistered duplicate 74.
                new HalyardWinchGroup(mast: 2, sources: new[] { 2, 12, 13, 26, 69, 76, 78 }),
                new HalyardWinchGroup(mast: 57, sources: new[] { 57, 12, 13, 26, 69, 76, 78 }),
                // Mainmast 1; B0 stays and matching topmast stays.
                new HalyardWinchGroup(mast: 5, sources: new[] { 5, 14, 15, 17, 18, 61, 63 }),
                new HalyardWinchGroup(mast: 56, sources: new[] { 56, 14, 15, 17, 18, 61, 63 }),
                // Mainmast 2; B1 stays; registered 70 has B1 reef seats.
                new HalyardWinchGroup(
                    mast: 4,
                    sources: new[] { 4, 16, 19, 20, 25, 51, 52, 62, 64, 70, 71, 73 }
                ),
                new HalyardWinchGroup(
                    mast: 58,
                    sources: new[] { 58, 16, 19, 20, 25, 51, 52, 62, 64, 70, 71, 73 }
                ),
                // Mizzenmast 1; mizzen0 jib and topmast stay seats.
                new HalyardWinchGroup(mast: 7, sources: new[] { 7, 21, 22, 65 }),
                new HalyardWinchGroup(mast: 59, sources: new[] { 59, 21, 22, 65 }),
                // Mizzenmast 2; mizzen1 jib and topmast stay seats.
                new HalyardWinchGroup(mast: 6, sources: new[] { 6, 23, 24, 66 }),
                new HalyardWinchGroup(mast: 60, sources: new[] { 60, 23, 24, 66 }),
                // Raked foremast 1; F2 jib seat.
                new HalyardWinchGroup(mast: 67, sources: new[] { 67, 80 }),
                // Raked foremast 2; F3 jib seats.
                new HalyardWinchGroup(mast: 79, sources: new[] { 79, 81 }),
            };

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 3, 2, 67, 79, 82 },
                    sources: new[]
                    {
                        3,
                        2,
                        55,
                        57,
                        67,
                        79,
                        82,
                        14,
                        15,
                        25,
                        16,
                        17,
                        18,
                        19,
                        20,
                        61,
                        62,
                        63,
                        64,
                        8,
                        9,
                        10,
                        11,
                        12,
                        13,
                        26,
                        68,
                        69,
                        70,
                        71,
                        72,
                        74,
                        75,
                        76,
                        77,
                        78,
                        80,
                        81,
                        85,
                        86,
                    },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 5, 4, 83 },
                    sources: new[] { 5, 4, 56, 58, 83, 21, 22, 23, 24, 65, 66, 51, 52, 73 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 7, 6, 84 },
                    sources: new[] { 7, 6, 59, 60, 84 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Bowsprit",
                    physicalMasts: new[] { 53, 54 },
                    sources: new[] { 53, 54 },
                    fallback: null
                ),
            };

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 15,
                    foreSections: new[] { 3 },
                    aftSections: new[] { 5 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 16,
                    foreSections: new[] { 3 },
                    aftSections: new[] { 4 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 18,
                    foreSections: new[] { 2 },
                    aftSections: new[] { 5 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 20,
                    foreSections: new[] { 2 },
                    aftSections: new[] { 4 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 22,
                    foreSections: new[] { 4 },
                    aftSections: new[] { 7 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 24,
                    foreSections: new[] { 4 },
                    aftSections: new[] { 6 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 61,
                    foreSections: new[] { 3 },
                    aftSections: new[] { 56, 5 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 62,
                    foreSections: new[] { 3 },
                    aftSections: new[] { 58, 4 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 63,
                    foreSections: new[] { 2 },
                    aftSections: new[] { 56, 5 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 64,
                    foreSections: new[] { 2 },
                    aftSections: new[] { 58, 4 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 65,
                    foreSections: new[] { 4 },
                    aftSections: new[] { 59, 7 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 66,
                    foreSections: new[] { 4 },
                    aftSections: new[] { 60, 6 }
                ),
            };

        // Installed mast ancestry: -1 marks a physical base section.
        private static Dictionary<int, int> MastParents() =>
            new Dictionary<int, int>
            {
                { 3, -1 },
                { 2, -1 },
                { 67, -1 },
                { 79, -1 },
                { 5, -1 },
                { 4, -1 },
                { 7, -1 },
                { 6, -1 },
                { 53, -1 },
                { 54, -1 },
                { 55, 3 },
                { 57, 2 },
                { 56, 5 },
                { 58, 4 },
                { 59, 7 },
                { 60, 6 },
            };

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
