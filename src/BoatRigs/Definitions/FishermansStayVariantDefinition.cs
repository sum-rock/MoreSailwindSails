using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    // Immutable authored FishermansStayVariantDefinition data, copied at the definition boundary.
    internal sealed class FishermansStayVariantDefinition
    {
        internal readonly int MountIndex,
            Donor,
            Fore,
            Aft,
            GuideIndex;
        internal readonly string Label;
        internal readonly Vector3 ForePoint,
            AftPoint;
        internal readonly bool ExtendedGuide,
            AlignGuideHeightToAftAnchor;
        internal readonly IReadOnlyList<int> Required,
            Forbidden;

        internal FishermansStayVariantDefinition(
            int mountIndex,
            int donor,
            string label,
            int fore,
            Vector3 forePoint,
            int aft,
            Vector3 aftPoint,
            bool extendedGuide,
            int guideIndex,
            IEnumerable<int> required,
            IEnumerable<int> forbidden,
            bool alignGuideHeightToAftAnchor = false
        )
        {
            var requiredCopy = (required ?? Array.Empty<int>()).ToArray();
            var forbiddenCopy = (forbidden ?? Array.Empty<int>()).ToArray();
            if (
                mountIndex < 128
                || mountIndex >= 256
                || donor < 0
                || donor >= 128
                || fore == aft
                || guideIndex < 0
                || string.IsNullOrEmpty(label)
                || !Finite(forePoint)
                || !Finite(aftPoint)
                || !requiredCopy.Contains(fore)
                || !requiredCopy.Contains(aft)
                || requiredCopy.Concat(forbiddenCopy).Any(i => i < 0 || i >= 128)
                || requiredCopy.Distinct().Count() != requiredCopy.Length
                || forbiddenCopy.Distinct().Count() != forbiddenCopy.Length
                || requiredCopy.Intersect(forbiddenCopy).Any()
            )
                throw new ArgumentException("Invalid Fisherman's Stay reference or mount ID.");
            MountIndex = mountIndex;
            Donor = donor;
            Label = label;
            Fore = fore;
            Aft = aft;
            ForePoint = forePoint;
            AftPoint = aftPoint;
            ExtendedGuide = extendedGuide;
            AlignGuideHeightToAftAnchor = alignGuideHeightToAftAnchor;
            GuideIndex = guideIndex;
            Required = Array.AsReadOnly(requiredCopy);
            Forbidden = Array.AsReadOnly(forbiddenCopy);
        }

        private static bool Finite(Vector3 point) =>
            !float.IsNaN(point.x)
            && !float.IsInfinity(point.x)
            && !float.IsNaN(point.y)
            && !float.IsInfinity(point.y)
            && !float.IsNaN(point.z)
            && !float.IsInfinity(point.z);
    }
}
