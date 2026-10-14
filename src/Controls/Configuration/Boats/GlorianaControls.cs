// Owns Gloriana's authored shared winch sources and fallback placements.
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class GlorianaControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Gloriana.Definition,
            sheetCategories: new[]
            {
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 1 },
                    sources: new[] { 1, 5 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 2 },
                    sources: new[] { 2, 6 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 3 },
                    sources: new[] { 3, 4 },
                    fallback: null
                ),
            },
            halyardGroups: new[]
            {
                new HalyardWinchGroup(mast: 1, sources: new[] { 1, 5 }),
                new HalyardWinchGroup(
                    mast: 2,
                    sources: new[] { 2 },
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #2, MAST_MAIN walking surface, 2026-10-04.
                        contact: new Vector3(-0.005373f, 1.887422f, 0.791344f),
                        normal: new Vector3(-0.098015f, 0.003737f, 0.995178f),
                        // halyard_mainmast2 local +Z in the installed boat frame.
                        sourceNormal: new Vector3(0f, 0.98480785f, -0.17364828f),
                        // Installed mesh rear bound -0.13091201, native scale 0.7.
                        offset: 0.09163841f,
                        templateIndex: 1
                    )
                ),
                new HalyardWinchGroup(
                    mast: 3,
                    sources: new[] { 3, 4, 6 },
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
                new HalyardWinchGroup(mast: 4, sources: new[] { 4, 6 }),
            }
        );
    }
}
