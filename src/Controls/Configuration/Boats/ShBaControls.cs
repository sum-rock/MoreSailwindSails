// Owns Sh'ba's captured and mirrored sheet fallback placements.
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class ShBaControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.ShBa.Definition,
            sheetFallbacks: SheetFallbacks()
        );

        // F9 captures #1-6 on WALK Shroud Small/structure_container/Hull.
        // Average |X|, Y/Z and reflected normals for each pair; discard #7-8.
        // Installed Shattered Seas Small donor inspected 2026-11-27:
        // mast 0 reefWinch[0], Coil_Foremast_Reef_1, native scale 1,
        // donor +Z normal expressed in boat space; 0.088011 m rendered rear depth.
        private static SheetFallbackDefinition[] SheetFallbacks() =>
            new[]
            {
                new SheetFallbackDefinition(
                    name: "Forward",
                    fallback: new SheetFallbackPair(
                        templateMast: 0,
                        templateRole: WinchRole.Reef,
                        templateIndex: 0,
                        sourceNormal: new Vector3(-0.53725375f, 0.84173477f, -0.05330091f),
                        offset: 0.088011f,
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
                        templateRole: WinchRole.Reef,
                        templateIndex: 0,
                        sourceNormal: new Vector3(-0.53725375f, 0.84173477f, -0.05330091f),
                        offset: 0.088011f,
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
                        templateRole: WinchRole.Reef,
                        templateIndex: 0,
                        sourceNormal: new Vector3(-0.53725375f, 0.84173477f, -0.05330091f),
                        offset: 0.088011f,
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
