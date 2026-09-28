using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoreSailwindSails.BoatRigs
{
    internal sealed class BoatRigDefinition
    {
        internal readonly string BoatName;
        internal readonly MastSupportDefinition[] Supports;
        internal readonly FishermansStayGroupDefinition[] Stays;
        internal readonly IReadOnlyDictionary<int, int> MastParents;
        internal readonly IReadOnlyDictionary<string, float> WinchClearances;
        internal readonly SheetWinchCategory[] SheetCategories;

        internal BoatRigDefinition(
            string boatName,
            MastSupportDefinition[] supports,
            FishermansStayGroupDefinition[] stays,
            IReadOnlyDictionary<int, int> mastParents,
            SheetWinchCategory[] sheetCategories = null,
            IReadOnlyDictionary<string, float> winchClearances = null
        )
        {
            if (
                supports.Length == 0
                || supports.Select(s => s.SheetControlSource).Distinct().Count() != supports.Length
            )
                throw new ArgumentException("Empty mast supports or duplicate control source.");
            var mounts = stays.SelectMany(g => g.Variants).Select(v => v.MountIndex).ToArray();
            if (mounts.Distinct().Count() != mounts.Length)
                throw new ArgumentException("Duplicate Fisherman's Stay mount ID.");
            if (
                mastParents.Any(p =>
                    p.Key < 0 || p.Value < -1 || (p.Value >= 0 && !mastParents.ContainsKey(p.Value))
                )
            )
                throw new ArgumentException("Invalid authored mast ancestry.");
            BoatName = boatName;
            Supports = supports;
            Stays = stays;
            MastParents = new ReadOnlyDictionary<int, int>(
                mastParents.ToDictionary(p => p.Key, p => p.Value)
            );
            WinchClearances = winchClearances ?? new Dictionary<string, float>();
            SheetCategories = sheetCategories ?? Array.Empty<SheetWinchCategory>();
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
