// Owns LargeDhow's shared boat identity and physical mast ancestry.
using System.Collections.Generic;

namespace MoreSailwindSails.BoatConfiguration.Boats
{
    internal static class LargeDhow
    {
        internal static readonly BoatDefinition Definition = new BoatDefinition(
            boatName: "BOAT dhow large (30)",
            mastParents: MastParents()
        );

        // Native topmast options require their matching mainmast despite being siblings.
        private static Dictionary<int, int> MastParents() =>
            new Dictionary<int, int>
            {
                { 0, -1 },
                { 1, -1 },
                { 2, -1 },
                { 3, 2 },
                { 4, -1 },
                { 5, 4 },
                { 6, -1 },
                { 7, -1 },
                { 8, -1 },
            };
    }
}
