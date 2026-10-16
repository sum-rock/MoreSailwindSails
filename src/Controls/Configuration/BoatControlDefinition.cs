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
        internal readonly IReadOnlyList<SheetFallbackDefinition> SheetFallbacks;
        internal readonly IReadOnlyList<HalyardFallbackDefinition> HalyardFallbacks;
        internal string BoatName => Boat.BoatName;

        internal BoatControlDefinition(
            BoatDefinition boat,
            IEnumerable<SheetFallbackDefinition> sheetFallbacks = null,
            IEnumerable<HalyardFallbackDefinition> halyardFallbacks = null
        )
        {
            Boat = boat;
            var sheetFallbacksCopy = (
                sheetFallbacks ?? Array.Empty<SheetFallbackDefinition>()
            ).ToArray();
            var halyardFallbacksCopy = (
                halyardFallbacks ?? Array.Empty<HalyardFallbackDefinition>()
            ).ToArray();
            SheetFallbacks = Array.AsReadOnly(sheetFallbacksCopy);
            HalyardFallbacks = Array.AsReadOnly(halyardFallbacksCopy);
            if (
                HalyardFallbacks.Any(g => g == null || !Boat.MastParents.ContainsKey(g.Mast))
                || HalyardFallbacks.Select(g => g.Mast).Distinct().Count() != HalyardFallbacks.Count
            )
                throw new ArgumentException("Unknown or duplicate halyard fallback mast.");
            if (SheetFallbacks.GroupBy(c => c.Name).Any(g => g.Count() != 1))
                throw new ArgumentException("Duplicate sheet fallback name.");
        }
    }
}
