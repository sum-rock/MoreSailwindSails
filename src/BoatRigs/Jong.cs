using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    internal static class Jong
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            boatName: "BOAT junk large (70)",
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
                // Upright foremast; upper and lower native forestay controls.
                new HalyardWinchGroup(mast: 1, sources: new[] { 1, 5, 6 }),
                // Mainmast 1 aft position; front-stay controls at that position.
                new HalyardWinchGroup(mast: 2, sources: new[] { 2, 7, 8, 9, 10, 65, 68, 72 }),
                // Mainmast 1 forward position; front-stay controls at that position.
                new HalyardWinchGroup(mast: 51, sources: new[] { 51, 60, 62, 64, 66 }),
                // Mainmast 2 aft position; middle-stay controls at that position.
                new HalyardWinchGroup(mast: 3, sources: new[] { 3, 11, 12, 58, 59 }),
                // Mainmast 2 forward position; middle-stay controls at that position.
                new HalyardWinchGroup(mast: 52, sources: new[] { 52, 54, 55, 73, 74 }),
                // Mizzenmast 1; back-stay controls at the aft position.
                new HalyardWinchGroup(mast: 4, sources: new[] { 4, 13, 14, 70, 71 }),
                // Mizzenmast 2; back-stay controls at the forward position.
                new HalyardWinchGroup(mast: 53, sources: new[] { 53, 56, 57, 75, 76 }),
            };

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 1, 67, 79 },
                    sources: new[] { 1, 67, 79, 7, 10, 73, 74, 5, 6, 81 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "MainmastA",
                    physicalMasts: new[] { 2, 51, 78 },
                    sources: new[]
                    {
                        2,
                        51,
                        78,
                        11,
                        12,
                        54,
                        55,
                        58,
                        59,
                        75,
                        76,
                        8,
                        9,
                        60,
                        61,
                        62,
                        64,
                        65,
                        66,
                        68,
                        69,
                        72,
                        82,
                        83,
                        84,
                    },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "MainmastB",
                    physicalMasts: new[] { 3, 52, 77 },
                    sources: new[] { 3, 52, 77, 13, 14, 56, 57, 70, 71 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 4, 53, 80 },
                    sources: new[] { 4, 53, 80 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Bowsprit",
                    physicalMasts: new[] { 0 },
                    sources: new[] { 0 },
                    fallback: null
                ),
            };

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 10,
                    foreSections: new[] { 1 },
                    aftSections: new[] { 2 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 12,
                    foreSections: new[] { 2 },
                    aftSections: new[] { 3 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 55,
                    foreSections: new[] { 51 },
                    aftSections: new[] { 52 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 58,
                    foreSections: new[] { 51 },
                    aftSections: new[] { 3 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 73,
                    foreSections: new[] { 1 },
                    aftSections: new[] { 52 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 71,
                    foreSections: new[] { 52 },
                    aftSections: new[] { 4 }
                ),
            };

        // Installed mast ancestry: -1 marks a physical base section.
        private static Dictionary<int, int> MastParents() =>
            new Dictionary<int, int>
            {
                { 0, -1 },
                { 1, -1 },
                { 67, -1 },
                { 2, -1 },
                { 51, -1 },
                { 3, -1 },
                { 52, -1 },
                { 4, -1 },
                { 53, -1 },
            };

        // Authored from installed native and boat-mod assets.
        private static FishermansStayGroupDefinition[] Stays() =>
            new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast 1",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 10,
                            label: "foremast / main mast 1",
                            fore: 1,
                            forePoint: new Vector3(0.00000f, -0.00000f, 1.13012f),
                            aft: 2,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.05000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 1, 2 },
                            forbidden: new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Mainmast 1 / mainmast 2",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 12,
                            label: "main mast 1 / main mast 2",
                            fore: 2,
                            forePoint: new Vector3(0.00000f, -0.00000f, -2.22414f),
                            aft: 3,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.09200f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 3 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 130,
                            donor: 55,
                            label: "main mast 1 fore / main mast 2 fore",
                            fore: 51,
                            forePoint: new Vector3(0.00000f, -0.00000f, -2.22315f),
                            aft: 52,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.09200f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 51, 52 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 131,
                            donor: 58,
                            label: "main mast 1 fore / main mast 2",
                            fore: 51,
                            forePoint: new Vector3(0.00000f, -0.00000f, -3.69245f),
                            aft: 3,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.09200f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 3, 51 },
                            forbidden: new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast 2",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 132,
                            donor: 73,
                            label: "foremast / main mast 2 fore",
                            fore: 1,
                            forePoint: new Vector3(-0.00000f, -0.00000f, 0.32528f),
                            aft: 52,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.09200f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 1, 52 },
                            forbidden: new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Mainmast 2 / mizzenmast",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 133,
                            donor: 13,
                            label: "main mast 2 / mizzen mast",
                            fore: 3,
                            forePoint: new Vector3(0.00000f, -0.00000f, -7.77488f),
                            aft: 4,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.15000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 3, 4 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 134,
                            donor: 56,
                            label: "main mast 2 fore / mizzen mast 2",
                            fore: 52,
                            forePoint: new Vector3(0.00000f, -0.00000f, -7.77379f),
                            aft: 53,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.15000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 52, 53 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 135,
                            donor: 71,
                            label: "main mast 2 fore / mizzen mast",
                            fore: 52,
                            forePoint: new Vector3(0.00000f, -0.00000f, -9.24418f),
                            aft: 4,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.15000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 52 },
                            forbidden: new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Mainmast 1 / mizzenmast",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 136,
                            donor: 75,
                            label: "main mast 1 / mizzen mast 2",
                            fore: 2,
                            forePoint: new Vector3(0.00000f, -0.00000f, -8.62064f),
                            aft: 53,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.15000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 53 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            };
    }
}
