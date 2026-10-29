// Checks wind-driven visual movement while preserving the physical sail and attachments.
using System;
using MoreSailwindSails.Sails.LooseFootedSpritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.LooseFootedSpritsail;

internal static class FlutterChecks
{
    internal static void Run()
    {
        Near(actual: Strength(capture: 0, speed: 8), expected: 1);
        Near(actual: Strength(capture: 0.15f, speed: 8), expected: 1);
        Near(actual: Strength(capture: 0.25f, speed: 8), expected: 0.5f);
        Near(actual: Strength(capture: 0.35f, speed: 8), expected: 0);
        Near(actual: Strength(capture: 1, speed: 8), expected: 0);
        Near(actual: Strength(capture: 0, speed: 0), expected: 0);
        Near(actual: Strength(capture: 0, speed: 1), expected: 0.5f);
        Near(actual: Strength(capture: 0, speed: 8, shadow: 0), expected: 0);
        Near(actual: Strength(capture: 0, speed: 2, shadow: 0.5f), expected: 0.5f);
        Near(actual: Strength(capture: float.NaN, speed: 8), expected: 0);
        Near(actual: Strength(capture: 0, speed: float.PositiveInfinity), expected: 0);
        Near(actual: Strength(capture: 0, speed: 8, shadow: float.NaN), expected: 0);
        Near(actual: Strength(capture: -1, speed: -2), expected: 0);
        Near(actual: Strength(capture: -1, speed: 8, shadow: 2), expected: 1);

        float smooth = LooseFootedSpritsailFlutter.SmoothStrength(
            current: 0,
            target: 1,
            deltaTime: 1f / 60
        );
        Check(smooth > 0 && smooth < 0.1f, "Luffing onset must ease in.");
        Near(
            actual: LooseFootedSpritsailFlutter.SmoothStrength(
                current: smooth,
                target: 0,
                deltaTime: 0
            ),
            expected: smooth
        );
        for (int i = 0; i < 300; i++)
            smooth = LooseFootedSpritsailFlutter.SmoothStrength(
                current: smooth,
                target: 0,
                deltaTime: 1f / 60
            );
        Check(smooth == 0, "Settled flutter must reach exact zero to stop mesh uploads.");
        Near(actual: Ramp(fps: 30), expected: Ramp(fps: 120));

        var data = LooseFootedSpritsailMkABakedAsset.Create();
        float scale = LooseFootedSpritsailFittingGeometry.BaseScale(
            data: data,
            fullGaffHeight: 8.8f
        );
        foreach (float height in new[] { 0.2f, 1f, 3f })
        foreach (float width in new[] { 0.2f, 1f, 3f })
        {
            var rest = LooseFootedSpritsailFittingGeometry.Pose(
                data: data,
                baseScale: scale,
                height: height,
                width: width,
                mastRadius: 0.3f
            );
            var cloth = new LooseFootedSpritsailClothGeometry(data: data, rest: rest);
            float limit = Math.Min((cloth.Clew - cloth.Tack).magnitude * 0.04f, 0.35f);
            foreach (float deployment in new[] { 0f, 0.25f, 0.75f, 1f })
            foreach (float load in new[] { -1f, 0f, 1f })
            {
                cloth.Pose(deployment: deployment, load: load);
                var saved = (Vector3[])cloth.Vertices.Clone();
                var wave = LooseFootedSpritsailFlutter.Waves(
                    time: 0.37,
                    phase: 1,
                    strength: deployment
                );
                var later = LooseFootedSpritsailFlutter.Waves(
                    time: 0.71,
                    phase: 1,
                    strength: deployment
                );
                var other = LooseFootedSpritsailFlutter.Waves(
                    time: 0.37,
                    phase: 2,
                    strength: deployment
                );
                bool moves = false,
                    differs = false;
                for (int i = 0; i < saved.Length; i++)
                {
                    var visual = cloth.VisualVertex(index: i, flutter: wave);
                    Check(float.IsFinite(visual.x), "Flutter produced an invalid vertex.");
                    Near(actual: visual.y, expected: saved[i].y);
                    Near(actual: visual.z, expected: saved[i].z);
                    Check(
                        Math.Abs(visual.x - saved[i].x) <= limit * deployment + 0.00001f,
                        "Flutter exceeded its size/deployment limit."
                    );
                    Near(
                        actual: cloth.VisualVertex(index: i, flutter: Vector4.zero).x,
                        expected: saved[i].x
                    );
                    moves |=
                        Math.Abs(visual.x - cloth.VisualVertex(index: i, flutter: later).x)
                        > 0.0001f;
                    differs |=
                        Math.Abs(visual.x - cloth.VisualVertex(index: i, flutter: other).x)
                        > 0.0001f;
                    Check(
                        cloth.Vertices[i] == saved[i],
                        "Visual flutter mutated physical geometry."
                    );
                    var source = data.Sail.Vertices[i];
                    if (
                        source.z == data.Sail.Vertices[data.Tack].z
                        || source.z == data.Sail.Vertices[data.Clew].z
                    )
                        Near(actual: visual.x, expected: saved[i].x);
                    // Returning to the same explicit time and phase must reproduce the vertex.
                    Near(actual: cloth.VisualVertex(index: i, flutter: wave).x, expected: visual.x);
                }
                Check(moves == (deployment > 0), "Only deployed cloth should move over time.");
                Check(
                    differs == (deployment > 0),
                    "Instance phases should separate flutter motion."
                );
                foreach (int corner in new[] { data.Throat, data.Tack, data.Peak, data.Clew })
                    Check(
                        cloth.VisualVertex(index: corner, flutter: wave) == saved[corner],
                        "Flutter detached a corner from its support."
                    );
            }
        }
        Console.WriteLine(
            "PASS: spritsail flutter gating, smoothing, pause, phase, size bounds and visual-only attachments across sizes, deployments and tacks."
        );
    }

    private static float Strength(float capture, float speed, float shadow = 1) =>
        LooseFootedSpritsailFlutter.Strength(
            capturedWind: capture,
            windSpeed: speed,
            shadow: shadow
        );

    private static float Ramp(int fps)
    {
        float value = 0;
        for (int i = 0; i < fps; i++)
            value = LooseFootedSpritsailFlutter.SmoothStrength(
                current: value,
                target: 1,
                deltaTime: 1f / fps
            );
        return value;
    }

    private static void Near(float actual, float expected) =>
        Check(Math.Abs(actual - expected) < 0.00001f, $"Expected {expected}, got {actual}.");

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
