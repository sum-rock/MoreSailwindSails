// Owns one boat's ordered Flying Sail support pairs.
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
            Boat = boat;
            Supports = Array.AsReadOnly(supportsCopy);
        }

        internal IEnumerable<IGrouping<int, MastSupportDefinition>> MastPairs(int foreIndex) =>
            Supports
                .Where(s => s.ForeSections.Contains(foreIndex))
                .GroupBy(s => s.AftSections.Last());
    }
}
