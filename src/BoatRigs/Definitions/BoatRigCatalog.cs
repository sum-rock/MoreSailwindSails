using System;
using System.Linq;

namespace MoreSailwindSails.BoatRigs
{
    internal static class BoatRigCatalog
    {
        internal static BoatRigDefinition[] All =>
            new[]
            {
                Brig.Definition,
                Junk.Definition,
                Jong.Definition,
                Sanbuq.Definition,
                Cog.Definition,
                Leopard.Definition,
                Shroud.Definition,
                LargeDhow.Definition,
            };

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
