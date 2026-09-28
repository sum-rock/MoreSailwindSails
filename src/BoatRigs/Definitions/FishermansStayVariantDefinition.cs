using System;
using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
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
        internal readonly bool ExtendedGuide;
        internal readonly int[] Required,
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
            int[] required,
            int[] forbidden
        )
        {
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
                || !required.Contains(fore)
                || !required.Contains(aft)
                || required.Concat(forbidden).Any(i => i < 0 || i >= 128)
                || required.Distinct().Count() != required.Length
                || forbidden.Distinct().Count() != forbidden.Length
                || required.Intersect(forbidden).Any()
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
            GuideIndex = guideIndex;
            Required = required;
            Forbidden = forbidden;
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
