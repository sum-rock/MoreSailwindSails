// Owns Shroud's authored fallback placements.
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class ShroudControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Shroud.Definition,
            sheetFallbacks: SheetFallbacks(),
            halyardFallbacks: HalyardFallbacks()
        );

        private static HalyardFallbackDefinition[] HalyardFallbacks()
        {
            // Shattered Seas 0.1.3 native reef coils, inspected 2026-10-21.
            // Both heights of each mast share a donor and the same halyard stand.
            var foremast = new HalyardFallbackSeat(
                // F9 #10, Halyard_Points (1)/Cube.006, 2026-10-21.
                contact: new Vector3(-0.983793f, 4.811661f, 12.544150f),
                normal: new Vector3(-0.996100f, 0f, -0.088232f),
                sourceNormal: new Vector3(-0.11218130f, 0.39073121f, -0.91364352f),
                offset: 0.088011f,
                templateIndex: 0
            );
            var mainmast = new HalyardFallbackSeat(
                // F9 #9, Halyard_Points (1)/Rope Stand 1.002, 2026-10-21.
                contact: new Vector3(-1.046992f, 4.529709f, 0.263511f),
                normal: new Vector3(-0.997735f, 0f, -0.067271f),
                sourceNormal: new Vector3(0.91364361f, -0.39073103f, -0.11218120f),
                offset: 0.088011f,
                templateIndex: 0
            );
            var mizzenmast = new HalyardFallbackSeat(
                // F9 #11, Halyard_Points (1)/Cube.005, 2026-10-21.
                contact: new Vector3(-0.003264f, 5.730850f, -11.788850f),
                normal: new Vector3(-0.117672f, 0f, -0.993053f),
                sourceNormal: new Vector3(0.91364361f, -0.39073103f, -0.11218120f),
                offset: 0.088011f,
                templateIndex: 0
            );
            return new[]
            {
                new HalyardFallbackDefinition(mast: 6, fallback: foremast),
                new HalyardFallbackDefinition(mast: 5, fallback: foremast),
                new HalyardFallbackDefinition(mast: 8, fallback: mainmast),
                new HalyardFallbackDefinition(mast: 7, fallback: mainmast),
                new HalyardFallbackDefinition(mast: 10, fallback: mizzenmast),
                new HalyardFallbackDefinition(mast: 9, fallback: mizzenmast),
            };
        }

        private static SheetFallbackDefinition[] SheetFallbacks() =>
            new SheetFallbackDefinition[]
            {
                new SheetFallbackDefinition(name: "Foremast", fallback: ForemastFallback),
                new SheetFallbackDefinition(
                    name: "Midships",
                    fallback: new SheetFallbackPair(
                        // Same inspected Coil_Foremast_Reef_1 mounting frame as Foremast.
                        templateMast: 6,
                        templateRole: WinchRole.Reef,
                        templateIndex: 0,
                        sourceNormal: new Vector3(-0.1121813f, 0.39073121f, -0.91364352f),
                        offset: 0.088011f,
                        // F9 #17/#16, 2026-10-21; average |X|, Y/Z and reflected normals.
                        port: new SheetFallbackSeat(
                            contact: new Vector3(-3.5188915f, 4.2685165f, -1.316389f),
                            normal: new Vector3(0.998513f, 0.000818f, 0.054512f)
                        ),
                        starboard: new SheetFallbackSeat(
                            contact: new Vector3(3.5188915f, 4.2685165f, -1.316389f),
                            normal: new Vector3(-0.998513f, 0.000818f, 0.054512f)
                        )
                    )
                ),
                new SheetFallbackDefinition(
                    name: "Aft",
                    fallback: new SheetFallbackPair(
                        // Same inspected Coil_Foremast_Reef_1 mounting frame as Foremast.
                        templateMast: 6,
                        templateRole: WinchRole.Reef,
                        templateIndex: 0,
                        sourceNormal: new Vector3(-0.1121813f, 0.39073121f, -0.91364352f),
                        offset: 0.088011f,
                        // F9 #14/#15, 2026-10-21; average |X|, Y/Z and reflected normals.
                        port: new SheetFallbackSeat(
                            contact: new Vector3(-3.069871f, 5.7209395f, -8.541576f),
                            normal: new Vector3(-0.0043215f, 0.999185f, 0.040142f)
                        ),
                        starboard: new SheetFallbackSeat(
                            contact: new Vector3(3.069871f, 5.7209395f, -8.541576f),
                            normal: new Vector3(0.0043215f, 0.999185f, 0.040142f)
                        )
                    )
                ),
                new SheetFallbackDefinition(
                    name: "Stern",
                    fallback: new SheetFallbackPair(
                        // Same inspected Coil_Foremast_Reef_1 mounting frame as Foremast.
                        templateMast: 6,
                        templateRole: WinchRole.Reef,
                        templateIndex: 0,
                        sourceNormal: new Vector3(-0.1121813f, 0.39073121f, -0.91364352f),
                        offset: 0.088011f,
                        // F9 #13/#12, 2026-10-21; average |X|, Y/Z and reflected normals.
                        port: new SheetFallbackSeat(
                            contact: new Vector3(-2.4212675f, 5.994309f, -13.336645f),
                            normal: new Vector3(-0.024844f, 0.992603f, 0.118836f)
                        ),
                        starboard: new SheetFallbackSeat(
                            contact: new Vector3(2.4212675f, 5.994309f, -13.336645f),
                            normal: new Vector3(0.024844f, 0.992603f, 0.118836f)
                        )
                    )
                ),
            };

        private static SheetFallbackPair ForemastFallback =>
            new SheetFallbackPair(
                // Shattered Seas 0.1.3: Coil_Foremast_Reef_1; inspected 2026-10-20.
                // Boat-local +Z normal and rendered rear depth at native scale.
                templateMast: 6,
                templateRole: WinchRole.Reef,
                templateIndex: 0,
                sourceNormal: new Vector3(-0.1121813f, 0.39073121f, -0.91364352f),
                offset: 0.088011f,
                port: new SheetFallbackSeat(
                    contact: new Vector3(-3.465970f, 4.829765f, -3.901223f),
                    normal: Vector3.up
                ),
                starboard: new SheetFallbackSeat(
                    contact: new Vector3(3.465970f, 4.829765f, -3.901223f),
                    normal: Vector3.up
                )
            );
    }
}
