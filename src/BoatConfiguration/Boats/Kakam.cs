// Owns Kakam's shared boat identity and physical mast ancestry.
using System.Collections.Generic;

namespace MoreSailwindSails.BoatConfiguration.Boats
{
    internal static class Kakam
    {
        internal static readonly BoatDefinition Definition = new BoatDefinition(
            boatName: "BOAT junk small singleroof(90)",
            mastParents: new Dictionary<int, int>
            {
                { 5, -1 },
                { 6, -1 },
                { 7, -1 },
            }
        );
    }
}
