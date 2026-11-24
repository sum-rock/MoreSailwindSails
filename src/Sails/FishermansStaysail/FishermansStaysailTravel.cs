// Measures mast-relative trim and keeps native hinge sway within fitted limits.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    internal static class FishermansStaysailTravel
    {
        // Measure the physical sheet rotation independently of stay pitch,
        // mast rake and boat heel. Do not clamp the readout to hide overshoot.
        internal static float SignedAngle(Vector3 neutral, Vector3 current, Vector3 mastAxis)
        {
            var axis = mastAxis.normalized;
            neutral -= axis * Vector3.Dot(neutral, axis);
            current -= axis * Vector3.Dot(current, axis);
            return (float)(
                Math.Atan2(
                    Vector3.Dot(axis, Vector3.Cross(neutral, current)),
                    Vector3.Dot(neutral, current)
                )
                * 180
                / Math.PI
            );
        }

        internal static void ConstrainHinge(
            ref float min,
            ref float max,
            float allowedMin,
            float allowedMax
        )
        {
            min = Math.Max(allowedMin, Math.Min(allowedMax, min));
            max = Math.Max(allowedMin, Math.Min(allowedMax, max));
            // Tight opposing sheets plus negative native sway can cross the
            // endpoints. Meet halfway instead of assigning an inverted joint range.
            if (min > max)
                min = max = (min + max) * 0.5f;
        }
    }
}
