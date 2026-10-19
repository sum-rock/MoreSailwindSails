// Owns Brig's captured main/mizzen reef seats and hull/stern sheet fallback pairs.
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class BrigControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Brig.Definition,
            sheetFallbacks: new[]
            {
                new SheetFallbackDefinition(
                    name: "Mainmast",
                    fallback: new SheetFallbackPair(
                        // F9 #3/#4 hull captures: average |X|, Y/Z; mirror at boat X=0.
                        port: new SheetFallbackSeat(
                            contact: new Vector3(-3.203389f, 2.9924165f, -2.802583f),
                            normal: new Vector3(0.997649f, -0.068499f, 0.002198f)
                        ),
                        starboard: new SheetFallbackSeat(
                            contact: new Vector3(3.203389f, 2.9924165f, -2.802583f),
                            normal: new Vector3(-0.997649f, -0.068499f, 0.002198f)
                        )
                    )
                ),
                new SheetFallbackDefinition(
                    name: "Mizzenmast",
                    fallback: new SheetFallbackPair(
                        // F9 #5/#6 stern trim captures: average |X|, Y/Z; mirror at X=0.
                        port: new SheetFallbackSeat(
                            contact: new Vector3(-2.662798f, 5.4580005f, -12.127725f),
                            normal: new Vector3(0f, 0.987032f, 0.160525f)
                        ),
                        starboard: new SheetFallbackSeat(
                            contact: new Vector3(2.662798f, 5.4580005f, -12.127725f),
                            normal: new Vector3(0f, 0.987032f, 0.160525f)
                        )
                    )
                ),
            },
            halyardFallbacks: new[]
            {
                new HalyardFallbackDefinition(
                    mast: 5,
                    fallback: new HalyardFallbackSeat(
                        // Latest session F9 capture #2, mast_Back_0 walking surface.
                        contact: new Vector3(-0.233388f, 6.668716f, -5.699141f),
                        normal: new Vector3(-0.874236f, 0.010572f, -0.485387f),
                        // Installed reefWinch[0] local +Z in boat coordinates.
                        sourceNormal: Vector3.left,
                        // Coiled rope rear bound -0.13091201, native scale 1.
                        offset: 0.13091201f,
                        templateIndex: 0
                    )
                ),
                new HalyardFallbackDefinition(
                    mast: 4,
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #1, mast_Back_1 walking surface, 2026-10-16.
                        contact: new Vector3(-0.013316f, 4.350601f, -1.022201f),
                        normal: new Vector3(0f, 0.010889f, -0.999941f),
                        // Installed reefWinch[0] local +Z in boat coordinates.
                        sourceNormal: Vector3.left,
                        // Coiled rope rear bound -0.13091201, native scale 1.
                        offset: 0.13091201f,
                        templateIndex: 0
                    )
                ),
                new HalyardFallbackDefinition(
                    mast: 7,
                    fallback: new HalyardFallbackSeat(
                        // Latest session F9 capture #3, mast_mizzen_0 walking surface.
                        contact: new Vector3(0.011908f, 6.553253f, -10.679990f),
                        normal: new Vector3(0f, 0.010889f, -0.999941f),
                        sourceNormal: Vector3.left,
                        // Coiled rope rear bound -0.13091201, native scale 0.9.
                        offset: 0.11782080f,
                        templateIndex: 0
                    )
                ),
                new HalyardFallbackDefinition(
                    mast: 6,
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #2, mast_mizzen_1 walking surface, 2026-10-16.
                        contact: new Vector3(0.015656f, 5.064286f, -8.234023f),
                        normal: new Vector3(0f, 0.010889f, -0.999941f),
                        sourceNormal: Vector3.left,
                        // Same coiled rope rear bound, native mizzen scale 0.9.
                        offset: 0.11782080f,
                        templateIndex: 0
                    )
                ),
            }
        );
    }
}
