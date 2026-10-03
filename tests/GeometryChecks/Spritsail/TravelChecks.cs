using System;
using MoreSailwindSails.Sails.Spritsail;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Exercises the shared envelope with independent sheets, native sway and obstructions.
internal static class TravelChecks
{
    internal static void Run()
    {
        Check(
            SpritsailTravel.Clamp(-120) == -89 && SpritsailTravel.Clamp(120) == 89,
            "Spritsails must allow the native 89-degree envelope on each tack."
        );
        foreach (float angle in new[] { -89f, -25f, 0f, 15f, 89f })
            Check(
                SpritsailTravel.Clamp(angle) == angle,
                "Existing narrower collision limits must remain unchanged."
            );

        Range(-89, 89, -89, 89, -89, 89);
        Range(-89.5f, 89.5f, -89, 89, -89, 89);
        Range(-89, 89, -20, 30, -20, 30);
        Range(-89, 89, -89, 25, -89, 25);
        Range(-10.5f, 15.5f, -89, 89, -10.5f, 15.5f);
        // Stale sheet targets entirely beyond a new limit must collapse at
        // the boundary, not push the sail out or invert the joint limits.
        Range(100, 120, -89, 89, 89, 89);
        Range(-120, -100, -89, 89, -89, -89);
        Range(10.5f, 9.5f, -89, 89, 10, 10);

        // Native Update combines the two sheet ranges, then adds signed sway.
        // Exercise independently tightened/eased sheets on both tacks, including
        // one controller still using the previous frame's wider collision limits.
        foreach (float savedMin in new[] { -89f, -60f, -20f })
        foreach (float savedMax in new[] { 89f, 60f, 30f })
        foreach (float left in new[] { 0f, 0.25f, 0.75f, 1f })
        foreach (float right in new[] { 0f, 0.25f, 0.75f, 1f })
        foreach (float sway in new[] { -0.5f, 0f, 0.5f })
        {
            float min = Math.Max(Lerp(-1, savedMin, left), Lerp(-1, -89, right)) - sway;
            float max = Math.Min(Lerp(1, savedMax, left), Lerp(1, 89, right)) + sway;
            SpritsailTravel.ConstrainHinge(
                min: ref min,
                max: ref max,
                allowedMin: savedMin,
                allowedMax: savedMax
            );
            Check(
                min >= -89 && max <= 89 && min <= max,
                "Sheet/sway combinations must produce an ordered range within 89 degrees."
            );
            Check(
                min >= savedMin && max <= savedMax,
                "Sway must not widen a tighter obstruction limit."
            );
        }
        Console.WriteLine(
            "PASS: 89-degree travel, outer limits, asymmetric obstructions, independent sheets and bounded native sway."
        );
    }

    private static float Lerp(float from, float to, float t) => from + (to - from) * t;

    private static void Range(
        float min,
        float max,
        float allowedMin,
        float allowedMax,
        float expectedMin,
        float expectedMax
    )
    {
        SpritsailTravel.ConstrainHinge(
            min: ref min,
            max: ref max,
            allowedMin: allowedMin,
            allowedMax: allowedMax
        );
        Check(
            min == expectedMin && max == expectedMax,
            $"Unexpected constrained hinge range: [{min}, {max}]."
        );
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
