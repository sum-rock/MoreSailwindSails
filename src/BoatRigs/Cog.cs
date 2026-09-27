using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    internal static class Cog
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            "BOAT medi small (40)",
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
                    "Mainmast",
                    new[] { 6, 5, 59, 63 },
                    new[] { 6, 5, 59, 63, 58, 3, 4, 53, 54, 55, 60, 61, 62, 64, 67 },
                    null
                ),
                new SheetWinchCategory(
                    "Foremast",
                    new[] { 56, 68 },
                    new[] { 56, 68, 69, 70 },
                    null
                ),
                new SheetWinchCategory(
                    "Mizzenmast",
                    new[] { 8, 57, 66 },
                    new[] { 8, 57, 66, 51, 65 },
                    null
                ),
            };

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(51, new[] { 8 }, new[] { 5 }),
                new MastSupportDefinition(58, new[] { 5 }, new[] { 57 }),
                new MastSupportDefinition(65, new[] { 8 }, new[] { 6 }),
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
                    "Mainmast / mizzenmast",
                    new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            128,
                            51,
                            "main mast 2 / mizzen mast",
                            5,
                            new Vector3(-0.00000f, 0.00000f, -4.77700f),
                            8,
                            new Vector3(-0.00000f, -0.00391f, 1.14600f),
                            false,
                            0,
                            new[] { 5, 8 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            129,
                            65,
                            "main mast 1 / mizzen mast",
                            6,
                            new Vector3(-0.00000f, 0.00000f, -3.33567f),
                            8,
                            new Vector3(-0.00000f, -0.00391f, 1.14600f),
                            false,
                            0,
                            new[] { 6, 8 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            130,
                            58,
                            "main mast 2 / mizzen mast 2",
                            5,
                            new Vector3(0.00000f, 0.00000f, -4.25708f),
                            57,
                            new Vector3(-0.00000f, -0.00391f, 1.14600f),
                            false,
                            0,
                            new[] { 5, 57 },
                            new int[0]
                        ),
                    }
                ),
            };
    }
}
