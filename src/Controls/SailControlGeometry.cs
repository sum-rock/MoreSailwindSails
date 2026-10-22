// Validates the neutral fitted extent supplied by control consumers.
using System;
using UnityEngine;

namespace MoreSailwindSails.Controls
{
    internal static class SailControlGeometry
    {
        // Measure the fitted width vector in boat space, independent of live sheet/reef motion.
        internal static float? SheetSpan(Vector3 neutralWidthInBoat)
        {
            if (
                float.IsNaN(neutralWidthInBoat.x)
                || float.IsInfinity(neutralWidthInBoat.x)
                || float.IsNaN(neutralWidthInBoat.y)
                || float.IsInfinity(neutralWidthInBoat.y)
                || float.IsNaN(neutralWidthInBoat.z)
                || float.IsInfinity(neutralWidthInBoat.z)
                || neutralWidthInBoat.sqrMagnitude <= 0.000001f
            )
                return null;
            return Math.Abs(neutralWidthInBoat.z);
        }
    }
}
