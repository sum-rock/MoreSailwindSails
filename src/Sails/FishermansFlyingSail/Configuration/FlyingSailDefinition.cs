// Owns one boat's ordered Flying Sail support pairs and donor selection.
using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatConfiguration;

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Configuration
{
    internal sealed class FlyingSailDefinition
    {
        internal readonly BoatDefinition Boat;
        internal readonly IReadOnlyList<MastSupportDefinition> Supports;
        internal string BoatName => Boat.BoatName;

        internal FlyingSailDefinition(
            BoatDefinition boat,
            IEnumerable<MastSupportDefinition> supports
        )
        {
            var supportsCopy = (supports ?? Array.Empty<MastSupportDefinition>()).ToArray();
            if (
                supportsCopy.Select(s => s.SheetControlSource).Distinct().Count()
                != supportsCopy.Length
            )
                throw new ArgumentException("Duplicate mast-pair control source.");
            Boat = boat;
            Supports = Array.AsReadOnly(supportsCopy);
        }

        internal IEnumerable<IGrouping<int, MastSupportDefinition>> MastPairs(int foreIndex) =>
            Supports
                .Where(s => s.ForeSections.Contains(foreIndex))
                .GroupBy(s => s.AftSections.Last());
    }
}
