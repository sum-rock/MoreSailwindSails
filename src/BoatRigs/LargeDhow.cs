using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    internal static class LargeDhow
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            "BOAT dhow large (30)",
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
                    new[] { 0, 1 },
                    new[] { 0, 1, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 10, 11 },
                    null
                ),
                new SheetWinchCategory(
                    "Mainmast",
                    new[] { 2, 4 },
                    new[]
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
                    null
                ),
                new SheetWinchCategory("Mizzenmast", new[] { 6, 7, 8 }, new[] { 6, 7, 8 }, null),
            };

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(20, new[] { 0 }, new[] { 3, 2 }),
                new MastSupportDefinition(23, new[] { 1 }, new[] { 3, 2 }),
                new MastSupportDefinition(26, new[] { 0 }, new[] { 5, 4 }),
                new MastSupportDefinition(29, new[] { 1 }, new[] { 5, 4 }),
                new MastSupportDefinition(32, new[] { 3, 2 }, new[] { 6 }),
                new MastSupportDefinition(34, new[] { 5, 4 }, new[] { 6 }),
                new MastSupportDefinition(36, new[] { 3, 2 }, new[] { 7 }),
                new MastSupportDefinition(38, new[] { 5, 4 }, new[] { 7 }),
                new MastSupportDefinition(40, new[] { 3, 2 }, new[] { 8 }),
                new MastSupportDefinition(42, new[] { 5, 4 }, new[] { 8 }),
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
                    "Foremast / mainmast",
                    new[]
                    {
                        // 64.63 degrees at aft mast; visible foremast head fallback.
                        new FishermansStayVariantDefinition(
                            128,
                            20,
                            "foremast 1 / main mast 1",
                            0,
                            new Vector3(-0.006836f, 0.000000f, 2.223290f),
                            2,
                            new Vector3(-0.006836f, 0.000000f, 2.083000f),
                            false,
                            0,
                            new[] { 0, 2 },
                            new[] { 3 }
                        ),
                        // 63.86 degrees at aft mast; visible foremast head fallback.
                        new FishermansStayVariantDefinition(
                            129,
                            20,
                            "foremast 1 / main topmast 1",
                            0,
                            new Vector3(-0.006836f, 0.000000f, 2.223290f),
                            3,
                            new Vector3(-0.006836f, 0.000000f, -4.563908f),
                            false,
                            0,
                            new[] { 0, 2, 3 },
                            new int[0]
                        ),
                        // 67.70 degrees at aft mast; visible foremast head fallback.
                        new FishermansStayVariantDefinition(
                            130,
                            23,
                            "foremast 2 / main mast 1",
                            1,
                            new Vector3(-0.006836f, 0.000000f, 2.223290f),
                            2,
                            new Vector3(-0.006836f, 0.000000f, 2.083000f),
                            false,
                            0,
                            new[] { 1, 2 },
                            new[] { 3 }
                        ),
                        // 67.12 degrees at aft mast; visible foremast head fallback.
                        new FishermansStayVariantDefinition(
                            131,
                            23,
                            "foremast 2 / main topmast 1",
                            1,
                            new Vector3(-0.006836f, 0.000000f, 2.223290f),
                            3,
                            new Vector3(-0.006836f, 0.000000f, -4.563908f),
                            false,
                            0,
                            new[] { 1, 2, 3 },
                            new int[0]
                        ),
                        // 56.32 degrees at aft mast; visible foremast head fallback.
                        new FishermansStayVariantDefinition(
                            132,
                            26,
                            "foremast 1 / main mast 2",
                            0,
                            new Vector3(-0.006836f, 0.000000f, 2.223290f),
                            4,
                            new Vector3(-0.006836f, 0.000000f, 2.101000f),
                            false,
                            0,
                            new[] { 0, 4 },
                            new[] { 5 }
                        ),
                        // 55.00 degrees at aft mast; visible foremast head fallback.
                        new FishermansStayVariantDefinition(
                            133,
                            26,
                            "foremast 1 / main topmast 2",
                            0,
                            new Vector3(-0.006836f, 0.000000f, 2.223290f),
                            5,
                            new Vector3(-0.006836f, 0.000000f, -4.561601f),
                            false,
                            0,
                            new[] { 0, 4, 5 },
                            new int[0]
                        ),
                        // 61.63 degrees at aft mast; visible foremast head fallback.
                        new FishermansStayVariantDefinition(
                            134,
                            29,
                            "foremast 2 / main mast 2",
                            1,
                            new Vector3(-0.006836f, 0.000000f, 2.223290f),
                            4,
                            new Vector3(-0.006836f, 0.000000f, 2.101000f),
                            false,
                            0,
                            new[] { 1, 4 },
                            new[] { 5 }
                        ),
                        // 60.71 degrees at aft mast; visible foremast head fallback.
                        new FishermansStayVariantDefinition(
                            135,
                            29,
                            "foremast 2 / main topmast 2",
                            1,
                            new Vector3(-0.006836f, 0.000000f, 2.223290f),
                            5,
                            new Vector3(-0.006836f, 0.000000f, -4.561601f),
                            false,
                            0,
                            new[] { 1, 4, 5 },
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
                            136,
                            32,
                            "main mast 1 / mizzen mast 1",
                            2,
                            new Vector3(-0.006836f, 0.000000f, -5.322307f),
                            6,
                            new Vector3(-0.006836f, 0.000000f, 0.873000f),
                            false,
                            0,
                            new[] { 2, 6 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            137,
                            34,
                            "main mast 2 / mizzen mast 1",
                            4,
                            new Vector3(-0.006836f, 0.000000f, -6.402065f),
                            6,
                            new Vector3(-0.006836f, 0.000000f, 0.873000f),
                            false,
                            0,
                            new[] { 4, 6 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            138,
                            36,
                            "main mast 1 / mizzen mast 2",
                            2,
                            new Vector3(-0.006836f, 0.000000f, -4.704098f),
                            7,
                            new Vector3(-0.006836f, 0.000000f, 0.775951f),
                            false,
                            0,
                            new[] { 2, 7 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            139,
                            38,
                            "main mast 2 / mizzen mast 2",
                            4,
                            new Vector3(-0.006836f, 0.000000f, -5.436457f),
                            7,
                            new Vector3(-0.006836f, 0.000000f, 0.775951f),
                            false,
                            0,
                            new[] { 4, 7 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            140,
                            40,
                            "main mast 1 / mizzen mast 3",
                            2,
                            new Vector3(-0.006836f, 0.000000f, -4.257566f),
                            8,
                            new Vector3(-0.006836f, 0.000000f, 0.738000f),
                            false,
                            0,
                            new[] { 2, 8 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            141,
                            42,
                            "main mast 2 / mizzen mast 3",
                            4,
                            new Vector3(-0.006836f, 0.000000f, -5.337324f),
                            8,
                            new Vector3(-0.006836f, 0.000000f, 0.738000f),
                            false,
                            0,
                            new[] { 4, 8 },
                            new int[0]
                        ),
                    }
                ),
            };
    }
}
