// Owns Shroud's authored fallback placements.
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class ShroudControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Shroud.Definition,
            sheetFallbacks: SheetFallbacks()
        );

        private static SheetFallbackDefinition[] SheetFallbacks() =>
            new SheetFallbackDefinition[]
            {
                new SheetFallbackDefinition(name: "Foremast", fallback: ForemastFallback),
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
