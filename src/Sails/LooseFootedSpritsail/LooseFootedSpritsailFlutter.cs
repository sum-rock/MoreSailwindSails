// Computes wind-driven visual flutter without changing the sail's physical pose.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal static class LooseFootedSpritsailFlutter
    {
        internal static bool CanAnimate(
            bool sailing,
            bool preview,
            bool loading,
            bool updatingOrder,
            bool sunk
        ) => (sailing || preview) && !loading && !updatingOrder && !sunk;

        internal static float Strength(float capturedWind, float windSpeed, float shadow)
        {
            if (!Finite(value: capturedWind) || !Finite(value: windSpeed) || !Finite(value: shadow))
                return 0;
            float filled = Clamp01(value: (capturedWind - 0.15f) / 0.2f);
            filled = filled * filled * (3 - 2 * filled);
            return (1 - filled) * Clamp01(value: windSpeed * Clamp01(value: shadow) / 2);
        }

        internal static float SmoothStrength(float current, float target, float deltaTime)
        {
            float next = LooseFootedSpritsailClothGeometry.SmoothLoad(
                current: current,
                target: target,
                deltaTime: deltaTime
            );
            // Reach exact rest so inactive flutter does not keep uploading the mesh.
            return deltaTime > 0 && Math.Abs(next - target) < 0.0001f ? target : next;
        }

        internal static Vector4 Weights(float u, float v, float footLength)
        {
            float amplitude = Math.Min(footLength * 0.04f, 0.35f) * 4 * u * (1 - u) * (1 - v);
            double first = 2 * Math.PI * (1.25 * u + 0.35 * v);
            double second = 2 * Math.PI * (2 * u - 0.65 * v);
            // sin(time - space) lets the vertex loop use multiply/add only.
            return new Vector4(
                x: amplitude * 0.65f * (float)Math.Cos(first),
                y: -amplitude * 0.65f * (float)Math.Sin(first),
                z: amplitude * 0.35f * (float)Math.Cos(second),
                w: -amplitude * 0.35f * (float)Math.Sin(second)
            );
        }

        internal static Vector4 Waves(double time, float phase, float strength)
        {
            double first = 2 * Math.PI * 1.2 * time + phase;
            double second = 2 * Math.PI * 2.1 * time + phase * 1.73;
            return new Vector4(
                x: (float)Math.Sin(first) * strength,
                y: (float)Math.Cos(first) * strength,
                z: (float)Math.Sin(second) * strength,
                w: (float)Math.Cos(second) * strength
            );
        }

        // Positive local X airflow comes from port; WindLoad uses the -X normal.
        // Its existing deadband supplies zero near head-to-wind: retain the last tack.
        internal static float FoldSide(float current, float pressure, float deltaTime)
        {
            float target =
                pressure < 0 ? -1
                : pressure > 0 ? 1
                : current;
            return SmoothStrength(current: current, target: target, deltaTime: deltaTime);
        }

        // Broad, slow folds use cached spatial coefficients and three scalar wave terms.
        internal static Vector3 GatheredWeights(float u, float v, float footLength)
        {
            float amplitude = Math.Min(footLength * 0.03f, 0.25f) * 4 * u * (1 - u) * (1 - v);
            double phase = Math.PI * (u + 0.5 * v);
            return new Vector3(
                x: amplitude * 0.75f,
                y: amplitude * 0.25f * (float)Math.Cos(phase),
                z: -amplitude * 0.25f * (float)Math.Sin(phase)
            );
        }

        internal static float GatheredStrength(float pressure, float shadow, float deployment)
        {
            if (!Finite(value: pressure) || !Finite(value: shadow) || !Finite(value: deployment))
                return 0;
            float gathered = 1 - Clamp01(value: deployment);
            return Math.Max(-1, Math.Min(1, pressure))
                * Clamp01(value: shadow)
                * gathered
                * gathered
                * (3 - 2 * gathered);
        }

        internal static Vector3 GatheredWaves(double time, float phase, float strength)
        {
            double angle = 2 * Math.PI * 0.35 * time + phase;
            return new Vector3(
                x: strength,
                y: (float)Math.Sin(angle) * strength,
                z: (float)Math.Cos(angle) * strength
            );
        }

        private static float Clamp01(float value) => Math.Max(0, Math.Min(1, value));

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
