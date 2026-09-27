using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    internal static class Junk
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            "BOAT junk medium (80)",
            Supports(),
            Stays(),
            MastParents(),
            SheetCategories()
        );

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    "Foremast",
                    new[] { 9, 58, 69 },
                    new[] { 9, 58, 62, 69, 15, 16, 61, 14, 54, 60, 67, 72 },
                    null
                ),
                new SheetWinchCategory(
                    "Mainmast",
                    new[] { 10, 11, 70 },
                    new[]
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
                    null
                ),
                new SheetWinchCategory(
                    "Mizzenmast",
                    new[] { 12, 13, 57, 71 },
                    new[] { 12, 13, 57, 53, 71 },
                    null
                ),
                new SheetWinchCategory("Bowsprit", new[] { 8, 51 }, new[] { 8, 51 }, null),
            };

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(16, new[] { 9 }, new[] { 10 }),
                new MastSupportDefinition(61, new[] { 58 }, new[] { 11 }),
                new MastSupportDefinition(5, new[] { 10 }, new[] { 12 }),
                new MastSupportDefinition(6, new[] { 11 }, new[] { 12 }),
                new MastSupportDefinition(65, new[] { 10 }, new[] { 53, 12 }),
                new MastSupportDefinition(66, new[] { 11 }, new[] { 53, 12 }),
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
                    "Foremast / mainmast",
                    new[]
                    {
                        // 59.94 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            128,
                            16,
                            "foremast / main mast 1",
                            9,
                            new Vector3(0.00000f, 0.00049f, 2.69000f),
                            10,
                            new Vector3(0.00248f, -0.00146f, 2.30206f),
                            false,
                            0,
                            new[] { 9, 10 },
                            new[] { 62 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            129,
                            16,
                            "fore topmast / main mast 1",
                            62,
                            new Vector3(-0.00000f, -0.00000f, -0.75865f),
                            10,
                            new Vector3(0.00248f, -0.00146f, 2.30206f),
                            false,
                            0,
                            new[] { 9, 10, 62 },
                            new int[0]
                        ),
                        // 57.86 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            130,
                            61,
                            "raked foremast / main mast 2",
                            58,
                            new Vector3(0.00000f, 0.00049f, 2.69000f),
                            11,
                            new Vector3(0.00248f, -0.00146f, 2.30206f),
                            false,
                            0,
                            new[] { 11, 58 },
                            new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    "Mainmast / mizzenmast",
                    new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            131,
                            5,
                            "main mast 1 / mizzen mast 1",
                            10,
                            new Vector3(0.00248f, -0.00146f, -7.52300f),
                            12,
                            new Vector3(0.00000f, 0.00049f, 1.57183f),
                            false,
                            0,
                            new[] { 10, 12 },
                            new[] { 53 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            132,
                            5,
                            "main mast 1 / mizzen topmast",
                            10,
                            new Vector3(0.00248f, -0.00146f, -2.59085f),
                            53,
                            new Vector3(-0.00000f, -0.00000f, 0.84800f),
                            false,
                            0,
                            new[] { 10, 12, 53 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            133,
                            6,
                            "main mast 2 / mizzen mast 1",
                            11,
                            new Vector3(0.00248f, -0.00146f, -8.89858f),
                            12,
                            new Vector3(0.00000f, 0.00049f, 1.57183f),
                            false,
                            0,
                            new[] { 11, 12 },
                            new[] { 53 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            134,
                            6,
                            "main mast 2 / mizzen topmast",
                            11,
                            new Vector3(0.00248f, -0.00146f, -3.96643f),
                            53,
                            new Vector3(-0.00000f, -0.00000f, 0.84800f),
                            false,
                            0,
                            new[] { 11, 12, 53 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            135,
                            7,
                            "main mast 2 / mizzen mast 2",
                            11,
                            new Vector3(0.00248f, -0.00146f, -7.08233f),
                            13,
                            new Vector3(0.00000f, 0.00049f, 1.57200f),
                            false,
                            0,
                            new[] { 11, 13 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            136,
                            52,
                            "main mast 2 / mizzen mast 3",
                            11,
                            new Vector3(0.00248f, -0.00146f, -2.19715f),
                            57,
                            new Vector3(0.00248f, -0.00146f, 2.30206f),
                            false,
                            0,
                            new[] { 11, 57 },
                            new int[0]
                        ),
                    }
                ),
            };
    }
}
