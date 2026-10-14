// Owns Gloriana's shared boat identity and physical mast ancestry.
using System.Collections.Generic;

namespace MoreSailwindSails.BoatConfiguration.Boats
{
    internal static class Gloriana
    {
        internal static readonly BoatDefinition Definition = new BoatDefinition(
            boatName: "BOAT GLORIANA (182)",
            mastParents: new Dictionary<int, int>
            {
                { 1, -1 },
                { 2, -1 },
                { 3, -1 },
                { 4, 3 },
            }
        );
    }
}
