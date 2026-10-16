// Owns Caelanor's measured mainmast reef and symmetric aft sheet fallbacks.
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class CaelanorControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Caelanor.Definition,
            sheetFallbacks: new[]
            {
                new SheetFallbackDefinition(
                    name: "Mainmast",
                    // Latest F9 captures #1/#2: average |X|, Y/Z and mirrored normals at boat X=0.
                    fallback: new SheetFallbackPair(
                        port: new SheetFallbackSeat(
                            contact: new Vector3(-2.4498375f, 2.416566f, -4.761331f),
                            normal: new Vector3(0.0654635f, 0.995183f, 0.072977f)
                        ),
                        starboard: new SheetFallbackSeat(
                            contact: new Vector3(2.4498375f, 2.416566f, -4.761331f),
                            normal: new Vector3(-0.0654635f, 0.995183f, 0.072977f)
                        )
                    )
                ),
            },
            halyardFallbacks: new[]
            {
                new HalyardFallbackDefinition(
                    mast: 5,
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #5 on the aft mainmast walking surface.
                        contact: new Vector3(0.204692f, 1.884204f, -1.520411f),
                        normal: new Vector3(0.965926f, 0.018054f, 0.258188f),
                        // Installed halyard_main_main1 local +Z in boat coordinates.
                        sourceNormal: new Vector3(0f, 0.98480805f, 0.17364808f),
                        // Coiled rope rear bound -0.130912, native scale 0.60000134.
                        offset: 0.07854738f,
                        templateIndex: 0
                    )
                ),
            }
        );
    }
}
