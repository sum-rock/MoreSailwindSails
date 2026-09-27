using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    internal static class Leopard
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            "BOAT LEOPARD (207)",
            Supports(),
            Stays(),
            MastParents(),
            WinchMounts(),
            SheetCategories()
        );

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    "Foremast",
                    new[] { 4 },
                    new[] { 4, 5, 6, 13, 14, 15, 16 },
                    null
                ),
                new SheetWinchCategory(
                    "Mainmast",
                    new[] { 7 },
                    new[] { 7, 8, 9, 17, 18, 19 },
                    null
                ),
                new SheetWinchCategory("Mizzenmast", new[] { 10 }, new[] { 10, 11, 12 }, null),
            };

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(18, new[] { 7 }, new[] { 12 }),
                new MastSupportDefinition(17, new[] { 8 }, new[] { 12 }),
                new MastSupportDefinition(19, new[] { 7 }, new[] { 11 }),
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

        // Installed donor directions in boat space; provenance and numeric fixtures
        // are documented in docs/DEVELOPMENT.md under shared winch placement.
        private static WinchMountDefinition[] WinchMounts() =>
            new[]
            {
                new WinchMountDefinition(
                    7,
                    WinchRole.Reef,
                    new Vector3(0.000000f, 0.996195f, -0.087156f),
                    0.130912f,
                    MainReefBeam()
                ),
                new WinchMountDefinition(
                    8,
                    WinchRole.Reef,
                    new Vector3(0.000000f, 0.996195f, -0.087156f),
                    0.130912f,
                    MainReefBeam()
                ),
                new WinchMountDefinition(
                    9,
                    WinchRole.Reef,
                    new Vector3(0.000000f, 0.996195f, -0.087156f),
                    0.130912f,
                    MainReefBeam()
                ),
                new WinchMountDefinition(
                    10,
                    WinchRole.Reef,
                    new Vector3(0.000000f, 0.996195f, -0.087156f),
                    0.130912f,
                    MizzenReefBeam()
                ),
                new WinchMountDefinition(
                    11,
                    WinchRole.Reef,
                    new Vector3(0.000000f, 0.996195f, -0.087156f),
                    0.130912f,
                    MizzenReefBeam()
                ),
                new WinchMountDefinition(
                    12,
                    WinchRole.Reef,
                    new Vector3(0.000000f, 0.996195f, -0.087156f),
                    0.130912f,
                    MizzenReefBeam()
                ),
                new WinchMountDefinition(
                    13,
                    WinchRole.Left,
                    new Vector3(0.173648f, 0.984808f, 0.000000f),
                    0.130912f,
                    SheetRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    13,
                    WinchRole.Right,
                    new Vector3(-0.173648f, 0.984808f, -0.000000f),
                    0.130912f,
                    SheetRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    17,
                    WinchRole.Left,
                    new Vector3(0.173648f, 0.984808f, 0.000000f),
                    0.130912f,
                    SheetRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    17,
                    WinchRole.Right,
                    new Vector3(-0.173648f, 0.984808f, -0.000000f),
                    0.130912f,
                    SheetRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    18,
                    WinchRole.Left,
                    new Vector3(0.173648f, 0.984808f, 0.000000f),
                    0.130912f,
                    SheetRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    18,
                    WinchRole.Right,
                    new Vector3(-0.173648f, 0.984808f, -0.000000f),
                    0.130912f,
                    SheetRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    19,
                    WinchRole.Left,
                    new Vector3(0.173648f, 0.984808f, 0.000000f),
                    0.130912f,
                    SheetRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    19,
                    WinchRole.Right,
                    new Vector3(-0.173648f, 0.984808f, -0.000000f),
                    0.130912f,
                    SheetRail(WinchRole.Right)
                ),
            };

        // Measured solid support: decking trim.
        private static WinchSurfaceSegment[] SheetRail(WinchRole role) =>
            role == WinchRole.Left
                ? new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(-4.484130f, 6.262010f, -8.177880f),
                        new Vector3(-4.398850f, 6.334280f, -9.868890f),
                        new Vector3(0.000000f, 0.999088f, 0.042699f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-4.398850f, 6.334280f, -9.868890f),
                        new Vector3(-4.303430f, 6.435680f, -11.489260f),
                        new Vector3(0.000000f, 0.998048f, 0.062456f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-4.303430f, 6.435680f, -11.489260f),
                        new Vector3(-4.197500f, 6.556330f, -13.107250f),
                        new Vector3(0.000000f, 0.997231f, 0.074361f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-4.197500f, 6.556330f, -13.107250f),
                        new Vector3(-4.043980f, 6.678020f, -14.739260f),
                        new Vector3(0.000000f, 0.997232f, 0.074358f)
                    ),
                }
                : new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(4.484140f, 6.262010f, -8.177880f),
                        new Vector3(4.398860f, 6.334280f, -9.868890f),
                        new Vector3(0.000000f, 0.999088f, 0.042699f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(4.398860f, 6.334280f, -9.868890f),
                        new Vector3(4.303440f, 6.435680f, -11.489260f),
                        new Vector3(0.000000f, 0.998048f, 0.062456f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(4.303440f, 6.435680f, -11.489260f),
                        new Vector3(4.197510f, 6.556330f, -13.107240f),
                        new Vector3(0.000000f, 0.997231f, 0.074362f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(4.197510f, 6.556330f, -13.107240f),
                        new Vector3(4.043990f, 6.678020f, -14.739260f),
                        new Vector3(0.000000f, 0.997232f, 0.074358f)
                    ),
                };

        // Measured solid support: mainfife back / mizzenfife.
        private static WinchSurfaceSegment[] MainReefBeam() =>
            new[]
            {
                new WinchSurfaceSegment(
                    new Vector3(-0.849100f, 5.770600f, -5.572300f),
                    new Vector3(0.850900f, 5.770600f, -5.572300f),
                    new Vector3(-0.000000f, 1.000000f, -0.000000f)
                ),
            };

        // Measured solid support: mainfife back / mizzenfife.
        private static WinchSurfaceSegment[] MizzenReefBeam() =>
            new[]
            {
                new WinchSurfaceSegment(
                    new Vector3(-0.849100f, 8.158000f, -17.343800f),
                    new Vector3(0.850900f, 8.158000f, -17.343800f),
                    new Vector3(-0.000000f, 1.000000f, -0.000000f)
                ),
            };

        // Authored from installed Leopard and Shipyard Expansion assets.
        // Endpoint vectors are local to the named physical mast section.
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
                            13,
                            "Foremast / Mainmast",
                            4,
                            new Vector3(-0.00000f, -0.00000f, -9.96491f),
                            7,
                            new Vector3(-0.00000f, -0.00000f, -4.26600f),
                            false,
                            0,
                            new[] { 4, 7 },
                            new[] { 5, 6, 8, 9 }
                        ),
                        // 56.21 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            129,
                            13,
                            "Foremast / Mainmast Mid",
                            4,
                            new Vector3(0.00000f, 0.00000f, 0.00000f),
                            8,
                            new Vector3(-0.00000f, 0.00000f, -2.12849f),
                            false,
                            0,
                            new[] { 4, 7, 8 },
                            new[] { 5, 6, 9 }
                        ),
                        // 35.14 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            130,
                            13,
                            "Foremast / Mainmast Top",
                            4,
                            new Vector3(0.00000f, 0.00000f, 0.00000f),
                            9,
                            new Vector3(-0.00000f, -0.00000f, -1.27499f),
                            false,
                            0,
                            new[] { 4, 7, 8, 9 },
                            new[] { 5, 6 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            131,
                            13,
                            "Foremast Mid / Mainmast",
                            4,
                            new Vector3(-0.00000f, -0.00000f, -9.96491f),
                            7,
                            new Vector3(-0.00000f, -0.00000f, -4.26600f),
                            false,
                            0,
                            new[] { 4, 5, 7 },
                            new[] { 6, 8, 9 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            132,
                            13,
                            "Foremast Mid / Mainmast Mid",
                            5,
                            new Vector3(-0.00000f, -0.00000f, -5.07507f),
                            8,
                            new Vector3(-0.00000f, 0.00000f, -2.12849f),
                            false,
                            0,
                            new[] { 4, 5, 7, 8 },
                            new[] { 6, 9 }
                        ),
                        // 50.95 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            133,
                            13,
                            "Foremast Mid / Mainmast Top",
                            5,
                            new Vector3(-0.00000f, -0.00000f, -0.01445f),
                            9,
                            new Vector3(-0.00000f, -0.00000f, -1.27499f),
                            false,
                            0,
                            new[] { 4, 5, 7, 8, 9 },
                            new[] { 6 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            134,
                            13,
                            "Foremast Top / Mainmast",
                            4,
                            new Vector3(-0.00000f, -0.00000f, -9.96491f),
                            7,
                            new Vector3(-0.00000f, -0.00000f, -4.26600f),
                            false,
                            0,
                            new[] { 4, 5, 6, 7 },
                            new[] { 8, 9 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            135,
                            13,
                            "Foremast Top / Mainmast Mid",
                            5,
                            new Vector3(-0.00000f, -0.00000f, -5.07507f),
                            8,
                            new Vector3(-0.00000f, 0.00000f, -2.12849f),
                            false,
                            0,
                            new[] { 4, 5, 6, 7, 8 },
                            new[] { 9 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            136,
                            13,
                            "Foremast Top / Mainmast Top",
                            6,
                            new Vector3(-0.00000f, -0.00000f, -3.00593f),
                            9,
                            new Vector3(-0.00000f, -0.00000f, -1.27499f),
                            false,
                            0,
                            new[] { 4, 5, 6, 7, 8, 9 },
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
                            137,
                            18,
                            "Mainmast / Mizzenmast",
                            7,
                            new Vector3(-0.00000f, 0.00000f, -11.63731f),
                            10,
                            new Vector3(-0.00000f, 0.00000f, -3.05500f),
                            false,
                            0,
                            new[] { 7, 10 },
                            new[] { 8, 9, 11, 12 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            138,
                            18,
                            "Mainmast / Mizzenmast Mid",
                            7,
                            new Vector3(0.00000f, 0.00000f, -0.22779f),
                            11,
                            new Vector3(-0.00000f, -0.00000f, -1.47699f),
                            false,
                            0,
                            new[] { 7, 10, 11 },
                            new[] { 8, 9, 12 }
                        ),
                        // 39.95 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            139,
                            18,
                            "Mainmast / Mizzenmast Top",
                            7,
                            new Vector3(0.00000f, 0.00000f, 0.00000f),
                            12,
                            new Vector3(-0.00000f, -0.00000f, -0.91439f),
                            false,
                            0,
                            new[] { 7, 10, 11, 12 },
                            new[] { 8, 9 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            140,
                            18,
                            "Mainmast Mid / Mizzenmast",
                            7,
                            new Vector3(-0.00000f, 0.00000f, -11.63731f),
                            10,
                            new Vector3(-0.00000f, 0.00000f, -3.05500f),
                            false,
                            0,
                            new[] { 7, 8, 10 },
                            new[] { 9, 11, 12 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            141,
                            18,
                            "Mainmast Mid / Mizzenmast Mid",
                            8,
                            new Vector3(0.00000f, 0.00000f, -13.78993f),
                            11,
                            new Vector3(-0.00000f, -0.00000f, -1.47699f),
                            false,
                            0,
                            new[] { 7, 8, 10, 11 },
                            new[] { 9, 12 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            142,
                            18,
                            "Mainmast Mid / Mizzenmast Top",
                            8,
                            new Vector3(-0.00000f, 0.00000f, -3.88556f),
                            12,
                            new Vector3(-0.00000f, -0.00000f, -0.91439f),
                            false,
                            0,
                            new[] { 7, 8, 10, 11, 12 },
                            new[] { 9 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            143,
                            18,
                            "Mainmast Top / Mizzenmast",
                            7,
                            new Vector3(-0.00000f, 0.00000f, -11.63731f),
                            10,
                            new Vector3(-0.00000f, 0.00000f, -3.05500f),
                            false,
                            0,
                            new[] { 7, 8, 9, 10 },
                            new[] { 11, 12 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            144,
                            18,
                            "Mainmast Top / Mizzenmast Mid",
                            8,
                            new Vector3(0.00000f, 0.00000f, -13.78993f),
                            11,
                            new Vector3(-0.00000f, -0.00000f, -1.47699f),
                            false,
                            0,
                            new[] { 7, 8, 9, 10, 11 },
                            new[] { 12 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            145,
                            18,
                            "Mainmast Top / Mizzenmast Top",
                            8,
                            new Vector3(-0.00000f, 0.00000f, -3.88556f),
                            12,
                            new Vector3(-0.00000f, -0.00000f, -0.91439f),
                            false,
                            0,
                            new[] { 7, 8, 9, 10, 11, 12 },
                            new int[0]
                        ),
                    }
                ),
            };
    }
}
