// Owns one boat's ordered stay groups and validates permanent mount identities.
using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatConfiguration;

namespace MoreSailwindSails.Stays.FishermansStay.Configuration
{
    internal sealed class FishermansStayDefinition
    {
        internal readonly BoatDefinition Boat;
        internal readonly IReadOnlyList<FishermansStayGroupDefinition> Stays;
        internal string BoatName => Boat.BoatName;

        internal FishermansStayDefinition(
            BoatDefinition boat,
            IEnumerable<FishermansStayGroupDefinition> stays
        )
        {
            var staysCopy = (stays ?? Array.Empty<FishermansStayGroupDefinition>()).ToArray();
            var mounts = staysCopy
                .SelectMany(g => g.Variants.Select(v => v.MountIndex).Distinct())
                .ToArray();
            if (mounts.Distinct().Count() != mounts.Length)
                throw new ArgumentException("Duplicate Fisherman's Stay mount ID.");
            Boat = boat;
            Stays = Array.AsReadOnly(staysCopy);
        }
    }
}
