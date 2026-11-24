// Adds wind-driven visual motion while leaving authored attachments and trim intact.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    internal sealed class FishermansStaysailFlutter
    {
        private readonly Vector4[] weights;

        internal FishermansStaysailFlutter(float width)
        {
            FishermansStaysailGeometry.ValidateWidth(width: width);
            int columns = FishermansStaysailGeometry.Columns;
            int rows = FishermansStaysailGeometry.Rows;
            weights = new Vector4[(columns + 1) * (rows + 1)];
            for (int row = 0; row <= rows; row++)
            for (int col = 0; col <= columns; col++)
            {
                float u = (float)col / columns;
                float v = (float)row / rows;
                float mask = Clamp01(value: 4 * u * (1 - u) + 4 * u * u * v * (1 - v));
                float amplitude = Math.Min(width * 0.04f, 0.35f) * mask;
                double first = 2 * Math.PI * (1.25 * u + 0.35 * v);
                double second = 2 * Math.PI * (2 * u - 0.65 * v);
                // Expand sin(time - space) so the vertex loop needs no trigonometry.
                weights[row * (columns + 1) + col] = new Vector4(
                    x: amplitude * 0.65f * (float)Math.Cos(first),
                    y: -amplitude * 0.65f * (float)Math.Sin(first),
                    z: amplitude * 0.35f * (float)Math.Cos(second),
                    w: -amplitude * 0.35f * (float)Math.Sin(second)
                );
            }
        }

        internal Vector3 VisualVertex(Vector3 authored, int index, Vector4 waves)
        {
            var weight = weights[index];
            authored.y +=
                weight.x * waves.x + weight.y * waves.y + weight.z * waves.z + weight.w * waves.w;
            return authored;
        }

        internal static float Strength(float capturedWind, float windSpeed, float shadow)
        {
            if (!Finite(value: capturedWind) || !Finite(value: windSpeed) || !Finite(value: shadow))
                return 0;
            float filled = SmoothStep(value: (capturedWind - 0.15f) / 0.2f);
            return (1 - filled) * Clamp01(value: windSpeed * Clamp01(value: shadow) / 2);
        }

        internal static float Deployment(float unroll) =>
            Finite(value: unroll)
                ? Clamp01(value: unroll) * SmoothStep(value: (unroll - 0.1f) / 0.15f)
                : 0;

        internal static float SmoothStrength(float current, float target, float seconds)
        {
            float next = FishermansStaysailFixedHead.SmoothLoad(
                previous: current,
                target: target,
                seconds: seconds
            );
            return seconds > 0 && Math.Abs(next - target) < 0.0001f ? target : next;
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

        private static float SmoothStep(float value)
        {
            float t = Clamp01(value: value);
            return t * t * (3 - 2 * t);
        }

        private static float Clamp01(float value) => Math.Max(0, Math.Min(1, value));

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
