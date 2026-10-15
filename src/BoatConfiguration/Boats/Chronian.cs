// Owns Chronian's shared boat identity and physical mast ancestry.
using System.Collections.Generic;

namespace MoreSailwindSails.BoatConfiguration.Boats
{
    internal static class Chronian
    {
        internal static readonly BoatDefinition Definition = new BoatDefinition(
            boatName: "BOAT CHRONIAN (187)",
            mastParents: new Dictionary<int, int>
            {
                { 2, -1 },
                { 3, 2 },
                { 15, 3 },
                { 4, -1 },
                { 5, 4 },
                { 16, 5 },
                { 6, -1 },
                { 7, 6 },
                { 17, 7 },
            }
        );
    }
}
