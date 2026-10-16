// Owns Kakam's authored fallback placements.
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class KakamControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Kakam.Definition,
            sheetFallbacks: new SheetFallbackDefinition[]
            {
                new SheetFallbackDefinition(name: "Mainmast", fallback: MainmastFallback),
            },
            halyardFallbacks: new HalyardFallbackDefinition[]
            {
                new HalyardFallbackDefinition(
                    mast: 7,
                    fallback: new HalyardFallbackSeat(
                        contact: new Vector3(-0.000950f, 2.005890f, -4.232710f),
                        normal: new Vector3(-0.000001f, 0.036369f, -0.999338f),
                        sourceNormal: new Vector3(-1f, 0f, 0f),
                        // Installed reef template rear Z bound 0.063862 at scale 0.792.
                        offset: 0.05057871f,
                        templateIndex: 0
                    )
                ),
            }
        );

        // F9 captures #2/#3 centered across the boat, with a shared normal for parallel winches.

        private static SheetFallbackPair MainmastFallback =>
            new SheetFallbackPair(
                port: new SheetFallbackSeat(
                    contact: new Vector3(-0.8067125f, 2.7384885f, -6.0352715f),
                    normal: new Vector3(0f, 0.984682f, 0.150002f)
                ),
                starboard: new SheetFallbackSeat(
                    contact: new Vector3(0.8067125f, 2.7384885f, -6.0352715f),
                    normal: new Vector3(0f, 0.984682f, 0.150002f)
                )
            );
    }
}
