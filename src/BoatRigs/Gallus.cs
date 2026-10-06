using System;
using System.Collections.Generic;

namespace MoreSailwindSails.BoatRigs
{
    // Supplies single-mast Spritsail ancestry and controls without Fisherman's sail mounts.
    internal static class Gallus
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            boatName: "BOAT GALLUS (197)",
            supports: Array.Empty<MastSupportDefinition>(),
            stays: Array.Empty<FishermansStayGroupDefinition>(),
            mastParents: new Dictionary<int, int> { { 1, -1 }, { 4, -1 } },
            sheetCategories: new[]
            {
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 1, 4 },
                    sources: new[] { 1, 4, 2, 3, 5, 6 },
                    fallback: null
                ),
            },
            halyardGroups: new[]
            {
                new HalyardWinchGroup(mast: 1, sources: new[] { 1, 2, 3 }),
                new HalyardWinchGroup(mast: 4, sources: new[] { 4, 5, 6 }),
            }
        );
    }
}
