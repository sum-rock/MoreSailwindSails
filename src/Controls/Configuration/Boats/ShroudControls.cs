// Owns Shroud's authored shared winch sources and fallback placements.
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class ShroudControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Shroud.Definition,
            sheetCategories: SheetCategories(),
            halyardGroups: HalyardGroups()
        );

        // Installed rig identities and support audit: Controls/HalyardMounts.txt.
        private static HalyardWinchGroup[] HalyardGroups() =>
            new[]
            {
                // Both foremast variants; Foremast Reef Coils/Jib Reefs only.
                new HalyardWinchGroup(mast: 5, sources: new[] { 5, 13, 14, 15, 20, 21, 22, 23 }),
                new HalyardWinchGroup(mast: 6, sources: new[] { 6, 13, 14, 15, 20, 21, 22, 23 }),
                // Both mainmast variants; Mainmast Reef Coils/Jib Reefs only.
                new HalyardWinchGroup(mast: 7, sources: new[] { 7, 16, 17, 24 }),
                new HalyardWinchGroup(mast: 8, sources: new[] { 8, 16, 17, 24 }),
                // Both mizzen variants; Mizzenmast Reef Coils/Jib Reef only.
                new HalyardWinchGroup(mast: 9, sources: new[] { 9, 18, 19, 25 }),
                new HalyardWinchGroup(mast: 10, sources: new[] { 10, 18, 19, 25 }),
            };

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 6, 5 },
                    sources: new[] { 6, 5, 16, 17, 24, 13, 14, 15, 20, 21, 22, 23 },
                    fallback: ForemastFallback
                ),
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 8, 7 },
                    sources: new[] { 8, 7, 18, 19, 25 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 10, 9 },
                    sources: new[] { 10, 9 },
                    fallback: null
                ),
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
