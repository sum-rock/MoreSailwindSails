using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    internal static class Junk
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            boatName: "BOAT junk medium (80)",
            supports: Supports(),
            stays: Stays(),
            mastParents: MastParents(),
            sheetCategories: SheetCategories()
        );

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 9, 58, 69 },
                    sources: new[] { 9, 58, 62, 69, 15, 16, 61, 14, 54, 60, 67, 72 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 10, 11, 70 },
                    sources: new[]
                    {
                        10,
                        11,
                        70,
                        5,
                        6,
                        7,
                        52,
                        65,
                        66,
                        0,
                        1,
                        2,
                        3,
                        4,
                        55,
                        56,
                        63,
                        64,
                        68,
                        73,
                        74,
                    },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 12, 13, 57, 71 },
                    sources: new[] { 12, 13, 57, 53, 71 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Bowsprit",
                    physicalMasts: new[] { 8, 51 },
                    sources: new[] { 8, 51 },
                    fallback: null
                ),
            };

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 16,
                    foreSections: new[] { 9 },
                    aftSections: new[] { 10 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 61,
                    foreSections: new[] { 58 },
                    aftSections: new[] { 11 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 5,
                    foreSections: new[] { 10 },
                    aftSections: new[] { 12 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 6,
                    foreSections: new[] { 11 },
                    aftSections: new[] { 12 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 65,
                    foreSections: new[] { 10 },
                    aftSections: new[] { 53, 12 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 66,
                    foreSections: new[] { 11 },
                    aftSections: new[] { 53, 12 }
                ),
            };

        // Installed mast ancestry: -1 marks a physical base section.
        private static Dictionary<int, int> MastParents() =>
            new Dictionary<int, int>
            {
                { 8, -1 },
                { 9, -1 },
                { 58, -1 },
                { 10, -1 },
                { 11, -1 },
                { 12, -1 },
                { 13, -1 },
                { 57, -1 },
                { 53, 12 },
                { 62, 9 },
            };

        // Authored from installed native and boat-mod assets.
        private static FishermansStayGroupDefinition[] Stays() =>
            new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast",
                    variants: new[]
                    {
                        // 59.94 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 16,
                            label: "foremast / main mast 1",
                            fore: 9,
                            forePoint: new Vector3(0.00000f, 0.00049f, 2.69000f),
                            aft: 10,
                            aftPoint: new Vector3(0.00248f, -0.00146f, 2.30206f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 9, 10 },
                            forbidden: new[] { 62 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 16,
                            label: "fore topmast / main mast 1",
                            fore: 62,
                            forePoint: new Vector3(-0.00000f, -0.00000f, -0.75865f),
                            aft: 10,
                            aftPoint: new Vector3(0.00248f, -0.00146f, 2.30206f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 9, 10, 62 },
                            forbidden: new int[0]
                        ),
                        // 57.86 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 130,
                            donor: 61,
                            label: "raked foremast / main mast 2",
                            fore: 58,
                            forePoint: new Vector3(0.00000f, 0.00049f, 2.69000f),
                            aft: 11,
                            aftPoint: new Vector3(0.00248f, -0.00146f, 2.30206f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 58 },
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
                            mountIndex: 131,
                            donor: 5,
                            label: "main mast 1 / mizzen mast 1",
                            fore: 10,
                            forePoint: new Vector3(0.00248f, -0.00146f, -7.52300f),
                            aft: 12,
                            aftPoint: new Vector3(0.00000f, 0.00049f, 1.57183f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 10, 12 },
                            forbidden: new[] { 53 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 132,
                            donor: 5,
                            label: "main mast 1 / mizzen topmast",
                            fore: 10,
                            forePoint: new Vector3(0.00248f, -0.00146f, -2.59085f),
                            aft: 53,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.84800f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 10, 12, 53 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 133,
                            donor: 6,
                            label: "main mast 2 / mizzen mast 1",
                            fore: 11,
                            forePoint: new Vector3(0.00248f, -0.00146f, -8.89858f),
                            aft: 12,
                            aftPoint: new Vector3(0.00000f, 0.00049f, 1.57183f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 12 },
                            forbidden: new[] { 53 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 134,
                            donor: 6,
                            label: "main mast 2 / mizzen topmast",
                            fore: 11,
                            forePoint: new Vector3(0.00248f, -0.00146f, -3.96643f),
                            aft: 53,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.84800f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 12, 53 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 135,
                            donor: 7,
                            label: "main mast 2 / mizzen mast 2",
                            fore: 11,
                            forePoint: new Vector3(0.00248f, -0.00146f, -7.08233f),
                            aft: 13,
                            aftPoint: new Vector3(0.00000f, 0.00049f, 1.57200f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 13 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 136,
                            donor: 52,
                            label: "main mast 2 / mizzen mast 3",
                            fore: 11,
                            forePoint: new Vector3(0.00248f, -0.00146f, -2.19715f),
                            aft: 57,
                            aftPoint: new Vector3(0.00248f, -0.00146f, 2.30206f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 57 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            };
    }
}
