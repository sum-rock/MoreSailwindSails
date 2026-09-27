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
            WinchMounts(),
            SheetCategories()
        );

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    "Foremast",
                    new[] { 9, 58, 69 },
                    new[] { 9, 58, 62, 69, 15, 16, 61 },
                    null
                ),
                new SheetWinchCategory(
                    "Mainmast",
                    new[] { 10, 11, 70 },
                    new[] { 10, 11, 70, 5, 6, 7, 52, 65, 66 },
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

        // Installed donor directions in boat space; provenance and numeric fixtures
        // are documented in docs/DEVELOPMENT.md under shared winch placement.
        private static WinchMountDefinition[] WinchMounts() =>
            new[]
            {
                new WinchMountDefinition(
                    5,
                    WinchRole.Left,
                    new Vector3(0.099430f, 0.993358f, 0.057917f),
                    0.059995f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    5,
                    WinchRole.Right,
                    new Vector3(-0.138346f, 0.987447f, 0.076213f),
                    0.059995f,
                    AftRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    6,
                    WinchRole.Left,
                    new Vector3(0.099430f, 0.993358f, 0.057917f),
                    0.059995f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    6,
                    WinchRole.Right,
                    new Vector3(-0.138346f, 0.987447f, 0.076213f),
                    0.059995f,
                    AftRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    7,
                    WinchRole.Left,
                    new Vector3(0.088613f, 0.995247f, 0.040388f),
                    0.059995f,
                    MainRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    7,
                    WinchRole.Right,
                    new Vector3(-0.041909f, 0.998363f, 0.038916f),
                    0.059995f,
                    MainRail(WinchRole.Right)
                ),
                new WinchMountDefinition(9, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 9),
                new WinchMountDefinition(
                    10,
                    WinchRole.Reef,
                    new Vector3(-0.057545f, 0.985028f, -0.162504f),
                    0.055177f,
                    ReefBeam()
                ),
                new WinchMountDefinition(
                    11,
                    WinchRole.Reef,
                    new Vector3(-0.057545f, 0.985028f, -0.162504f),
                    0.055177f,
                    ReefBeam()
                ),
                new WinchMountDefinition(12, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 12),
                new WinchMountDefinition(13, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 13),
                new WinchMountDefinition(
                    16,
                    WinchRole.Left,
                    new Vector3(0.067725f, 0.997686f, -0.005921f),
                    0.059995f,
                    ForeRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    16,
                    WinchRole.Right,
                    new Vector3(-0.059001f, 0.998218f, 0.008946f),
                    0.059995f,
                    ForeRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    52,
                    WinchRole.Left,
                    new Vector3(0.088613f, 0.995247f, 0.040388f),
                    0.059995f,
                    MainRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    52,
                    WinchRole.Right,
                    new Vector3(-0.041909f, 0.998363f, 0.038916f),
                    0.059995f,
                    MainRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    53,
                    WinchRole.Reef,
                    new Vector3(-0.000349f, 1f, 0f),
                    true,
                    53
                ),
                new WinchMountDefinition(57, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 57),
                new WinchMountDefinition(
                    58,
                    WinchRole.Reef,
                    new Vector3(0f, 0.965926f, 0.258819f),
                    true,
                    58
                ),
                new WinchMountDefinition(
                    61,
                    WinchRole.Left,
                    new Vector3(0.067725f, 0.997686f, -0.005921f),
                    0.059995f,
                    ForeRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    61,
                    WinchRole.Right,
                    new Vector3(-0.059001f, 0.998218f, 0.008946f),
                    0.059995f,
                    ForeRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    65,
                    WinchRole.Left,
                    new Vector3(0.099430f, 0.993358f, 0.057917f),
                    0.059995f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    65,
                    WinchRole.Right,
                    new Vector3(-0.138347f, 0.987447f, 0.076213f),
                    0.059995f,
                    AftRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    66,
                    WinchRole.Left,
                    new Vector3(0.099430f, 0.993358f, 0.057917f),
                    0.059995f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    66,
                    WinchRole.Right,
                    new Vector3(-0.138347f, 0.987447f, 0.076213f),
                    0.059995f,
                    AftRail(WinchRole.Right)
                ),
            };

        // Measured solid support: trim_001.
        private static WinchSurfaceSegment[] ForeRail(WinchRole role) =>
            role == WinchRole.Left
                ? new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(-2.628225f, 2.397060f, 5.002665f),
                        new Vector3(-2.753665f, 2.371230f, 2.825295f),
                        new Vector3(-0.000111f, 0.999930f, -0.011856f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-2.755235f, 2.370960f, 2.781305f),
                        new Vector3(-2.798305f, 2.370960f, -0.293320f),
                        new Vector3(0.000000f, 1.000000f, 0.000000f)
                    ),
                }
                : new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(2.755245f, 2.370960f, 2.781295f),
                        new Vector3(2.798300f, 2.370960f, -0.293335f),
                        new Vector3(0.000000f, 1.000000f, 0.000000f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(2.628245f, 2.397060f, 5.002655f),
                        new Vector3(2.753675f, 2.371220f, 2.825285f),
                        new Vector3(0.000111f, 0.999930f, -0.011860f)
                    ),
                };

        // Measured solid support: Cube_035.
        private static WinchSurfaceSegment[] MainRail(WinchRole role) =>
            role == WinchRole.Left
                ? new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(-2.718665f, 3.429095f, -3.021380f),
                        new Vector3(-2.687885f, 3.426800f, -3.669960f),
                        new Vector3(-0.025734f, 0.999657f, -0.004759f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-2.686300f, 3.426850f, -3.691960f),
                        new Vector3(-2.604715f, 3.433735f, -4.537545f),
                        new Vector3(-0.011194f, 0.999912f, 0.007062f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-2.601785f, 3.433650f, -4.559580f),
                        new Vector3(-2.471120f, 3.421210f, -5.345955f),
                        new Vector3(0.016242f, 0.999782f, -0.013117f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-2.768610f, 2.922770f, -1.349910f),
                        new Vector3(-2.719500f, 3.425925f, -2.999850f),
                        new Vector3(-0.024620f, 0.956427f, 0.290933f)
                    ),
                }
                : new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(2.768600f, 2.922770f, -1.349920f),
                        new Vector3(2.719485f, 3.425925f, -2.999865f),
                        new Vector3(0.024629f, 0.956427f, 0.290932f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(2.718650f, 3.429095f, -3.021390f),
                        new Vector3(2.687875f, 3.426800f, -3.669970f),
                        new Vector3(0.025734f, 0.999658f, -0.004758f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(2.686285f, 3.426850f, -3.691970f),
                        new Vector3(2.604700f, 3.433735f, -4.537555f),
                        new Vector3(0.011194f, 0.999912f, 0.007061f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(2.601765f, 3.433645f, -4.559590f),
                        new Vector3(2.471100f, 3.421205f, -5.345965f),
                        new Vector3(-0.016242f, 0.999782f, -0.013117f)
                    ),
                };

        // Measured solid support: trim_001.
        private static WinchSurfaceSegment[] AftRail(WinchRole role) =>
            role == WinchRole.Left
                ? new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(-1.995125f, 3.657265f, -8.089520f),
                        new Vector3(-1.596100f, 3.695950f, -9.326365f),
                        new Vector3(0.174253f, 0.980859f, 0.086895f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-1.582010f, 3.697260f, -9.371070f),
                        new Vector3(-1.218625f, 3.730990f, -10.573880f),
                        new Vector3(0.153202f, 0.985426f, 0.073918f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-1.186505f, 3.734195f, -10.603405f),
                        new Vector3(-0.624895f, 3.789535f, -10.696625f),
                        new Vector3(-0.062395f, 0.976966f, 0.204070f)
                    ),
                }
                : new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(1.995085f, 3.657265f, -8.089530f),
                        new Vector3(1.596065f, 3.695950f, -9.326370f),
                        new Vector3(-0.174251f, 0.980860f, 0.086894f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(1.581975f, 3.697260f, -9.371080f),
                        new Vector3(1.218580f, 3.730990f, -10.573885f),
                        new Vector3(-0.153194f, 0.985428f, 0.073918f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(1.186460f, 3.734195f, -10.603410f),
                        new Vector3(0.624850f, 3.789535f, -10.696630f),
                        new Vector3(0.062394f, 0.976965f, 0.204076f)
                    ),
                };

        // Measured solid support: Cube_032.
        private static WinchSurfaceSegment[] ReefBeam() =>
            new[]
            {
                new WinchSurfaceSegment(
                    new Vector3(-1.476270f, 3.329240f, -3.722440f),
                    new Vector3(-0.655040f, 3.314270f, -3.722440f),
                    new Vector3(0.018226f, 0.999834f, 0.000000f)
                ),
                new WinchSurfaceSegment(
                    new Vector3(-0.655000f, 3.314300f, -3.722450f),
                    new Vector3(-0.110300f, 3.329200f, -3.722450f),
                    new Vector3(-0.027344f, 0.999626f, -0.000000f)
                ),
            };

        // Authored from installed Junk and Shipyard Expansion assets.
        // Endpoint vectors are local to the named physical mast section.
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
