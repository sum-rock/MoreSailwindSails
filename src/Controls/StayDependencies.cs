// Traverses installation dependencies without requiring donor options to be active.
using System;
using System.Collections.Generic;

namespace MoreSailwindSails.Controls
{
    internal static class StayDependencies
    {
        internal static bool Reaches<T>(
            T option,
            Func<T, IEnumerable<T>> requires,
            Predicate<T> matches
        )
            where T : class
        {
            var visited = new HashSet<T>();
            bool Visit(T current)
            {
                if (current == null || !visited.Add(current))
                    return false;
                if (matches(current))
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
