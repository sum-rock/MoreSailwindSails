using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    internal static class Shroud
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            "BOAT Shroud Large",
            Supports(),
            Stays(),
            MastParents(),
            SheetCategories(),
            PinClearances()
        );

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    "Foremast",
                    new[] { 6, 5 },
                    new[] { 6, 5, 16, 17, 24, 13, 14, 15, 20, 21, 22, 23 },
                    ForemastFallback
                ),
                new SheetWinchCategory(
                    "Mainmast",
                    new[] { 8, 7 },
                    new[] { 8, 7, 18, 19, 25 },
                    null
                ),
                new SheetWinchCategory("Mizzenmast", new[] { 10, 9 }, new[] { 10, 9 }, null),
            };

        private static SheetFallbackPair ForemastFallback =>
            new SheetFallbackPair(
                new SheetFallbackSeat(
                    new Vector3(-3.465970f, 4.829765f, -3.901223f),
                    Vector3.up,
                    new Vector3(-0.006981f, 0.999976f, 0f),
                    0.076685f,
                    24,
                    WinchRole.Left,
                    0
                ),
                new SheetFallbackSeat(
                    new Vector3(3.465970f, 4.829765f, -3.901223f),
                    Vector3.up,
                    new Vector3(-0.006981f, 0.999976f, 0f),
                    0.076685f,
                    24,
                    WinchRole.Right,
                    0
                )
            );

        // Measured side-pin pitch affects clearance only, never seat eligibility.
        private static Dictionary<string, float> PinClearances() =>
            new Dictionary<string, float>
            {
                { "Coil_Mainmast_Reef_1", 0.14f },
                { "Coil_Mainmast_Reef_2", 0.14f },
                { "Coil_Mainmast_Reef_3", 0.14f },
                { "Coil_Mainmast_Reef_4", 0.14f },
                { "Coil_Mainmast_Reef_5", 0.14f },
                { "Coil_Mainmast_Reef_6", 0.14f },
                { "Coil_Mainmast_Jib_Reef_1", 0.14f },
                { "Coil_Mainmast_Jib_Reef_2", 0.14f },
                { "Coil_Mainmast_Jib_Reef_3", 0.14f },
                { "Coil_Mainmast_Jib_Reef_4", 0.14f },
                { "Coil_Mainmast_Jib_Reef_5", 0.14f },
                { "Coil_Mainmast_Jib_Reef_6", 0.14f },
                { "Coil_Mizzenmast_Reef_1", 0.14f },
                { "Coil_Mizzenmast_Reef_2", 0.14f },
                { "Coil_Mizzenmast_Reef_3", 0.14f },
                { "Coil_Mizzenmast_Reef_4", 0.14f },
                { "Coil_Mizzenmast_Reef_5", 0.14f },
                { "Coil_Mizzenmast_Reef_7", 0.14f },
                { "Coil_Mizzenmast_Jib_Reef_1", 0.14f },
                { "Coil_Mizzenmast_Jib_Reef_2", 0.14f },
                { "Coil_Mizzenmast_Jib_Reef_3", 0.14f },
                { "Coil_Mizzenmast_Jib_Reef_4", 0.14f },
                { "Coil_Mizzenmast_Jib_Reef_5", 0.14f },
                { "Coil_Mizzenmast_Jib_Reef_6", 0.14f },
            };

        private static MastSupportDefinition[] Supports() =>
            new[] { new MastSupportDefinition(25, new[] { 7 }, new[] { 9 }) };

        // Installed mast ancestry: -1 marks a physical base section.
        private static Dictionary<int, int> MastParents() =>
            new Dictionary<int, int>
            {
                { 6, -1 },
                { 5, -1 },
                { 8, -1 },
                { 7, -1 },
                { 10, -1 },
                { 9, -1 },
            };

        // Authored from installed native and boat-mod assets.
        private static FishermansStayGroupDefinition[] Stays() =>
            new[]
            {
                new FishermansStayGroupDefinition(
                    "Foremast / mainmast",
                    new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            128,
                            24,
                            "Foremast 1 / Mainmast 1",
                            6,
                            new Vector3(0.00000f, -0.00682f, -1.26976f),
                            8,
                            new Vector3(0.00000f, -0.00928f, -0.42400f),
                            false,
                            0,
                            new[] { 6, 8 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            129,
                            24,
                            "Foremast 1 Tall / Mainmast 1",
                            5,
                            new Vector3(-0.00291f, 0.00006f, -11.85458f),
                            8,
                            new Vector3(0.00000f, -0.00928f, -0.42400f),
                            false,
                            0,
                            new[] { 5, 8 },
                            new int[0]
                        ),
                        // 42.20 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            130,
                            24,
                            "Foremast 1 / Mainmast 1 Tall",
                            6,
                            new Vector3(0.00000f, -0.00682f, 0.50000f),
                            7,
                            new Vector3(-0.00537f, -0.00391f, -0.46400f),
                            false,
                            0,
                            new[] { 6, 7 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            131,
                            24,
                            "Foremast 1 Tall / Mainmast 1 Tall",
                            5,
                            new Vector3(-0.00291f, 0.00006f, -2.44452f),
                            7,
                            new Vector3(-0.00537f, -0.00391f, -0.46400f),
                            false,
                            0,
                            new[] { 5, 7 },
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
                            132,
                            25,
                            "Mainmast 1 / Mizzen 1",
                            8,
                            new Vector3(0.00000f, -0.00928f, -6.67603f),
                            10,
                            new Vector3(0.00000f, -0.00365f, -0.51800f),
                            false,
                            0,
                            new[] { 8, 10 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            133,
                            25,
                            "Mainmast 1 Tall / Mizzen 1",
                            7,
                            new Vector3(-0.00537f, -0.00391f, -16.12609f),
                            10,
                            new Vector3(0.00000f, -0.00365f, -0.51800f),
                            false,
                            0,
                            new[] { 7, 10 },
                            new int[0]
                        ),
                        // 55.88 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            134,
                            25,
                            "Mainmast 1 / Mizzenmast 1 Tall",
                            8,
                            new Vector3(0.00000f, -0.00928f, 0.50000f),
                            9,
                            new Vector3(0.00025f, -0.00391f, -0.12789f),
                            false,
                            0,
                            new[] { 8, 9 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            135,
                            25,
                            "Mainmast 1 Tall / Mizzenmast 1 Tall",
                            7,
                            new Vector3(-0.00537f, -0.00391f, -5.67900f),
                            9,
                            new Vector3(0.00025f, -0.00391f, -0.12789f),
                            false,
                            0,
                            new[] { 7, 9 },
                            new int[0]
                        ),
                    }
                ),
            };
    }
}
