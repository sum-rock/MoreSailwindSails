// Checks low-capture motion and preservation of authored attachments across all cuts.
using System;
using MoreSailwindSails.Sails.FishermansStaysail;
using MoreSailwindSails.Sails.FishermansStaysail.MkA;
using MoreSailwindSails.Sails.FishermansStaysail.MkB;
using MoreSailwindSails.Sails.FishermansStaysail.MkC;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail;

internal static class FlutterChecks
{
    internal static void Run()
    {
        foreach (float capture in new[] { 0f, 0.15f })
            Near(
                FishermansStaysailFlutter.Strength(capturedWind: capture, windSpeed: 8, shadow: 1),
                1
            );
        Near(
            FishermansStaysailFlutter.Strength(capturedWind: 0.25f, windSpeed: 8, shadow: 1),
            0.5f
        );
        foreach (float capture in new[] { 0.35f, 1f })
            Near(
                FishermansStaysailFlutter.Strength(capturedWind: capture, windSpeed: 8, shadow: 1),
                0
            );
        Near(FishermansStaysailFlutter.Strength(capturedWind: 0, windSpeed: 0, shadow: 1), 0);
        Near(FishermansStaysailFlutter.Strength(capturedWind: 0, windSpeed: 8, shadow: 0), 0);
        Near(
            FishermansStaysailFlutter.Strength(capturedWind: 0, windSpeed: 1, shadow: 0.5f),
            0.25f
        );
        foreach (
            float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity }
        )
        {
            Near(
                FishermansStaysailFlutter.Strength(capturedWind: invalid, windSpeed: 8, shadow: 1),
                0
            );
            Near(
                FishermansStaysailFlutter.Strength(capturedWind: 0, windSpeed: invalid, shadow: 1),
                0
            );
            Near(
                FishermansStaysailFlutter.Strength(capturedWind: 0, windSpeed: 8, shadow: invalid),
                0
            );
            Near(FishermansStaysailFlutter.Deployment(unroll: invalid), 0);
        }
        foreach (float unroll in new[] { 0f, 0.001f, 0.04f, 0.1f })
            Near(FishermansStaysailFlutter.Deployment(unroll: unroll), 0);
        Near(FishermansStaysailFlutter.Deployment(unroll: 0.175f), 0.0875f);
        Near(FishermansStaysailFlutter.Deployment(unroll: 0.25f), 0.25f);
        Near(FishermansStaysailFlutter.Deployment(unroll: 1), 1);
        float previous = 1;
        for (int i = 0; i <= 100; i++)
        {
            float strength = FishermansStaysailFlutter.Strength(
                capturedWind: i / 100f,
                windSpeed: 8,
                shadow: 1
            );
            if (strength > previous || previous - strength > 0.08f)
                throw new Exception("Capture response must fade continuously without increasing.");
            previous = strength;
        }
        float slow = 0,
            fast = 0;
        for (int i = 0; i < 30; i++)
            slow = FishermansStaysailFlutter.SmoothStrength(
                current: slow,
                target: 1,
                seconds: 1f / 30
            );
        for (int i = 0; i < 120; i++)
            fast = FishermansStaysailFlutter.SmoothStrength(
                current: fast,
                target: 1,
                seconds: 1f / 120
            );
        Near(slow, fast);
        if (slow <= 0 || slow >= 1)
            throw new Exception("Flutter must approach its target smoothly.");
        Near(FishermansStaysailFlutter.SmoothStrength(current: slow, target: 0, seconds: 0), slow);

        foreach (
            var data in new[]
            {
                FishermansStaysailMkABakedMesh.CreateAnimation(),
                FishermansStaysailMkBBakedMesh.CreateAnimation(),
                FishermansStaysailMkCBakedMesh.CreateAnimation(),
            }
        )
            CheckCut(data: data);
        Console.WriteLine(
            "PASS: Staysail flutter response, reef fade, bounded motion and fixed attachments for all cuts."
        );
    }

    private static void CheckCut(FishermansStaysailAnimationData data)
    {
        foreach (float width in new[] { 0.25f, 13.8f, 40f })
        foreach (float slope in new[] { 0f, 20f, 75f })
        {
            var flutter = new FishermansStaysailFlutter(width: width);
            var animation = new FishermansStaysailAnimation(
                data: data,
                width: width,
                headSlope: slope
            );
            bool moved = false;
            foreach (float deployment in new[] { 0f, 0.1f, 0.175f, 0.5f, 1f })
            foreach (float ease in new[] { 0f, 0.5f, 1f })
            foreach (float side in new[] { -1f, 0f, 1f })
            {
                animation.Pose(deployment: deployment, ease: ease, side: side);
                for (int sample = 0; sample < 8; sample++)
                {
                    var waves = FishermansStaysailFlutter.Waves(
                        time: sample * 0.19,
                        phase: 0.7f,
                        strength: FishermansStaysailFlutter.Deployment(unroll: deployment)
                    );
                    for (int i = 0; i < animation.Vertices.Length; i++)
                    {
                        var original = animation.Vertices[i];
                        var visual = flutter.VisualVertex(
                            authored: original,
                            index: i,
                            waves: waves
                        );
                        float offset = Math.Abs(visual.y - original.y);
                        if (
                            !float.IsFinite(visual.sqrMagnitude)
                            || offset > Math.Min(width * 0.04f, 0.35f) + 0.00001f
                            || visual.x != original.x
                            || visual.z != original.z
                        )
                            throw new Exception(
                                "Flutter must remain finite, bounded and depth-only."
                            );
                        bool pinned =
                            i % (FishermansStaysailGeometry.Columns + 1) == 0
                            || Array.IndexOf(data.CornerIndices, i) >= 0;
                        if ((pinned || deployment <= 0.1f) && offset != 0)
                            throw new Exception(
                                "Flutter moved an attachment or nearly furled cloth."
                            );
                        var resting = flutter.VisualVertex(
                            authored: original,
                            index: i,
                            waves: default
                        );
                        if ((resting - original).sqrMagnitude != 0)
                            throw new Exception(
                                "Zero flutter must preserve the exact authored pose."
                            );
                        moved |= offset > 0.0001f;
                    }
                }
            }
            if (!moved)
                throw new Exception("Low-capture cloth must move away from its authored pose.");
        }
    }

    private static void Near(float actual, float expected)
    {
        if (!float.IsFinite(actual) || Math.Abs(actual - expected) > 0.00001f)
            throw new Exception($"Unexpected flutter response: {actual}, expected {expected}.");
    }
}
