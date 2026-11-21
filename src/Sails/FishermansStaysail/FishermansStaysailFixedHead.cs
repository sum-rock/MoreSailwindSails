using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    // Per-instance tack state in the boat's neutral frame. No sheet position,
    // rotating cloth transform or load magnitude is used to set the angle.
    internal struct FishermansStaysailFixedHead
    {
        private int targetSide;
        internal float Side { get; private set; }

        internal void Update(Vector3 wind, Vector3 mastAxis, Vector3 aftDirection, float seconds)
        {
            var positiveAngleDirection = Vector3.Cross(mastAxis, aftDirection).normalized;
            float across = Vector3.Dot(wind, positiveAngleDirection);
            int next = CamberSide(targetSide == 0 ? 1 : targetSide, across);
            if (targetSide == 0)
                Side = next;
            else
            {
                Side = SmoothLoad(Side, next, seconds);
                if (Math.Abs(Side - next) < 0.0005f)
                    Side = next;
            }
            targetSide = next;
        }

        internal static float SmoothLoad(float previous, float target, float seconds)
        {
            if (float.IsNaN(target) || float.IsInfinity(target))
                target = 0;
            target = Math.Max(-1, Math.Min(1, target));
            float blend = 1 - (float)Math.Exp(-3 * Math.Max(0, Math.Min(0.1f, seconds)));
            return previous + (target - previous) * blend;
        }

        internal static int CamberSide(int previous, float normalFlow) =>
            normalFlow > 0.6f ? 1
            : normalFlow < -0.6f ? -1
            : previous;
    }
}
