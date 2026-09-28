using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    internal static class Leopard
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            boatName: "BOAT LEOPARD (207)",
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
                // Foremast sections; FORESTAYS/ROPES controls and foremast guides.
                new HalyardWinchGroup(mast: 4, sources: new[] { 4, 0, 1, 2, 3 }),
                new HalyardWinchGroup(mast: 5, sources: new[] { 5, 0, 1, 2, 3 }),
                new HalyardWinchGroup(mast: 6, sources: new[] { 6, 0, 1, 2, 3 }),
                // Mainmast sections; MAINSTAYS/ROPES controls and mainmast guides.
                new HalyardWinchGroup(mast: 7, sources: new[] { 7, 13, 14, 15, 16 }),
                new HalyardWinchGroup(mast: 8, sources: new[] { 8, 13, 14, 15, 16 }),
                new HalyardWinchGroup(mast: 9, sources: new[] { 9, 13, 14, 15, 16 }),
                // Mizzen sections; MIZZENSTAYS/ROPES controls and mizzen guides.
                new HalyardWinchGroup(mast: 10, sources: new[] { 10, 17, 18, 19 }),
                new HalyardWinchGroup(mast: 11, sources: new[] { 11, 17, 18, 19 }),
                new HalyardWinchGroup(mast: 12, sources: new[] { 12, 17, 18, 19 }),
            };

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 4 },
                    sources: new[] { 4, 5, 6, 13, 14, 15, 16, 0, 1, 2, 3 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 7 },
                    sources: new[] { 7, 8, 9, 17, 18, 19 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 10 },
                    sources: new[] { 10, 11, 12 },
                    fallback: null
                ),
            };

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 18,
                    foreSections: new[] { 7 },
                    aftSections: new[] { 12 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 17,
                    foreSections: new[] { 8 },
                    aftSections: new[] { 12 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 19,
                    foreSections: new[] { 7 },
                    aftSections: new[] { 11 }
                ),
            };

        // Installed mast ancestry: -1 marks a physical base section.
        private static Dictionary<int, int> MastParents() =>
            new Dictionary<int, int>
            {
                { 4, -1 },
                { 5, 4 },
                { 6, 5 },
                { 7, -1 },
                { 8, 7 },
                { 9, 8 },
                { 10, -1 },
                { 11, 10 },
                { 12, 11 },
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
                            donor: 13,
                            label: "Foremast / Mainmast",
                            fore: 4,
                            forePoint: new Vector3(-0.00000f, -0.00000f, -9.96491f),
                            aft: 7,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, -4.26600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 7 },
                            forbidden: new[] { 5, 6, 8, 9 }
                        ),
                        // 56.21 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 13,
                            label: "Foremast / Mainmast Mid",
                            fore: 4,
                            forePoint: new Vector3(0.00000f, 0.00000f, 0.00000f),
                            aft: 8,
                            aftPoint: new Vector3(-0.00000f, 0.00000f, -2.12849f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 7, 8 },
                            forbidden: new[] { 5, 6, 9 }
                        ),
                        // 35.14 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 130,
                            donor: 13,
                            label: "Foremast / Mainmast Top",
                            fore: 4,
                            forePoint: new Vector3(0.00000f, 0.00000f, 0.00000f),
                            aft: 9,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, -1.27499f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 7, 8, 9 },
                            forbidden: new[] { 5, 6 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 131,
                            donor: 13,
                            label: "Foremast Mid / Mainmast",
                            fore: 4,
                            forePoint: new Vector3(-0.00000f, -0.00000f, -9.96491f),
                            aft: 7,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, -4.26600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 5, 7 },
                            forbidden: new[] { 6, 8, 9 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 132,
                            donor: 13,
                            label: "Foremast Mid / Mainmast Mid",
                            fore: 5,
                            forePoint: new Vector3(-0.00000f, -0.00000f, -5.07507f),
                            aft: 8,
                            aftPoint: new Vector3(-0.00000f, 0.00000f, -2.12849f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 5, 7, 8 },
                            forbidden: new[] { 6, 9 }
                        ),
                        // 50.95 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 133,
                            donor: 13,
                            label: "Foremast Mid / Mainmast Top",
                            fore: 5,
                            forePoint: new Vector3(-0.00000f, -0.00000f, -0.01445f),
                            aft: 9,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, -1.27499f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 5, 7, 8, 9 },
                            forbidden: new[] { 6 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 134,
                            donor: 13,
                            label: "Foremast Top / Mainmast",
                            fore: 4,
                            forePoint: new Vector3(-0.00000f, -0.00000f, -9.96491f),
                            aft: 7,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, -4.26600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 5, 6, 7 },
                            forbidden: new[] { 8, 9 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 135,
                            donor: 13,
                            label: "Foremast Top / Mainmast Mid",
                            fore: 5,
                            forePoint: new Vector3(-0.00000f, -0.00000f, -5.07507f),
                            aft: 8,
                            aftPoint: new Vector3(-0.00000f, 0.00000f, -2.12849f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 5, 6, 7, 8 },
                            forbidden: new[] { 9 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 136,
                            donor: 13,
                            label: "Foremast Top / Mainmast Top",
                            fore: 6,
                            forePoint: new Vector3(-0.00000f, -0.00000f, -3.00593f),
                            aft: 9,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, -1.27499f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 5, 6, 7, 8, 9 },
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
                            mountIndex: 137,
                            donor: 18,
                            label: "Mainmast / Mizzenmast",
                            fore: 7,
                            forePoint: new Vector3(-0.00000f, 0.00000f, -11.63731f),
                            aft: 10,
                            aftPoint: new Vector3(-0.00000f, 0.00000f, -3.05500f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 7, 10 },
                            forbidden: new[] { 8, 9, 11, 12 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 138,
                            donor: 18,
                            label: "Mainmast / Mizzenmast Mid",
                            fore: 7,
                            forePoint: new Vector3(0.00000f, 0.00000f, -0.22779f),
                            aft: 11,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, -1.47699f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 7, 10, 11 },
                            forbidden: new[] { 8, 9, 12 }
                        ),
                        // 39.95 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 139,
                            donor: 18,
                            label: "Mainmast / Mizzenmast Top",
                            fore: 7,
                            forePoint: new Vector3(0.00000f, 0.00000f, 0.00000f),
                            aft: 12,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, -0.91439f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 7, 10, 11, 12 },
                            forbidden: new[] { 8, 9 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 140,
                            donor: 18,
                            label: "Mainmast Mid / Mizzenmast",
                            fore: 7,
                            forePoint: new Vector3(-0.00000f, 0.00000f, -11.63731f),
                            aft: 10,
                            aftPoint: new Vector3(-0.00000f, 0.00000f, -3.05500f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 7, 8, 10 },
                            forbidden: new[] { 9, 11, 12 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 141,
                            donor: 18,
                            label: "Mainmast Mid / Mizzenmast Mid",
                            fore: 8,
                            forePoint: new Vector3(0.00000f, 0.00000f, -13.78993f),
                            aft: 11,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, -1.47699f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 7, 8, 10, 11 },
                            forbidden: new[] { 9, 12 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 142,
                            donor: 18,
                            label: "Mainmast Mid / Mizzenmast Top",
                            fore: 8,
                            forePoint: new Vector3(-0.00000f, 0.00000f, -3.88556f),
                            aft: 12,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, -0.91439f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 7, 8, 10, 11, 12 },
                            forbidden: new[] { 9 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 143,
                            donor: 18,
                            label: "Mainmast Top / Mizzenmast",
                            fore: 7,
                            forePoint: new Vector3(-0.00000f, 0.00000f, -11.63731f),
                            aft: 10,
                            aftPoint: new Vector3(-0.00000f, 0.00000f, -3.05500f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 7, 8, 9, 10 },
                            forbidden: new[] { 11, 12 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 144,
                            donor: 18,
                            label: "Mainmast Top / Mizzenmast Mid",
                            fore: 8,
                            forePoint: new Vector3(0.00000f, 0.00000f, -13.78993f),
                            aft: 11,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, -1.47699f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 7, 8, 9, 10, 11 },
                            forbidden: new[] { 12 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 145,
                            donor: 18,
                            label: "Mainmast Top / Mizzenmast Top",
                            fore: 8,
                            forePoint: new Vector3(-0.00000f, 0.00000f, -3.88556f),
                            aft: 12,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, -0.91439f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 7, 8, 9, 10, 11, 12 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            };
    }
}
