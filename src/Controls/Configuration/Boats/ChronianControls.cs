// Owns Chronian's mirrored sheet pairs and captured main/mizzen halyard fallbacks.
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    // Supplies mirrored hull-mounted sheet pairs and per-mast halyard seats.
    internal static class ChronianControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Chronian.Definition,
            sheetFallbacks: new[]
            {
                new SheetFallbackDefinition(
                    name: "MainmastForward",
                    fallback: new SheetFallbackPair(
                        // Same inspected native donor and mounting frame as the mainmast pair.
                        templateMast: 4,
                        templateRole: WinchRole.Left,
                        templateIndex: 0,
                        sourceNormal: new Vector3(0.2588185f, 0.96592597f, 0.00000018f),
                        offset: 0.07854724f,
                        // F9 #1/#2 on structure_container/1_TRIM, 2026-10-20;
                        // average |X|, Y/Z and mirror across boat X=0.
                        port: new SheetFallbackSeat(
                            contact: new Vector3(-4.3953975f, 4.2576155f, -0.563150f),
                            normal: new Vector3(0.000182f, 0.999904f, 0.013889f)
                        ),
                        starboard: new SheetFallbackSeat(
                            contact: new Vector3(4.3953975f, 4.2576155f, -0.563150f),
                            normal: new Vector3(-0.000182f, 0.999904f, 0.013889f)
                        )
                    )
                ),
                new SheetFallbackDefinition(
                    name: "Mainmast",
                    fallback: new SheetFallbackPair(
                        // OldChronian 0.6.0: rope_mainmain_reef_port; inspected 2026-10-20.
                        // Boat-local +Z normal and rendered rear depth at native scale.
                        templateMast: 4,
                        templateRole: WinchRole.Left,
                        templateIndex: 0,
                        sourceNormal: new Vector3(0.2588185f, 0.96592597f, 0.00000018f),
                        offset: 0.07854724f,
                        // F9 #5/#6 trim captures: average |X|, Y/Z; mirror at boat X=0.
                        port: new SheetFallbackSeat(
                            contact: new Vector3(-4.1310105f, 5.744949f, -8.2060095f),
                            normal: new Vector3(-0.000128f, 0.996816f, 0.079740f)
                        ),
                        starboard: new SheetFallbackSeat(
                            contact: new Vector3(4.1310105f, 5.744949f, -8.2060095f),
                            normal: new Vector3(0.000128f, 0.996816f, 0.079740f)
                        )
                    )
                ),
                new SheetFallbackDefinition(
                    name: "Mizzenmast",
                    fallback: new SheetFallbackPair(
                        // OldChronian 0.6.0: rope_mizenmain_reef_port; inspected 2026-10-20.
                        // Boat-local +Z normal and rendered rear depth at native scale.
                        templateMast: 6,
                        templateRole: WinchRole.Left,
                        templateIndex: 0,
                        sourceNormal: new Vector3(0.2588185f, 0.96592597f, 0.00000018f),
                        offset: 0.07854724f,
                        // F9 #8/#9 trim captures: average |X|, Y/Z; mirror at boat X=0.
                        port: new SheetFallbackSeat(
                            contact: new Vector3(-3.474694f, 6.5194475f, -16.214630f),
                            normal: new Vector3(-0.000128f, 0.994475f, 0.104975f)
                        ),
                        starboard: new SheetFallbackSeat(
                            contact: new Vector3(3.474694f, 6.5194475f, -16.214630f),
                            normal: new Vector3(0.000128f, 0.994475f, 0.104975f)
                        )
                    )
                ),
            },
            halyardFallbacks: new[]
            {
                new HalyardFallbackDefinition(
                    mast: 4,
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #7, MAINMAST/FIFE MAIN walking surface.
                        contact: new Vector3(0.450090f, 3.190525f, -3.516479f),
                        normal: Vector3.back,
                        // Installed rope_MainMast_mast1_1 local +Z in boat coordinates.
                        sourceNormal: Vector3.forward,
                        // Coiled rope rear bound -0.13091201, native scale 0.60000074.
                        offset: 0.07854730f,
                        templateIndex: 0
                    )
                ),
                new HalyardFallbackDefinition(
                    mast: 6,
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #10, MIZENMAST/FIFE MIZEN walking surface.
                        contact: new Vector3(0.484589f, 5.905076f, -13.384650f),
                        normal: Vector3.back,
                        // Installed rope_MizenMast_mast1_1 local +Z in boat coordinates.
                        sourceNormal: Vector3.forward,
                        // Coiled rope rear bound -0.13091201, native scale 0.60000110.
                        offset: 0.07854735f,
                        templateIndex: 0
                    )
                ),
            }
        );
    }
}
