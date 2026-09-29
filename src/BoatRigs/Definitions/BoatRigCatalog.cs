using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreSailwindSails.BoatRigs
{
    // Resolves only supported boat profiles for sail fitting and stay registration.
    internal static class BoatRigCatalog
    {
        internal static readonly IReadOnlyList<BoatRigDefinition> All = Array.AsReadOnly(
            new[]
            {
                Brig.Definition,
                Junk.Definition,
                Jong.Definition,
                Sanbuq.Definition,
                Cog.Definition,
                Shroud.Definition,
                LargeDhow.Definition,
            }
        );

        internal static BoatRigDefinition Find(string boatName)
        {
            if (boatName == null)
                return null;
            while (boatName.EndsWith("(Clone)", StringComparison.Ordinal))
                boatName = boatName.Substring(0, boatName.Length - 7).TrimEnd();
            return All.FirstOrDefault(d => d.BoatName == boatName);
        }
    }
}
