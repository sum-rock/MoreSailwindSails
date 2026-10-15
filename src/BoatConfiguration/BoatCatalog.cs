// Looks up shared boat identity and mast ancestry; normalizes native clone names.
using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatConfiguration.Boats;

namespace MoreSailwindSails.BoatConfiguration
{
    // Resolves shared boat identity and ancestry independently of feature support.
    internal static class BoatCatalog
    {
        internal static readonly IReadOnlyList<BoatDefinition> All = Array.AsReadOnly(
            new[]
            {
                Brig.Definition,
                Junk.Definition,
                Jong.Definition,
                Sanbuq.Definition,
                Cog.Definition,
                Shroud.Definition,
                LargeDhow.Definition,
                Gloriana.Definition,
                Chronian.Definition,
                Caelanor.Definition,
                Kakam.Definition,
                Dhow.Definition,
            }
        );

        internal static BoatDefinition Find(string boatName)
        {
            boatName = NormalizeName(boatName: boatName);
            return All.FirstOrDefault(d => d.BoatName == boatName);
        }

        internal static string NormalizeName(string boatName)
        {
            if (boatName == null)
                return null;
            while (boatName.EndsWith("(Clone)", StringComparison.Ordinal))
                boatName = boatName.Substring(0, boatName.Length - 7).TrimEnd();
            return boatName;
        }
    }
}
