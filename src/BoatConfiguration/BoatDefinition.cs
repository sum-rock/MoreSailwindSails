// Owns immutable boat identity and physical mast ancestry, independent of sail families.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoreSailwindSails.BoatConfiguration
{
    internal sealed class BoatDefinition
    {
        internal readonly string BoatName;
        internal readonly IReadOnlyDictionary<int, int> MastParents;

        internal BoatDefinition(string boatName, IReadOnlyDictionary<int, int> mastParents)
        {
            if (
                mastParents.Any(p =>
                    p.Key < 0 || p.Value < -1 || (p.Value >= 0 && !mastParents.ContainsKey(p.Value))
                )
            )
                throw new ArgumentException("Invalid authored mast ancestry.");
            BoatName = boatName;
            MastParents = new ReadOnlyDictionary<int, int>(
                mastParents.ToDictionary(p => p.Key, p => p.Value)
            );
            foreach (int section in MastParents.Keys)
                Sections(section: section); // Reject cycles before any runtime lookup can hang.
        }

        internal int Base(int section) => Sections(section: section).Last();

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
                    throw new ArgumentException("No authored mast reference.");
            }
            return sections.ToArray();
        }
    }
}
