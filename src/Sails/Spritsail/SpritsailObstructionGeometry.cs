using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Defines the tunable tack penalty and localized camber attenuation without sampling Cloth.
    internal static class SpritsailObstructionGeometry
    {
        internal const float DefaultForceMultiplier = 0.9f;

        internal static float Target(Vector3 boatWind, bool starboardAffected, float previous)
        {
            if (!Finite(point: boatWind))
                return 0;
            float horizontal = (float)
                Math.Sqrt((double)boatWind.x * boatWind.x + (double)boatWind.z * boatWind.z);
            if (horizontal < 0.01f)
                return 0;
            // Native apparent wind is airflow velocity, so negative boat X arrives from starboard.
            float side = -boatWind.x / horizontal * (starboardAffected ? 1 : -1);
            if (Math.Abs(side) < 0.035f)
                return Clamp(value: previous);
            return side > 0 ? 1 : 0;
        }

        internal static float Step(float previous, float target, float seconds)
        {
            previous = Clamp(value: previous);
            target = Clamp(value: target);
            float distance = SpritsailDeployment.Finite(value: seconds)
                ? Math.Max(0, Math.Min(0.1f, seconds)) * 2
                : 0;
            return previous + Math.Max(-distance, Math.Min(distance, target - previous));
        }

        internal static float ValidMultiplier(float value) =>
            SpritsailDeployment.Finite(value: value) && value >= 0 && value <= 1
                ? value
                : DefaultForceMultiplier;

        internal static float Force(float obstruction, float multiplier) =>
            1 - Clamp(value: obstruction) * (1 - ValidMultiplier(value: multiplier));

        internal static float Camber(
            Vector3 point,
            Vector3 heel,
            Vector3 tip,
            float width,
            float obstruction
        )
        {
            if (
                !Finite(point: point)
                || !Finite(point: heel)
                || !Finite(point: tip)
                || !SpritsailDeployment.Finite(value: width)
                || width <= 1e-6f
            )
                return 1;
            point.y = heel.y = tip.y = 0;
            var segment = tip - heel;
            float length = segment.sqrMagnitude;
            if (length < 1e-10f)
                return 1;
            float along = Clamp(value: Vector3.Dot(point - heel, segment) / length);
            float distance = (point - (heel + segment * along)).magnitude;
            float t = Clamp(value: distance / (width * 0.15f));
            float mask = 1 - t * t * (3 - 2 * t);
            return 1 - 0.5f * Clamp(value: obstruction) * mask;
        }

        private static float Clamp(float value) =>
            SpritsailDeployment.Finite(value: value) ? Math.Max(0, Math.Min(1, value)) : 0;

        private static bool Finite(Vector3 point) =>
            SpritsailDeployment.Finite(value: point.x)
            && SpritsailDeployment.Finite(value: point.y)
            && SpritsailDeployment.Finite(value: point.z);
    }
}
