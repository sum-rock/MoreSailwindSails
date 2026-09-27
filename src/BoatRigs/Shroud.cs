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
            WinchMounts(),
            SheetCategories()
        );

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    "Foremast",
                    new[] { 6, 5 },
                    new[] { 6, 5, 16, 17, 24 },
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

        // Installed donor directions in boat space; provenance and numeric fixtures
        // are documented in docs/DEVELOPMENT.md under shared winch placement.
        private static WinchMountDefinition[] WinchMounts() =>
            new[]
            {
                new WinchMountDefinition(7, WinchRole.Reef, 0.14f, ReefPins("Mainmast", false)),
                new WinchMountDefinition(8, WinchRole.Reef, 0.14f, ReefPins("Mainmast", false)),
                new WinchMountDefinition(9, WinchRole.Reef, 0.14f, ReefPins("Mizzenmast", true)),
                new WinchMountDefinition(10, WinchRole.Reef, 0.14f, ReefPins("Mizzenmast", true)),
                new WinchMountDefinition(
                    24,
                    WinchRole.Left,
                    new Vector3(-0.006981f, 0.999976f, -0.000000f),
                    0.076685f,
                    MainSheetPoint(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    24,
                    WinchRole.Right,
                    new Vector3(-0.006981f, 0.999976f, -0.000000f),
                    0.076685f,
                    MainSheetPoint(WinchRole.Right)
                ),
                new WinchMountDefinition(
                    25,
                    WinchRole.Left,
                    new Vector3(-0.006982f, 0.997142f, 0.075225f),
                    0.076685f,
                    AftRail(WinchRole.Left)
                ),
                new WinchMountDefinition(
                    25,
                    WinchRole.Right,
                    new Vector3(-0.006982f, 0.997142f, 0.075225f),
                    0.076685f,
                    AftRail(WinchRole.Right)
                ),
            };

        // Captures 1/3 centred across the visible Clipper_Upper_Trim cap and
        // mirrored at their mean longitudinal position. The collider is 1 cm lower.
        private static WinchSurfacePoint MainSheetPoint(WinchRole role) =>
            new WinchSurfacePoint(
                new Vector3(role == WinchRole.Left ? -3.465970f : 3.465970f, 4.829765f, -3.901223f),
                Vector3.up
            );

        // Measured solid support: Clipper_Upper_Trim.
        private static WinchSurfaceSegment[] AftRail(WinchRole role) =>
            role == WinchRole.Left
                ? new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(-2.138915f, 6.125940f, -14.791540f),
                        new Vector3(-1.946805f, 6.186030f, -15.501990f),
                        new Vector3(-0.016682f, 0.996672f, 0.079788f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-2.346065f, 6.068880f, -13.846190f),
                        new Vector3(-2.138915f, 6.125940f, -14.791540f),
                        new Vector3(-0.012046f, 0.998266f, 0.057614f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(-2.593680f, 5.846370f, -12.051000f),
                        new Vector3(-2.346065f, 6.068880f, -13.846190f),
                        new Vector3(-0.019018f, 0.992543f, 0.120400f)
                    ),
                }
                : new[]
                {
                    new WinchSurfaceSegment(
                        new Vector3(2.593740f, 5.846370f, -12.051000f),
                        new Vector3(2.346125f, 6.068880f, -13.846190f),
                        new Vector3(0.019018f, 0.992543f, 0.120400f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(2.346125f, 6.068880f, -13.846190f),
                        new Vector3(2.138975f, 6.125940f, -14.791540f),
                        new Vector3(0.012046f, 0.998266f, 0.057614f)
                    ),
                    new WinchSurfaceSegment(
                        new Vector3(2.138975f, 6.125940f, -14.791540f),
                        new Vector3(1.946865f, 6.186030f, -15.501990f),
                        new Vector3(0.016682f, 0.996672f, 0.079788f)
                    ),
                };

        // Six seats on each side of each existing rack. Mizzen main reef 6 is
        // on top of the beam; number 7 is its sixth side pin instead.
        private static string[] ReefPins(string mast, bool mizzen)
        {
            var pins = new string[12];
            for (int i = 0; i < 6; i++)
            {
                pins[i] = "Coil_" + mast + "_Reef_" + (mizzen && i == 5 ? 7 : i + 1);
                pins[i + 6] = "Coil_" + mast + "_Jib_Reef_" + (i + 1);
            }
            return pins;
        }

        // Authored from installed Shroud and Shipyard Expansion assets.
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
