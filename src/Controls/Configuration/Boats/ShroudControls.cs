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
