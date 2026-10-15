// Owns shared sheet and halyard placement data for one boat.
using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatConfiguration;

namespace MoreSailwindSails.Controls.Configuration
{
    internal sealed class BoatControlDefinition
    {
        internal readonly BoatDefinition Boat;
        internal readonly IReadOnlyList<SheetWinchCategory> SheetCategories;
        internal readonly IReadOnlyList<HalyardWinchGroup> HalyardGroups;
        internal string BoatName => Boat.BoatName;

        internal BoatControlDefinition(
            BoatDefinition boat,
            IEnumerable<SheetWinchCategory> sheetCategories = null,
            IEnumerable<HalyardWinchGroup> halyardGroups = null
        )
        {
            Boat = boat;
            var sheetCategoriesCopy = (
                sheetCategories ?? Array.Empty<SheetWinchCategory>()
            ).ToArray();
            var halyardGroupsCopy = (halyardGroups ?? Array.Empty<HalyardWinchGroup>()).ToArray();
            SheetCategories = Array.AsReadOnly(sheetCategoriesCopy);
            HalyardGroups = Array.AsReadOnly(halyardGroupsCopy);
            if (
                HalyardGroups.Any(g => g == null || !Boat.MastParents.ContainsKey(g.Mast))
                || HalyardGroups.Select(g => g.Mast).Distinct().Count() != HalyardGroups.Count
            )
                throw new ArgumentException("Unknown or duplicate halyard group mast.");
            if (
                SheetCategories
                    .SelectMany(c => c.PhysicalMasts)
                    .GroupBy(id => id)
                    .Any(g => g.Count() != 1)
                || SheetCategories.GroupBy(c => c.Name).Any(g => g.Count() != 1)
            )
                throw new ArgumentException("Duplicate physical winch category.");
        }

        // Groups define the complete ordered list. Unconfigured masts retain
        // their own reef array; never inherit a group from another mast variant.
        internal IEnumerable<int> HalyardSources(int mast) =>
            HalyardGroups.FirstOrDefault(g => g.Mast == mast)?.Sources ?? new[] { mast };

        internal SheetWinchCategory SheetCategory(int mast)
        {
            int physical = Boat.MastParents.ContainsKey(mast) ? Boat.Base(section: mast) : mast;
            return SheetCategories.FirstOrDefault(c => c.PhysicalMasts.Contains(physical));
        }
    }
}
