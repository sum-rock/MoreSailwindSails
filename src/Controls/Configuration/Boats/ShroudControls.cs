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
                    normal: Vector3.up,
                    sourceNormal: new Vector3(-0.006981f, 0.999976f, 0f),
                    offset: 0.076685f,
                    templateMast: 24,
                    templateRole: WinchRole.Left,
                    templateIndex: 0
                ),
                starboard: new SheetFallbackSeat(
                    contact: new Vector3(3.465970f, 4.829765f, -3.901223f),
                    normal: Vector3.up,
                    sourceNormal: new Vector3(-0.006981f, 0.999976f, 0f),
                    offset: 0.076685f,
                    templateMast: 24,
                    templateRole: WinchRole.Right,
                    templateIndex: 0
                )
            );
    }
}
