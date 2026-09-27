using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    internal static class Jong
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            "BOAT junk large (70)",
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
                    new[] { 1, 67, 79 },
                    new[] { 1, 67, 79, 7, 10, 73, 74, 5, 6, 81 },
                    null
                ),
                new SheetWinchCategory(
                    "MainmastA",
                    new[] { 2, 51, 78 },
                    new[]
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
                    null
                ),
                new SheetWinchCategory(
                    "MainmastB",
                    new[] { 3, 52, 77 },
                    new[] { 3, 52, 77, 13, 14, 56, 57, 70, 71 },
                    null
                ),
                new SheetWinchCategory(
                    "Mizzenmast",
                    new[] { 4, 53, 80 },
                    new[] { 4, 53, 80 },
                    null
                ),
                new SheetWinchCategory("Bowsprit", new[] { 0 }, new[] { 0 }, null),
            };

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(10, new[] { 1 }, new[] { 2 }),
                new MastSupportDefinition(12, new[] { 2 }, new[] { 3 }),
                new MastSupportDefinition(55, new[] { 51 }, new[] { 52 }),
                new MastSupportDefinition(58, new[] { 51 }, new[] { 3 }),
                new MastSupportDefinition(73, new[] { 1 }, new[] { 52 }),
                new MastSupportDefinition(71, new[] { 52 }, new[] { 4 }),
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
                    "Foremast / mainmast 1",
                    new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            128,
                            10,
                            "foremast / main mast 1",
                            1,
                            new Vector3(0.00000f, -0.00000f, 1.13012f),
                            2,
                            new Vector3(-0.00000f, -0.00000f, 0.05000f),
                            false,
                            0,
                            new[] { 1, 2 },
                            new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    "Mainmast 1 / mainmast 2",
                    new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            129,
                            12,
                            "main mast 1 / main mast 2",
                            2,
                            new Vector3(0.00000f, -0.00000f, -2.22414f),
                            3,
                            new Vector3(-0.00000f, -0.00000f, 0.09200f),
                            false,
                            0,
                            new[] { 2, 3 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            130,
                            55,
                            "main mast 1 fore / main mast 2 fore",
                            51,
                            new Vector3(0.00000f, -0.00000f, -2.22315f),
                            52,
                            new Vector3(0.00000f, -0.00000f, 0.09200f),
                            false,
                            0,
                            new[] { 51, 52 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            131,
                            58,
                            "main mast 1 fore / main mast 2",
                            51,
                            new Vector3(0.00000f, -0.00000f, -3.69245f),
                            3,
                            new Vector3(-0.00000f, -0.00000f, 0.09200f),
                            false,
                            0,
                            new[] { 3, 51 },
                            new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    "Foremast / mainmast 2",
                    new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            132,
                            73,
                            "foremast / main mast 2 fore",
                            1,
                            new Vector3(-0.00000f, -0.00000f, 0.32528f),
                            52,
                            new Vector3(0.00000f, -0.00000f, 0.09200f),
                            false,
                            0,
                            new[] { 1, 52 },
                            new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    "Mainmast 2 / mizzenmast",
                    new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            133,
                            13,
                            "main mast 2 / mizzen mast",
                            3,
                            new Vector3(0.00000f, -0.00000f, -7.77488f),
                            4,
                            new Vector3(-0.00000f, -0.00000f, 0.15000f),
                            false,
                            0,
                            new[] { 3, 4 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            134,
                            56,
                            "main mast 2 fore / mizzen mast 2",
                            52,
                            new Vector3(0.00000f, -0.00000f, -7.77379f),
                            53,
                            new Vector3(0.00000f, -0.00000f, 0.15000f),
                            false,
                            0,
                            new[] { 52, 53 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            135,
                            71,
                            "main mast 2 fore / mizzen mast",
                            52,
                            new Vector3(0.00000f, -0.00000f, -9.24418f),
                            4,
                            new Vector3(-0.00000f, -0.00000f, 0.15000f),
                            false,
                            0,
                            new[] { 4, 52 },
                            new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    "Mainmast 1 / mizzenmast",
                    new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            136,
                            75,
                            "main mast 1 / mizzen mast 2",
                            2,
                            new Vector3(0.00000f, -0.00000f, -8.62064f),
                            53,
                            new Vector3(0.00000f, -0.00000f, 0.15000f),
                            false,
                            0,
                            new[] { 2, 53 },
                            new int[0]
                        ),
                    }
                ),
            };
    }
}
