// Owns Dhow's shared boat identity and physical mast ancestry.
using System.Collections.Generic;

namespace MoreSailwindSails.BoatConfiguration.Boats
{
    internal static class Dhow
    {
        internal static readonly BoatDefinition Definition = new BoatDefinition(
            boatName: "BOAT dhow small (10)",
            mastParents: new Dictionary<int, int>
            {
                { 6, -1 },
                { 7, -1 },
                { 51, -1 },
            }
        );
    }
}
