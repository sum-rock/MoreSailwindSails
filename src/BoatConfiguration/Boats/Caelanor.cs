// Owns Caelanor's shared boat identity and physical mast ancestry.
using System.Collections.Generic;

namespace MoreSailwindSails.BoatConfiguration.Boats
{
    internal static class Caelanor
    {
        internal static readonly BoatDefinition Definition = new BoatDefinition(
            boatName: "BOAT CAELANOR (192)",
            mastParents: new Dictionary<int, int>
            {
                { 1, -1 },
                { 2, 1 },
                { 17, 2 },
                { 3, -1 },
                { 4, 3 },
                { 19, 4 },
                { 5, -1 },
                { 6, 5 },
                { 18, 6 },
            }
        );
    }
}
