using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoreSailwindSails.BoatRigs
{
    // Immutable authored BoatRigDefinition data, copied at the definition boundary.
    internal sealed class BoatRigDefinition
    {
        internal readonly string BoatName;
        internal readonly IReadOnlyList<MastSupportDefinition> Supports;
        internal readonly IReadOnlyList<FishermansStayGroupDefinition> Stays;
        internal readonly IReadOnlyDictionary<int, int> MastParents;
        internal readonly IReadOnlyList<SheetWinchCategory> SheetCategories;
        internal readonly IReadOnlyList<HalyardWinchGroup> HalyardGroups;

        internal BoatRigDefinition(
            string boatName,
            IEnumerable<MastSupportDefinition> supports,
            IEnumerable<FishermansStayGroupDefinition> stays,
            IReadOnlyDictionary<int, int> mastParents,
            IEnumerable<SheetWinchCategory> sheetCategories = null,
            IEnumerable<HalyardWinchGroup> halyardGroups = null
        )
        {
            var supportsCopy = (supports ?? Array.Empty<MastSupportDefinition>()).ToArray();
            var staysCopy = (stays ?? Array.Empty<FishermansStayGroupDefinition>()).ToArray();
            var sheetCategoriesCopy = (
                sheetCategories ?? Array.Empty<SheetWinchCategory>()
            ).ToArray();
            var halyardGroupsCopy = (halyardGroups ?? Array.Empty<HalyardWinchGroup>()).ToArray();
            // Single-mast sail families need ancestry and controls without a mast pair.
            if (
                supportsCopy.Select(s => s.SheetControlSource).Distinct().Count()
                != supportsCopy.Length
            )
                throw new ArgumentException("Duplicate mast-pair control source.");
            var mounts = staysCopy
                .SelectMany(g => g.Variants.Select(v => v.MountIndex).Distinct())
                .ToArray();
            if (mounts.Distinct().Count() != mounts.Length)
                throw new ArgumentException("Duplicate Fisherman's Stay mount ID.");
            if (
                mastParents.Any(p =>
                    p.Key < 0 || p.Value < -1 || (p.Value >= 0 && !mastParents.ContainsKey(p.Value))
                )
            )
                throw new ArgumentException("Invalid authored mast ancestry.");
            BoatName = boatName;
            Supports = Array.AsReadOnly(supportsCopy);
            Stays = Array.AsReadOnly(staysCopy);
            MastParents = new ReadOnlyDictionary<int, int>(
                mastParents.ToDictionary(p => p.Key, p => p.Value)
            );
            SheetCategories = Array.AsReadOnly(sheetCategoriesCopy);
            HalyardGroups = Array.AsReadOnly(halyardGroupsCopy);
            if (
                HalyardGroups.Any(g => g == null || !MastParents.ContainsKey(g.Mast))
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
            foreach (int section in MastParents.Keys)
                Sections(section); // Reject cycles before any runtime lookup can hang.
        }

        internal IEnumerable<IGrouping<int, MastSupportDefinition>> MastPairs(int foreIndex) =>
            Supports
                .Where(s => s.ForeSections.Contains(foreIndex))
                .GroupBy(s => s.AftSections.Last());

        internal int Base(int section) => Sections(section).Last();

        // Groups define the complete ordered list. Unconfigured masts retain
        // their own reef array; never inherit a group from another mast variant.
        internal IEnumerable<int> HalyardSources(int mast) =>
            HalyardGroups.FirstOrDefault(g => g.Mast == mast)?.Sources ?? new[] { mast };

        internal int[] Sections(int section)
        {
            var sections = new List<int>();
            var visited = new HashSet<int>();
            while (section >= 0)
            {
                if (!visited.Add(section))
                    throw new ArgumentException("Cyclic authored mast ancestry.");
                sections.Add(section);
                if (!MastParents.TryGetValue(section, out section))
                    throw new ArgumentException("No authored staysail mast reference.");
            }
            return sections.ToArray();
        }

        internal SheetWinchCategory SheetCategory(int mast)
        {
            int physical = MastParents.ContainsKey(mast) ? Base(mast) : mast;
            return SheetCategories.FirstOrDefault(c => c.PhysicalMasts.Contains(physical));
        }
    }
}
