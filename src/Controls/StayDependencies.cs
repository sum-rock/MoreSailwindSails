// Traverses installation dependencies without requiring donor options to be active.
using System;
using System.Collections.Generic;

namespace MoreSailwindSails.Controls
{
    internal static class StayDependencies
    {
        internal static bool ReachesBoth<T>(
            T option,
            Func<T, IEnumerable<T>> requires,
            Predicate<T> matchesFirst,
            Predicate<T> matchesSecond
        )
            where T : class
        {
            if (option == null)
                return false;
            var visited = new HashSet<T>();
            bool foundFirst = false,
                foundSecond = false;
            bool Visit(T current)
            {
                if (current == null || !visited.Add(current))
                    return false;
                foundFirst = foundFirst || matchesFirst(current);
                foundSecond = foundSecond || matchesSecond(current);
                if (foundFirst && foundSecond)
                    return true;
                foreach (var dependency in requires(current) ?? Array.Empty<T>())
                    if (Visit(dependency))
                        return true;
                return false;
            }
            // The source itself is a stay, not a dependency on a physical mast.
            visited.Add(option);
            foreach (var dependency in requires(option) ?? Array.Empty<T>())
                if (Visit(dependency))
                    return true;
            return false;
        }
    }
}
