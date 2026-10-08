using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreSailwindSails.BoatRigs
{
    // Immutable authored FishermansStayGroupDefinition data, copied at the definition boundary.
    internal sealed class FishermansStayGroupDefinition
    {
        internal readonly string Label;
        internal readonly IReadOnlyList<FishermansStayVariantDefinition> Variants;

        internal FishermansStayGroupDefinition(
            string label,
            IEnumerable<FishermansStayVariantDefinition> variants
        )
        {
            var variantsCopy = (
                variants ?? Array.Empty<FishermansStayVariantDefinition>()
            ).ToArray();
            if (string.IsNullOrEmpty(label) || variantsCopy.Length == 0)
                throw new ArgumentException("Empty Fisherman's Stay group.");
            // A shared mount registers once; its mast-pair configurations must use
            // the same menu label and donor assets.
            foreach (var mount in variantsCopy.GroupBy(v => v.MountIndex))
                if (
                    mount.Any(v => v.Label != mount.First().Label || v.Donor != mount.First().Donor)
                    || mount.Select(v => (v.Fore, v.Aft)).Distinct().Count() != mount.Count()
                )
                    throw new ArgumentException(
                        "Conflicting configurations for one Fisherman's Stay."
                    );
            Label = label;
            Variants = Array.AsReadOnly(variantsCopy);
        }
    }
}
