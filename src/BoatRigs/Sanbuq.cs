using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    internal static class Sanbuq
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            "BOAT dhow medium (20)",
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
                    new[] { 51, 62 },
                    new[] { 51, 62, 64, 57, 58, 68, 79 },
                    null
                ),
                new SheetWinchCategory(
                    "Mainmast",
                    new[] { 10, 11, 75 },
                    new[] { 10, 11, 13, 14, 75, 9, 54, 56, 60, 61, 66, 67, 71, 81, 82 },
                    null
                ),
                new SheetWinchCategory(
                    "Mizzenmast",
                    new[] { 12, 55, 69, 74 },
                    new[] { 12, 55, 69, 59, 70, 80, 74 },
                    null
                ),
            };

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(60, new[] { 10 }, new[] { 59, 55 }),
                new MastSupportDefinition(67, new[] { 11 }, new[] { 59, 55 }),
                new MastSupportDefinition(71, new[] { 10 }, new[] { 70, 69 }),
                new MastSupportDefinition(81, new[] { 11 }, new[] { 80, 12 }),
                new MastSupportDefinition(82, new[] { 10 }, new[] { 80, 12 }),
                new MastSupportDefinition(58, new[] { 51 }, new[] { 14, 11 }),
                new MastSupportDefinition(68, new[] { 62 }, new[] { 14, 11 }),
                new MastSupportDefinition(79, new[] { 51 }, new[] { 14, 11 }),
                new MastSupportDefinition(54, new[] { 10 }, new[] { 55 }),
                new MastSupportDefinition(61, new[] { 10 }, new[] { 69 }),
                new MastSupportDefinition(66, new[] { 11 }, new[] { 55 }),
            };

        // Installed mast ancestry: -1 marks a physical base section.
        private static Dictionary<int, int> MastParents() =>
            new Dictionary<int, int>
            {
                { 10, -1 },
                { 11, -1 },
                { 12, -1 },
                { 55, -1 },
                { 69, -1 },
                { 13, 10 },
                { 14, 11 },
                { 51, -1 },
                { 62, -1 },
                { 64, 51 },
                { 59, 55 },
                { 70, 69 },
                { 80, 12 },
            };

        // Installed donor directions in boat space; provenance and numeric fixtures
        // are documented in docs/DEVELOPMENT.md under shared winch placement.
        private static WinchMountDefinition[] WinchMounts() =>
            new[]
            {
                new WinchMountDefinition(
                    9,
                    WinchRole.Left,
                    new Vector3(0.069322f, 0.991350f, 0.111444f),
                    0.065329f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    9,
                    WinchRole.Right,
                    new Vector3(-0.037551f, 0.990635f, 0.131270f),
                    0.065329f,
                    AftRail(WinchRole.Right)
                ),
                new WinchMountDefinition(10, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 10),
                new WinchMountDefinition(11, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 11),
                new WinchMountDefinition(
                    12,
                    WinchRole.Reef,
                    new Vector3(0f, 0.995601f, -0.093689f),
                    true,
                    12
                ),
                new WinchMountDefinition(14, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 11),
                new WinchMountDefinition(51, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 51),
                new WinchMountDefinition(
                    54,
                    WinchRole.Left,
                    new Vector3(0.996663f, -0.073649f, 0.035202f),
                    0.065329f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    54,
                    WinchRole.Right,
                    new Vector3(-0.996042f, -0.047695f, 0.075002f),
                    0.065329f,
                    AftRail(WinchRole.Right)
                ),
                new WinchMountDefinition(55, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 55),
                new WinchMountDefinition(
                    57,
                    WinchRole.Left,
                    new Vector3(0.069701f, 0.996772f, -0.039843f),
                    0.052263f,
                    ForeRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    57,
                    WinchRole.Right,
                    new Vector3(-0.028583f, 0.999160f, -0.029362f),
                    0.052263f,
                    ForeRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    58,
                    WinchRole.Left,
                    new Vector3(0.069701f, 0.996772f, -0.039843f),
                    0.052263f,
                    ForeRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    58,
                    WinchRole.Right,
                    new Vector3(-0.028583f, 0.999160f, -0.029362f),
                    0.052263f,
                    ForeRail(WinchRole.Right)
                ),
                new WinchMountDefinition(59, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 55),
                new WinchMountDefinition(
                    60,
                    WinchRole.Left,
                    new Vector3(0.996663f, -0.073649f, 0.035202f),
                    0.065329f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    60,
                    WinchRole.Right,
                    new Vector3(-0.996042f, -0.047695f, 0.075002f),
                    0.065329f,
                    AftRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    61,
                    WinchRole.Left,
                    new Vector3(0.996663f, -0.073649f, 0.035202f),
                    0.065329f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    61,
                    WinchRole.Right,
                    new Vector3(-0.996042f, -0.047695f, 0.075002f),
                    0.065329f,
                    AftRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    62,
                    WinchRole.Reef,
                    new Vector3(0f, 0.951057f, 0.309017f),
                    true,
                    62
                ),
                new WinchMountDefinition(
                    66,
                    WinchRole.Left,
                    new Vector3(0.996663f, -0.073649f, 0.035202f),
                    0.065329f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    66,
                    WinchRole.Right,
                    new Vector3(-0.996042f, -0.047695f, 0.075002f),
                    0.065329f,
                    AftRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    67,
                    WinchRole.Left,
                    new Vector3(0.996663f, -0.073649f, 0.035202f),
                    0.065329f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    67,
                    WinchRole.Right,
                    new Vector3(-0.996042f, -0.047695f, 0.075002f),
                    0.065329f,
                    AftRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    68,
                    WinchRole.Left,
                    new Vector3(0.069701f, 0.996772f, -0.039843f),
                    0.052263f,
                    ForeRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    68,
                    WinchRole.Right,
                    new Vector3(-0.028583f, 0.999160f, -0.029362f),
                    0.052263f,
                    ForeRail(WinchRole.Right)
                ),
                new WinchMountDefinition(69, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 69),
                new WinchMountDefinition(70, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 69),
                new WinchMountDefinition(
                    71,
                    WinchRole.Left,
                    new Vector3(0.996663f, -0.073649f, 0.035202f),
                    0.065329f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    71,
                    WinchRole.Right,
                    new Vector3(-0.996042f, -0.047695f, 0.075002f),
                    0.065329f,
                    AftRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    79,
                    WinchRole.Left,
                    new Vector3(0.069701f, 0.996772f, -0.039843f),
                    0.052263f,
                    ForeRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    79,
                    WinchRole.Right,
                    new Vector3(-0.028583f, 0.999160f, -0.029362f),
                    0.052263f,
                    ForeRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    80,
                    WinchRole.Reef,
                    new Vector3(0f, 0.995595f, -0.093759f),
                    true,
                    80
                ),
                new WinchMountDefinition(
                    81,
                    WinchRole.Left,
                    new Vector3(0.996663f, -0.073649f, 0.035202f),
                    0.065329f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    81,
                    WinchRole.Right,
                    new Vector3(-0.996042f, -0.047695f, 0.075002f),
                    0.065329f,
                    AftRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    82,
                    WinchRole.Left,
                    new Vector3(0.996663f, -0.073649f, 0.035202f),
                    0.065329f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    82,
                    WinchRole.Right,
                    new Vector3(-0.996042f, -0.047695f, 0.075002f),
                    0.065329f,
                    AftRail(WinchRole.Right)
                ),
            };

        // Measured solid support: Cube_013.
        private static WinchSurfaceSegment[] ForeRail(WinchRole role) =>
            role == WinchRole.Left
                ? new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(-2.378610f, 2.666290f, 2.244240f),
                        new Vector3(-2.466200f, 2.547410f, -0.992530f),
                        new Vector3(0.000000f, 0.999326f, -0.036703f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-2.466200f, 2.547410f, -0.992530f),
                        new Vector3(-2.461910f, 2.545190f, -1.952240f),
                        new Vector3(0.000000f, 0.999997f, -0.002313f)
                    ),
                }
                : new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(2.378610f, 2.666290f, 2.244240f),
                        new Vector3(2.466200f, 2.547410f, -0.992530f),
                        new Vector3(0.000000f, 0.999326f, -0.036703f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(2.466200f, 2.547410f, -0.992530f),
                        new Vector3(2.461920f, 2.545190f, -1.952240f),
                        new Vector3(0.000000f, 0.999997f, -0.002313f)
                    ),
                };

        // Measured solid support: Cube_013.
        private static WinchSurfaceSegment[] AftRail(WinchRole role) =>
            role == WinchRole.Left
                ? new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(-2.294540f, 3.571950f, -9.850500f),
                        new Vector3(-2.195340f, 3.794650f, -11.883770f),
                        new Vector3(-0.000000f, 0.994055f, 0.108877f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-2.445640f, 3.329340f, -6.600000f),
                        new Vector3(-2.294540f, 3.571950f, -9.850500f),
                        new Vector3(-0.000000f, 0.997226f, 0.074431f)
                    ),
                }
                : new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(2.294540f, 3.571950f, -9.850500f),
                        new Vector3(2.195350f, 3.794650f, -11.883770f),
                        new Vector3(-0.000000f, 0.994055f, 0.108877f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(2.445640f, 3.329340f, -6.600000f),
                        new Vector3(2.294540f, 3.571950f, -9.850500f),
                        new Vector3(-0.000000f, 0.997226f, 0.074431f)
                    ),
                };

        // Authored from installed Sanbuq and Shipyard Expansion assets.
        // Endpoint vectors are local to the named physical mast section.
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
                            9,
                            "mast 1 / mizzen mast",
                            10,
                            new Vector3(0.00116f, 0.00513f, -4.94033f),
                            12,
                            new Vector3(0.00116f, 0.00513f, 1.48931f),
                            false,
                            0,
                            new[] { 10, 12 },
                            new[] { 13, 80 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            129,
                            9,
                            "mast 1 / mizzen topmast 3",
                            10,
                            new Vector3(0.00116f, 0.00513f, 0.97319f),
                            80,
                            new Vector3(-0.00000f, -0.00000f, 0.38279f),
                            false,
                            0,
                            new[] { 10, 12, 80 },
                            new[] { 13 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            130,
                            9,
                            "topmast 1 / mizzen mast",
                            10,
                            new Vector3(0.00116f, 0.00513f, -4.94033f),
                            12,
                            new Vector3(0.00116f, 0.00513f, 1.48931f),
                            false,
                            0,
                            new[] { 10, 12, 13 },
                            new[] { 80 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            131,
                            9,
                            "topmast 1 / mizzen topmast 3",
                            13,
                            new Vector3(-0.00000f, 0.00000f, -8.42753f),
                            80,
                            new Vector3(-0.00000f, -0.00000f, 0.38279f),
                            false,
                            0,
                            new[] { 10, 12, 13, 80 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            132,
                            54,
                            "mast 1 / mizzen mast 2",
                            10,
                            new Vector3(0.00116f, 0.00513f, -2.46726f),
                            55,
                            new Vector3(0.00116f, 0.00513f, 2.18743f),
                            false,
                            2,
                            new[] { 10, 55 },
                            new[] { 13, 59 }
                        ),
                        // 63.23 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            133,
                            54,
                            "mast 1 / mizzen topmast 1",
                            10,
                            new Vector3(0.00116f, 0.00513f, 3.43000f),
                            59,
                            new Vector3(0.00000f, 0.00000f, 0.37763f),
                            false,
                            0,
                            new[] { 10, 55, 59 },
                            new[] { 13 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            134,
                            54,
                            "topmast 1 / mizzen mast 2",
                            10,
                            new Vector3(0.00116f, 0.00513f, -2.46726f),
                            55,
                            new Vector3(0.00116f, 0.00513f, 2.18743f),
                            false,
                            2,
                            new[] { 10, 13, 55 },
                            new[] { 59 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            135,
                            54,
                            "topmast 1 / mizzen topmast 1",
                            13,
                            new Vector3(0.00000f, 0.00000f, -4.27195f),
                            59,
                            new Vector3(0.00000f, 0.00000f, 0.37763f),
                            false,
                            0,
                            new[] { 10, 13, 55, 59 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            136,
                            66,
                            "mast 2 / mizzen mast 2",
                            11,
                            new Vector3(0.00116f, 0.00513f, -0.70023f),
                            55,
                            new Vector3(0.00116f, 0.00513f, 2.18743f),
                            false,
                            2,
                            new[] { 11, 55 },
                            new[] { 14, 59 }
                        ),
                        // 50.32 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            137,
                            66,
                            "mast 2 / mizzen topmast 1",
                            11,
                            new Vector3(0.00116f, 0.00513f, 3.43000f),
                            59,
                            new Vector3(0.00000f, 0.00000f, 0.37763f),
                            false,
                            0,
                            new[] { 11, 55, 59 },
                            new[] { 14 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            138,
                            66,
                            "topmast 2 / mizzen mast 2",
                            11,
                            new Vector3(0.00116f, 0.00513f, -0.70023f),
                            55,
                            new Vector3(0.00116f, 0.00513f, 2.18743f),
                            false,
                            2,
                            new[] { 11, 14, 55 },
                            new[] { 59 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            139,
                            66,
                            "topmast 2 / mizzen topmast 1",
                            14,
                            new Vector3(0.00000f, 0.00000f, -2.50331f),
                            59,
                            new Vector3(0.00000f, 0.00000f, 0.37763f),
                            false,
                            0,
                            new[] { 11, 14, 55, 59 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            140,
                            61,
                            "mast 1 / mizzen mast 3",
                            10,
                            new Vector3(0.00116f, 0.00513f, -1.37176f),
                            69,
                            new Vector3(0.00116f, 0.00513f, 2.18743f),
                            false,
                            2,
                            new[] { 10, 69 },
                            new[] { 13, 70 }
                        ),
                        // 56.31 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            141,
                            61,
                            "mast 1 / mizzen topmast 2",
                            10,
                            new Vector3(0.00116f, 0.00513f, 3.43000f),
                            70,
                            new Vector3(-0.00000f, -0.00000f, 0.38279f),
                            false,
                            0,
                            new[] { 10, 69, 70 },
                            new[] { 13 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            142,
                            61,
                            "topmast 1 / mizzen mast 3",
                            10,
                            new Vector3(0.00116f, 0.00513f, -1.37176f),
                            69,
                            new Vector3(0.00116f, 0.00513f, 2.18743f),
                            false,
                            2,
                            new[] { 10, 13, 69 },
                            new[] { 70 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            143,
                            61,
                            "topmast 1 / mizzen topmast 2",
                            13,
                            new Vector3(-0.00000f, -0.00000f, -3.17129f),
                            70,
                            new Vector3(-0.00000f, -0.00000f, 0.38279f),
                            false,
                            0,
                            new[] { 10, 13, 69, 70 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            144,
                            81,
                            "mast 2 / mizzen mast",
                            11,
                            new Vector3(0.00116f, 0.00513f, -3.67355f),
                            12,
                            new Vector3(0.00116f, 0.00513f, 1.48931f),
                            false,
                            0,
                            new[] { 11, 12 },
                            new[] { 14, 80 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            145,
                            81,
                            "mast 2 / mizzen topmast 3",
                            11,
                            new Vector3(0.00116f, 0.00513f, 2.23960f),
                            80,
                            new Vector3(-0.00000f, -0.00000f, 0.38279f),
                            false,
                            0,
                            new[] { 11, 12, 80 },
                            new[] { 14 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            146,
                            81,
                            "topmast 2 / mizzen mast",
                            11,
                            new Vector3(0.00116f, 0.00513f, -3.67355f),
                            12,
                            new Vector3(0.00116f, 0.00513f, 1.48931f),
                            false,
                            0,
                            new[] { 11, 12, 14 },
                            new[] { 80 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            147,
                            81,
                            "topmast 2 / mizzen topmast 3",
                            14,
                            new Vector3(-0.00000f, -0.00000f, -7.15996f),
                            80,
                            new Vector3(-0.00000f, -0.00000f, 0.38279f),
                            false,
                            0,
                            new[] { 11, 12, 14, 80 },
                            new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    "Foremast / mainmast",
                    new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            148,
                            57,
                            "foremast 1 / mast 2",
                            51,
                            new Vector3(0.00116f, 0.00513f, -0.79138f),
                            11,
                            new Vector3(0.00116f, 0.00513f, 2.18743f),
                            false,
                            2,
                            new[] { 11, 51 },
                            new[] { 14, 64 }
                        ),
                        // 51.31 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            149,
                            57,
                            "foremast 1 / topmast 2",
                            51,
                            new Vector3(0.00116f, 0.00513f, 3.43000f),
                            14,
                            new Vector3(0.00000f, -0.00000f, 0.38279f),
                            false,
                            0,
                            new[] { 11, 14, 51 },
                            new[] { 64 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            150,
                            57,
                            "fore topmast / mast 2",
                            51,
                            new Vector3(0.00116f, 0.00513f, -0.79138f),
                            11,
                            new Vector3(0.00116f, 0.00513f, 2.18743f),
                            false,
                            2,
                            new[] { 11, 51, 64 },
                            new[] { 14 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            151,
                            57,
                            "fore topmast / topmast 2",
                            64,
                            new Vector3(-0.00000f, -0.00000f, -2.59788f),
                            14,
                            new Vector3(0.00000f, -0.00000f, 0.38279f),
                            false,
                            0,
                            new[] { 11, 14, 51, 64 },
                            new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            152,
                            68,
                            "raked foremast / mast 2",
                            62,
                            new Vector3(0.00116f, 0.00513f, -1.39570f),
                            11,
                            new Vector3(0.00116f, 0.00513f, 2.18743f),
                            false,
                            2,
                            new[] { 11, 62 },
                            new[] { 14 }
                        ),
                        // 61.21 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            153,
                            68,
                            "raked foremast / topmast 2",
                            62,
                            new Vector3(0.00116f, 0.00513f, 3.43000f),
                            14,
                            new Vector3(0.00000f, -0.00000f, 0.38279f),
                            false,
                            0,
                            new[] { 11, 14, 62 },
                            new int[0]
                        ),
                    }
                ),
            };
    }
}
