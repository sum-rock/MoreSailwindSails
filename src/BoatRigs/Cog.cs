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
            WinchMounts(),
            SheetCategories()
        );

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    "Mainmast",
                    new[] { 6, 5, 56, 59, 63 },
                    new[] { 6, 5, 56, 59, 63, 58 },
                    null
                ),
                new SheetWinchCategory("Foremast", new[] { 68 }, new[] { 68 }, null),
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

        // Installed donor directions in boat space; provenance and numeric fixtures
        // are documented in docs/DEVELOPMENT.md under shared winch placement.
        private static WinchMountDefinition[] WinchMounts() =>
            new[]
            {
                new WinchMountDefinition(5, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 5),
                new WinchMountDefinition(8, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 8),
                new WinchMountDefinition(
                    51,
                    WinchRole.Left,
                    new Vector3(0.080921f, 0.992272f, 0.094062f),
                    0.057454f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    51,
                    WinchRole.Right,
                    new Vector3(-0.103256f, 0.992379f, 0.067245f),
                    0.057454f,
                    AftRail(WinchRole.Right)
                ),
                new WinchMountDefinition(57, WinchRole.Reef, new Vector3(0f, 1f, 0f), true, 57),
                new WinchMountDefinition(
                    58,
                    WinchRole.Left,
                    new Vector3(0.080921f, 0.992272f, 0.094062f),
                    0.057454f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    58,
                    WinchRole.Right,
                    new Vector3(-0.103256f, 0.992379f, 0.067245f),
                    0.057454f,
                    AftRail(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    65,
                    WinchRole.Left,
                    new Vector3(0.080921f, 0.992272f, 0.094062f),
                    0.057454f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    65,
                    WinchRole.Right,
                    new Vector3(-0.103256f, 0.992379f, 0.067245f),
                    0.057454f,
                    AftRail(WinchRole.Right)
                ),
            };

        // Measured solid support: trim_001.
        private static WinchSurfaceSegment[] AftRail(WinchRole role) =>
            role == WinchRole.Left
                ? new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(-1.938420f, 2.440695f, -2.586095f),
                        new Vector3(-1.899160f, 2.540430f, -3.484050f),
                        new Vector3(0.002328f, 0.993874f, 0.110490f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-1.896715f, 2.544770f, -3.530110f),
                        new Vector3(-1.793510f, 2.672010f, -5.174305f),
                        new Vector3(0.066976f, 0.994448f, 0.081162f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-1.790820f, 2.675890f, -5.220275f),
                        new Vector3(-1.727715f, 2.782630f, -6.387575f),
                        new Vector3(0.145756f, 0.984465f, 0.097901f)
                    ),
                }
                : new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(1.938420f, 2.440695f, -2.586095f),
                        new Vector3(1.899160f, 2.540430f, -3.484050f),
                        new Vector3(-0.002328f, 0.993874f, 0.110490f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(1.896715f, 2.544770f, -3.530110f),
                        new Vector3(1.793510f, 2.672010f, -5.174305f),
                        new Vector3(-0.066976f, 0.994448f, 0.081162f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(1.790825f, 2.675890f, -5.220275f),
                        new Vector3(1.727715f, 2.782630f, -6.387575f),
                        new Vector3(-0.145758f, 0.984464f, 0.097902f)
                    ),
                };

        // Authored from installed Cog and Shipyard Expansion assets.
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
