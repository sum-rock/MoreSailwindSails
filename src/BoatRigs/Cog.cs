using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    internal static class Cog
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            boatName: "BOAT medi small (40)",
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
                    name: "Mainmast",
                    physicalMasts: new[] { 6, 5, 59, 63 },
                    sources: new[] { 6, 5, 59, 63, 58, 3, 4, 53, 54, 55, 60, 61, 62, 64, 67 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 56, 68 },
                    sources: new[] { 56, 68, 69, 70 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 8, 57, 66 },
                    sources: new[] { 8, 57, 66, 51, 65 },
                    fallback: null
                ),
            };

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 51,
                    foreSections: new[] { 8 },
                    aftSections: new[] { 5 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 58,
                    foreSections: new[] { 5 },
                    aftSections: new[] { 57 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 65,
                    foreSections: new[] { 8 },
                    aftSections: new[] { 6 }
                ),
            };

        // Installed mast ancestry: -1 marks a physical base section.
        private static Dictionary<int, int> MastParents() =>
            new Dictionary<int, int>
            {
                { 6, -1 },
                { 5, -1 },
                { 8, -1 },
                { 57, -1 },
                { 56, -1 },
                { 68, -1 },
            };

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
                            donor: 51,
                            label: "main mast 2 / mizzen mast",
                            fore: 5,
                            forePoint: new Vector3(-0.00000f, 0.00000f, -4.77700f),
                            aft: 8,
                            aftPoint: new Vector3(-0.00000f, -0.00391f, 1.14600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 5, 8 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 65,
                            label: "main mast 1 / mizzen mast",
                            fore: 6,
                            forePoint: new Vector3(-0.00000f, 0.00000f, -3.33567f),
                            aft: 8,
                            aftPoint: new Vector3(-0.00000f, -0.00391f, 1.14600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 6, 8 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 130,
                            donor: 58,
                            label: "main mast 2 / mizzen mast 2",
                            fore: 5,
                            forePoint: new Vector3(0.00000f, 0.00000f, -4.25708f),
                            aft: 57,
                            aftPoint: new Vector3(-0.00000f, -0.00391f, 1.14600f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 5, 57 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            };
    }
}
