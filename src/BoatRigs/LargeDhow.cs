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
            WinchMounts(),
            SheetCategories()
        );

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    "Foremast",
                    new[] { 0, 1 },
                    new[] { 0, 1, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 },
                    null
                ),
                new SheetWinchCategory(
                    "Mainmast",
                    new[] { 2, 4 },
                    new[] { 2, 4, 3, 5, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43 },
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
        // bounded positions around the lower fitting are blocked by native rows.
        private static WinchMountDefinition[] WinchMounts() =>
            new[]
            {
                new WinchMountDefinition(
                    0,
                    WinchRole.Reef,
                    new Vector3(0.000000f, 1.000000f, 0.000000f),
                    true,
                    0,
                    2
                ),
                new WinchMountDefinition(
                    1,
                    WinchRole.Reef,
                    new Vector3(0.000000f, 0.984808f, 0.173648f),
                    true,
                    1,
                    2
                ),
                new WinchMountDefinition(
                    2,
                    WinchRole.Reef,
                    new Vector3(0.000000f, 1.000000f, 0.000000f),
                    true,
                    2,
                    2
                ),
                new WinchMountDefinition(
                    3,
                    WinchRole.Reef,
                    new Vector3(0.000000f, 1.000000f, 0.000000f),
                    true,
                    2
                ),
                new WinchMountDefinition(
                    4,
                    WinchRole.Reef,
                    new Vector3(0.000000f, 1.000000f, 0.000000f),
                    true,
                    4,
                    2
                ),
                new WinchMountDefinition(
                    5,
                    WinchRole.Reef,
                    new Vector3(0.000000f, 1.000000f, 0.000000f),
                    true,
                    4
                ),
                new WinchMountDefinition(
                    6,
                    WinchRole.Reef,
                    new Vector3(0.000000f, 1.000000f, 0.000000f),
                    true,
                    6
                ),
                new WinchMountDefinition(
                    7,
                    WinchRole.Reef,
                    new Vector3(0.000000f, 0.994277f, -0.106834f),
                    true,
                    7
                ),
                new WinchMountDefinition(
                    8,
                    WinchRole.Reef,
                    new Vector3(0.000000f, 1.000000f, 0.000000f),
                    true,
                    8
                ),
                new WinchMountDefinition(
                    20,
                    WinchRole.Left,
                    new Vector3(0.016009f, 0.999810f, -0.011145f),
                    0.065662f,
                    MiddleRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    20,
                    WinchRole.Right,
                    new Vector3(0.003479f, 0.999848f, 0.017104f),
                    0.065662f,
                    MiddleRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    23,
                    WinchRole.Left,
                    new Vector3(0.016009f, 0.999810f, -0.011145f),
                    0.065662f,
                    MiddleRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    23,
                    WinchRole.Right,
                    new Vector3(0.003479f, 0.999848f, 0.017104f),
                    0.065662f,
                    MiddleRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    26,
                    WinchRole.Left,
                    new Vector3(-0.001925f, 0.951514f, -0.307601f),
                    0.065662f,
                    ForwardMiddleRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    26,
                    WinchRole.Right,
                    new Vector3(0.000000f, 0.957008f, -0.290061f),
                    0.065662f,
                    ForwardMiddleRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    29,
                    WinchRole.Left,
                    new Vector3(-0.001925f, 0.951514f, -0.307601f),
                    0.065662f,
                    ForwardMiddleRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    29,
                    WinchRole.Right,
                    new Vector3(0.000000f, 0.957008f, -0.290061f),
                    0.065662f,
                    ForwardMiddleRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    32,
                    WinchRole.Left,
                    new Vector3(-0.999721f, 0.023374f, 0.003527f),
                    0.525562f,
                    AftSheetRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    32,
                    WinchRole.Right,
                    new Vector3(0.997499f, 0.049775f, -0.050189f),
                    0.525562f,
                    AftSheetRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    34,
                    WinchRole.Left,
                    new Vector3(-0.999721f, 0.023374f, 0.003527f),
                    0.525562f,
                    AftSheetRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    34,
                    WinchRole.Right,
                    new Vector3(0.997499f, 0.049775f, -0.050189f),
                    0.525562f,
                    AftSheetRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    36,
                    WinchRole.Left,
                    new Vector3(-0.999721f, 0.023374f, 0.003527f),
                    0.525562f,
                    AftSheetRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    36,
                    WinchRole.Right,
                    new Vector3(0.997499f, 0.049775f, -0.050189f),
                    0.525562f,
                    AftSheetRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    38,
                    WinchRole.Left,
                    new Vector3(-0.999721f, 0.023374f, 0.003527f),
                    0.525562f,
                    AftSheetRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    38,
                    WinchRole.Right,
                    new Vector3(0.997499f, 0.049775f, -0.050189f),
                    0.525562f,
                    AftSheetRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    40,
                    WinchRole.Left,
                    new Vector3(0.005720f, 0.999977f, 0.003606f),
                    0.065662f,
                    ForwardMizzenRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    40,
                    WinchRole.Right,
                    new Vector3(0.000000f, 0.999991f, -0.004350f),
                    0.065662f,
                    ForwardMizzenRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    42,
                    WinchRole.Left,
                    new Vector3(0.005720f, 0.999977f, 0.003606f),
                    0.065662f,
                    ForwardMizzenRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    42,
                    WinchRole.Right,
                    new Vector3(0.000000f, 0.999991f, -0.004350f),
                    0.065662f,
                    ForwardMizzenRail(WinchRole.Right)
                ),
            };

        // Solid rail-cap faces from Cube_001.
        private static WinchSurfaceSegment[] MiddleRail(WinchRole role) =>
            role == WinchRole.Left
                ? new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(-2.842428f, 2.396628f, 5.867308f),
                        new Vector3(-2.984101f, 2.288556f, -0.090174f),
                        new Vector3(0.000000f, 0.999836f, -0.018138f)
                    ),
                }
                : new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(2.842431f, 2.396627f, 5.867307f),
                        new Vector3(2.984102f, 2.288556f, -0.090175f),
                        new Vector3(0.000000f, 0.999836f, -0.018138f)
                    ),
                };

        // Solid rail-cap faces from Cube_001.
        private static WinchSurfaceSegment[] ForwardMiddleRail(WinchRole role) =>
            role == WinchRole.Left
                ? new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(-2.593796f, 3.050338f, 8.272216f),
                        new Vector3(-2.786983f, 2.489596f, 6.403616f),
                        new Vector3(0.000000f, 0.957803f, -0.287424f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-2.842428f, 2.396628f, 5.867308f),
                        new Vector3(-2.984101f, 2.288556f, -0.090174f),
                        new Vector3(0.000000f, 0.999836f, -0.018138f)
                    ),
                }
                : new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(2.593799f, 3.050338f, 8.272216f),
                        new Vector3(2.786986f, 2.489595f, 6.403615f),
                        new Vector3(0.000000f, 0.957803f, -0.287424f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(2.842431f, 2.396627f, 5.867307f),
                        new Vector3(2.984102f, 2.288556f, -0.090175f),
                        new Vector3(0.000000f, 0.999836f, -0.018138f)
                    ),
                };

        // Solid inboard rail faces; keep these large sheet fittings accessible from the deck.
        private static WinchSurfaceSegment[] AftSheetRail(WinchRole role) =>
            role == WinchRole.Left
                ? new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(-2.801353f, 3.386376f, -6.000000f),
                        new Vector3(-2.693516f, 3.457779f, -8.955000f),
                        new Vector3(0.999335f, 0.000000f, 0.036469f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-2.801352f, 3.425882f, -1.915426f),
                        new Vector3(-2.801353f, 3.386376f, -6.000000f),
                        new Vector3(1.000000f, 0.000000f, 0.000000f)
                    ),
                }
                : new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(2.801352f, 3.386375f, -6.000001f),
                        new Vector3(2.693513f, 3.457778f, -8.955001f),
                        new Vector3(-0.999335f, 0.000000f, 0.036469f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(2.801353f, 3.425881f, -1.915427f),
                        new Vector3(2.801352f, 3.386375f, -6.000001f),
                        new Vector3(-1.000000f, 0.000000f, 0.000000f)
                    ),
                };

        // Solid rail-cap faces from Cube_008.
        private static WinchSurfaceSegment[] ForwardMizzenRail(WinchRole role) =>
            role == WinchRole.Left
                ? new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(-2.966188f, 3.551913f, -1.915426f),
                        new Vector3(-2.966189f, 3.512407f, -6.000000f),
                        new Vector3(0.000000f, 0.999953f, -0.009671f)
                    ),
                }
                : new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(2.966188f, 3.551912f, -1.915427f),
                        new Vector3(2.966188f, 3.512406f, -6.000001f),
                        new Vector3(0.000000f, 0.999953f, -0.009671f)
                    ),
                };

        // Endpoints are authored in each physical mast section's local frame.
        // Foremast mesh ends at z=2.223295; its capsule extends 6.7 cm higher.
        // Native topmast halyards use the lower mainmast guide; retain that height.
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
