// Owns Gloriana's authored fallback placements.
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class GlorianaControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Gloriana.Definition,
            sheetFallbacks: new[]
            {
                new SheetFallbackDefinition(
                    name: "Mainmast",
                    fallback: new SheetFallbackPair(
                        // F9 #5/#6 trim captures, 2026-10-17: average |X|, Y/Z and mirrored normals.
                        port: new SheetFallbackSeat(
                            contact: new Vector3(-1.8593095f, 6.2631075f, -12.393245f),
                            normal: new Vector3(-0.032162f, 0.982311f, 0.1844725f)
                        ),
                        starboard: new SheetFallbackSeat(
                            contact: new Vector3(1.8593095f, 6.2631075f, -12.393245f),
                            normal: new Vector3(0.032162f, 0.982311f, 0.1844725f)
                        )
                    )
                ),
                new SheetFallbackDefinition(
                    name: "MainmastForward",
                    fallback: new SheetFallbackPair(
                        // F9 #7/#8 trim captures, 2026-10-17: average |X|, Y/Z and mirrored normals.
                        port: new SheetFallbackSeat(
                            contact: new Vector3(-3.1751935f, 3.5531495f, -0.929988f),
                            normal: new Vector3(0.2764265f, 0.957037f, 0.087572f)
                        ),
                        starboard: new SheetFallbackSeat(
                            contact: new Vector3(3.1751935f, 3.5531495f, -0.929988f),
                            normal: new Vector3(-0.2764265f, 0.957037f, 0.087572f)
                        )
                    )
                ),
            },
            halyardFallbacks: new HalyardFallbackDefinition[]
            {
                new HalyardFallbackDefinition(
                    mast: 2,
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #3, FIFE_MAIN walking surface, 2026-10-17.
                        contact: new Vector3(0.613389f, 1.827591f, 0.040833f),
                        normal: new Vector3(0.773011f, 0f, -0.634393f),
                        // halyard_mainmast2 local +Z in the installed boat frame.
                        sourceNormal: new Vector3(0f, 0.98480785f, -0.17364828f),
                        // Installed mesh rear bound -0.13091201, native scale 0.7.
                        offset: 0.09163841f,
                        templateIndex: 1
                    )
                ),
                new HalyardFallbackDefinition(
                    mast: 3,
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #1, FIFE_MIZEN walking surface, 2026-10-05.
                        contact: new Vector3(0.801387f, 4.769459f, -9.351812f),
                        normal: Vector3.right,
                        // halyard_mizenmast1 local +Z in the installed boat frame.
                        sourceNormal: new Vector3(-0.17364817f, 0.96984630f, 0.17101012f),
                        // Installed mesh rear bound -0.13091201, native scale 0.7.
                        offset: 0.09163841f,
                        templateIndex: 0
                    )
                ),
            }
        );
    }
}
