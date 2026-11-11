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
                        // OldChronian 0.6.0: sheet_main_main_L; inspected 2026-10-20.
                        // Boat-local +Z normal and rendered rear depth at native scale.
                        templateMast: 5,
                        templateRole: WinchRole.Left,
                        templateIndex: 0,
                        sourceNormal: new Vector3(0.17364825f, 0.98480774f, 0f),
                        offset: 0.07854738f,
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
                new SheetFallbackDefinition(
                    name: "MainmastForward",
                    // F9 captures #3/#4 on CAE_trim, 2026-11-10: average |X|, Y/Z and mirrored normals at boat X=0.
                    fallback: new SheetFallbackPair(
                        // OldChronian 0.6.0: sheet_main_main_L; inspected 2026-10-20.
                        // Boat-local +Z normal and rendered rear depth at native scale.
                        templateMast: 5,
                        templateRole: WinchRole.Left,
                        templateIndex: 0,
                        sourceNormal: new Vector3(0.17364825f, 0.98480774f, 0f),
                        offset: 0.07854738f,
                        port: new SheetFallbackSeat(
                            contact: new Vector3(-3.009186f, 1.848863f, 0.809379f),
                            normal: new Vector3(0.076346f, 0.996563f, -0.032150f)
                        ),
                        starboard: new SheetFallbackSeat(
                            contact: new Vector3(3.009186f, 1.848863f, 0.809379f),
                            normal: new Vector3(-0.076346f, 0.996563f, -0.032150f)
                        )
                    )
                ),
                new SheetFallbackDefinition(
                    name: "Stern",
                    // F9 captures #1/#2 on CAE_trim, 2026-11-10: average |X|, Y/Z and mirrored normals at boat X=0.
                    fallback: new SheetFallbackPair(
                        // OldChronian 0.6.0: sheet_main_main_L; inspected 2026-10-20.
                        // Boat-local +Z normal and rendered rear depth at native scale.
                        templateMast: 5,
                        templateRole: WinchRole.Left,
                        templateIndex: 0,
                        sourceNormal: new Vector3(0.17364825f, 0.98480774f, 0f),
                        offset: 0.07854738f,
                        port: new SheetFallbackSeat(
                            contact: new Vector3(-1.957292f, 2.5856635f, -6.8068655f),
                            normal: new Vector3(0.0523535f, 0.991616f, 0.1181355f)
                        ),
                        starboard: new SheetFallbackSeat(
                            contact: new Vector3(1.957292f, 2.5856635f, -6.8068655f),
                            normal: new Vector3(-0.0523535f, 0.991616f, 0.1181355f)
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
