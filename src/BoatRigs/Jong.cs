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
            WinchMounts()
        );

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

        // Installed donor directions in boat space; provenance and numeric fixtures
        // are documented in docs/DEVELOPMENT.md under shared winch placement.
        private static WinchMountDefinition[] WinchMounts() =>
            new[]
            {
                new WinchMountDefinition(1, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 1),
                new WinchMountDefinition(2, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 2),
                new WinchMountDefinition(3, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 3),
                new WinchMountDefinition(4, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 4),
                new WinchMountDefinition(
                    10,
                    WinchRole.Left,
                    new Vector3(0.023180f, 0.999569f, -0.018025f),
                    0.053645f,
                    7,
                    ForeRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    10,
                    WinchRole.Right,
                    new Vector3(0.023180f, 0.999569f, -0.018025f),
                    0.053645f,
                    7,
                    ForeRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    12,
                    WinchRole.Left,
                    new Vector3(0.023180f, 0.999569f, -0.018025f),
                    0.053645f,
                    MainRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    12,
                    WinchRole.Right,
                    new Vector3(0.023180f, 0.999569f, -0.018025f),
                    0.053645f,
                    MainRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    13,
                    WinchRole.Left,
                    new Vector3(0.000703f, 0.999728f, -0.023303f),
                    0.053645f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    13,
                    WinchRole.Right,
                    new Vector3(0.000703f, 0.999800f, 0.020009f),
                    0.053645f,
                    AftRail(WinchRole.Right)
                ),
                new WinchMountDefinition(51, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 51),
                new WinchMountDefinition(52, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 52),
                new WinchMountDefinition(53, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 53),
                new WinchMountDefinition(
                    55,
                    WinchRole.Left,
                    new Vector3(0.023180f, 0.999569f, -0.018025f),
                    0.053645f,
                    MainRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    55,
                    WinchRole.Right,
                    new Vector3(0.023180f, 0.999569f, -0.018025f),
                    0.053645f,
                    MainRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    56,
                    WinchRole.Left,
                    new Vector3(0.000703f, 0.999728f, -0.023303f),
                    0.053645f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    56,
                    WinchRole.Right,
                    new Vector3(0.000703f, 0.999800f, 0.020010f),
                    0.053645f,
                    AftRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    58,
                    WinchRole.Left,
                    new Vector3(0.023180f, 0.999569f, -0.018025f),
                    0.053645f,
                    MainRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    58,
                    WinchRole.Right,
                    new Vector3(0.023180f, 0.999569f, -0.018025f),
                    0.053645f,
                    MainRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    71,
                    WinchRole.Left,
                    new Vector3(0.000703f, 0.999728f, -0.023303f),
                    0.053645f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    71,
                    WinchRole.Right,
                    new Vector3(0.000703f, 0.999800f, 0.020010f),
                    0.053645f,
                    AftRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    73,
                    WinchRole.Left,
                    new Vector3(0.023180f, 0.999569f, -0.018025f),
                    0.053645f,
                    MainRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    73,
                    WinchRole.Right,
                    new Vector3(0.023180f, 0.999569f, -0.018025f),
                    0.053645f,
                    MainRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    75,
                    WinchRole.Left,
                    new Vector3(0.000703f, 0.999728f, -0.023303f),
                    0.053645f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    75,
                    WinchRole.Right,
                    new Vector3(0.000703f, 0.999800f, 0.020010f),
                    0.053645f,
                    AftRail(WinchRole.Right)
                ),
            };

        // Donor 10's band is blocked on port by native and SE fittings. Clone
        // native lower-stay (7) sheets instead, on the opposed lower trim_010
        // caps. Restrict both sides to those caps so they remain symmetric.
        private static WinchSurfaceSegment[] ForeRail(WinchRole role) =>
            role == WinchRole.Left
                ? new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(-3.050210f, 4.079560f, 4.135915f),
                        new Vector3(-3.186495f, 4.105650f, 0.674695f),
                        new Vector3(0.000441f, 0.999972f, 0.007520f)
                    ),
                }
                : new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(3.050210f, 4.079560f, 4.135915f),
                        new Vector3(3.186500f, 4.105650f, 0.674695f),
                        new Vector3(-0.000441f, 0.999972f, 0.007520f)
                    ),
                };

        // Measured solid support: trim_010.
        private static WinchSurfaceSegment[] MainRail(WinchRole role) =>
            role == WinchRole.Left
                ? new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(-3.223645f, 4.105900f, -1.658675f),
                        new Vector3(-3.072725f, 4.105900f, -4.373055f),
                        new Vector3(0.000000f, 1.000000f, 0.000000f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-3.067520f, 4.105900f, -4.439160f),
                        new Vector3(-2.554765f, 4.105900f, -9.475065f),
                        new Vector3(0.000000f, 1.000000f, 0.000000f)
                    ),
                }
                : new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(3.223650f, 4.105900f, -1.658675f),
                        new Vector3(3.072725f, 4.105900f, -4.373050f),
                        new Vector3(0.000000f, 1.000000f, 0.000000f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(3.067520f, 4.105900f, -4.439160f),
                        new Vector3(2.554770f, 4.105900f, -9.475065f),
                        new Vector3(0.000000f, 1.000000f, 0.000000f)
                    ),
                };

        // Measured solid support: trim_010.
        private static WinchSurfaceSegment[] AftRail(WinchRole role) =>
            role == WinchRole.Left
                ? new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(-2.460285f, 4.282390f, -10.292015f),
                        new Vector3(-2.223185f, 4.312770f, -11.575575f),
                        new Vector3(0.000445f, 0.999718f, 0.023744f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-2.209350f, 4.312210f, -11.641805f),
                        new Vector3(-2.074360f, 4.288835f, -12.219070f),
                        new Vector3(0.004402f, 0.999213f, -0.039431f)
                    ),
                }
                : new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(2.460290f, 4.282390f, -10.292015f),
                        new Vector3(2.223190f, 4.312770f, -11.575575f),
                        new Vector3(-0.000445f, 0.999718f, 0.023744f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(2.209355f, 4.312210f, -11.641805f),
                        new Vector3(2.074360f, 4.288835f, -12.219070f),
                        new Vector3(-0.004401f, 0.999213f, -0.039431f)
                    ),
                };

        // Authored from installed Jong and Shipyard Expansion assets.
        // Endpoint vectors are local to the named physical mast section.
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
