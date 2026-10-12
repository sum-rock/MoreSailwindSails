using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    // Per-instance tack state in the boat's neutral frame. No sheet position,
    // rotating cloth transform or load magnitude is used to set the angle.
    internal struct FishermansStaysailFixedHead
    {
        internal const float DeployedAngle = 14f;
        private int targetSide;
        internal float Side { get; private set; }

        internal void Update(Vector3 wind, Vector3 mastAxis, Vector3 aftDirection, float seconds)
        {
            var positiveAngleDirection = Vector3.Cross(mastAxis, aftDirection).normalized;
            float across = Vector3.Dot(wind, positiveAngleDirection);
            int next = FishermansStaysailBillow.CamberSide(
                targetSide == 0 ? 1 : targetSide,
                across
            );
            if (targetSide == 0)
                Side = next;
            else
            {
                Side = FishermansStaysailBillow.SmoothLoad(Side, next, seconds);
                if (Math.Abs(Side - next) < 0.0005f)
                    Side = next;
            }
            targetSide = next;
        }

        internal static float ReefFraction(float unroll) =>
            float.IsNaN(unroll) ? 0 : Math.Max(0, Math.Min(1, unroll));

        internal static float LowerAngle(float sheetAngle, float deployedHeadAngle, float unroll)
        {
            float headAngle = deployedHeadAngle * ReefFraction(unroll);
            return headAngle
                + (sheetAngle - headAngle) * FishermansStaysailBillow.Deployment(unroll);
        }

        internal static Vector3 Position(
            Vector3 neutralHead,
            Vector3 foreHead,
            Vector3 mastAxis,
            float side,
            float angle,
            float unroll
        ) =>
            FishermansStaysailFrameGeometry.RotateAroundMast(
                neutralHead,
                foreHead,
                mastAxis,
                side * angle * ReefFraction(unroll)
            );
    }
}
