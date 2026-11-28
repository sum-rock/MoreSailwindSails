// Owns Sh'ba's captured sheet pairs and native-cleat halyard fallback placements.
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class ShBaControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.ShBa.Definition,
            sheetFallbacks: SheetFallbacks(),
            halyardFallbacks: HalyardFallbacks()
        );

        // Latest F9 #2-4 identify the unused starboard cabin cleats, not the deck
        // below them. Translate Main/Mizzen Reef 1-3 by the measured post spacing
        // (+1.5905841 m X), retaining the native coil heights and rotations.
        // Contact is the translated rear face; the normal offset restores the
        // native coil pivot so the rope wraps around the cleat instead of the deck.
        private static HalyardFallbackDefinition[] HalyardFallbacks() =>
            new[]
            {
                new HalyardFallbackDefinition(
                    mast: 3,
                    fallback: new HalyardFallbackSeat(
                        contact: new Vector3(0.72280586f, 2.56354018f, -2.52535176f),
                        normal: new Vector3(0.27301129f, -0.96056000f, -0.05281400f),
                        sourceNormal: new Vector3(0.27301129f, -0.96056000f, -0.05281400f),
                        offset: 0.088011f,
                        templateIndex: 0
                    )
                ),
                new HalyardFallbackDefinition(
                    mast: 4,
                    fallback: new HalyardFallbackSeat(
                        contact: new Vector3(0.69577674f, 2.57599087f, -2.87587270f),
                        normal: new Vector3(0.53573838f, -0.84069375f, -0.07885692f),
                        sourceNormal: new Vector3(0.53573838f, -0.84069375f, -0.07885692f),
                        offset: 0.088011f,
                        templateIndex: 1
                    )
                ),
                new HalyardFallbackDefinition(
                    mast: 5,
                    fallback: new HalyardFallbackSeat(
                        contact: new Vector3(0.80986833f, 2.41691802f, -3.21130901f),
                        normal: new Vector3(-0.53725375f, 0.84173477f, -0.05330091f),
                        sourceNormal: new Vector3(-0.53725375f, 0.84173477f, -0.05330091f),
                        offset: 0.088011f,
                        // Mizzenmast 2 reef[0] has the same coil pose as Main/Mizzen Reef 3.
                        templateIndex: 0
                    )
                ),
            };

        // F9 captures #1-6 on WALK Shroud Small/structure_container/Hull.
        // Average |X|, Y/Z and reflected normals for each pair; discard #7-8.
        // Installed Shattered Seas Small donor inspected 2026-11-27:
        // mast 0 leftAngleWinch[0], Foremast_Sheet_Port, native scale 1,
        // donor +Z normal expressed in boat space; 0.095856 m rendered rear depth.
        private static SheetFallbackDefinition[] SheetFallbacks() =>
            new[]
            {
                new SheetFallbackDefinition(
                    name: "Forward",
                    fallback: new SheetFallbackPair(
                        templateMast: 0,
                        templateRole: WinchRole.Left,
                        templateIndex: 0,
                        sourceNormal: new Vector3(0.99637828f, 0f, 0.08503134f),
                        offset: 0.095856f,
                        // F9 #3/#4, mirrored about the boat centreline.
                        port: new SheetFallbackSeat(
                            contact: new Vector3(-2.1699795f, 2.1843340f, -1.4416415f),
                            normal: new Vector3(0.9912560f, -0.1079530f, 0.0758860f)
                        ),
                        starboard: new SheetFallbackSeat(
                            contact: new Vector3(2.1699795f, 2.1843340f, -1.4416415f),
                            normal: new Vector3(-0.9912560f, -0.1079530f, 0.0758860f)
                        )
                    )
                ),
                new SheetFallbackDefinition(
                    name: "Midships",
                    fallback: new SheetFallbackPair(
                        templateMast: 0,
                        templateRole: WinchRole.Left,
                        templateIndex: 0,
                        sourceNormal: new Vector3(0.99637828f, 0f, 0.08503134f),
                        offset: 0.095856f,
                        // F9 #1/#2, mirrored about the boat centreline.
                        port: new SheetFallbackSeat(
                            contact: new Vector3(-1.9755635f, 2.2257010f, -3.4302190f),
                            normal: new Vector3(0.9894000f, -0.0964430f, 0.1085660f)
                        ),
                        starboard: new SheetFallbackSeat(
                            contact: new Vector3(1.9755635f, 2.2257010f, -3.4302190f),
                            normal: new Vector3(-0.9894000f, -0.0964430f, 0.1085660f)
                        )
                    )
                ),
                new SheetFallbackDefinition(
                    name: "Aft",
                    fallback: new SheetFallbackPair(
                        templateMast: 0,
                        templateRole: WinchRole.Left,
                        templateIndex: 0,
                        sourceNormal: new Vector3(0.99637828f, 0f, 0.08503134f),
                        offset: 0.095856f,
                        // F9 #6/#5, mirrored about the boat centreline.
                        port: new SheetFallbackSeat(
                            contact: new Vector3(-1.2945680f, 2.2256470f, -7.6097145f),
                            normal: new Vector3(0.9200890f, 0.3464960f, 0.1826945f)
                        ),
                        starboard: new SheetFallbackSeat(
                            contact: new Vector3(1.2945680f, 2.2256470f, -7.6097145f),
                            normal: new Vector3(-0.9200890f, 0.3464960f, 0.1826945f)
                        )
                    )
                ),
            };
    }
}
