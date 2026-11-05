// Checks wind-driven visual movement while preserving the physical sail and attachments.
using System;
using MoreSailwindSails.Sails.LooseFootedSpritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.LooseFootedSpritsail;

internal static class FlutterChecks
{
    internal static void Run()
    {
        foreach (bool sailing in new[] { false, true })
        foreach (bool preview in new[] { false, true })
        {
            Check(
                LooseFootedSpritsailFlutter.CanAnimate(
                    sailing: sailing,
                    preview: preview,
                    loading: false,
                    updatingOrder: false,
                    sunk: false
                ) == (sailing || preview),
                "Shipyard animation must not require sailing admission."
            );
            for (int guard = 0; guard < 3; guard++)
                Check(
                    !LooseFootedSpritsailFlutter.CanAnimate(
                        sailing: sailing,
                        preview: preview,
                        loading: guard == 0,
                        updatingOrder: guard == 1,
                        sunk: guard == 2
                    ),
                    "Loading, order updates and sinking must suppress flutter in both modes."
                );
        }
        foreach (float speed in new[] { -16f, -8f, -4f, -0.5f, 0f, 0.5f, 4f, 8f, 16f })
        {
            float load = LooseFootedSpritsailClothGeometry.WindLoad(
                wind: Vector3.right * speed,
                normal: Vector3.right
            );
            Near(actual: load, expected: Math.Abs(speed) < 0.6f ? 0 : Math.Clamp(speed / 8, -1, 1));
            Near(
                actual: LooseFootedSpritsailClothGeometry.WindLoad(
                    wind: Vector3.forward * speed,
                    normal: Vector3.right
                ),
                expected: 0
            );
            Near(
                actual: LooseFootedSpritsailClothGeometry.SmoothLoad(
                    current: 0.25f,
                    target: load,
                    deltaTime: 0
                ),
                expected: 0.25f
            );
        }
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
        CheckGathered();
        Console.WriteLine(
            "PASS: spritsail flutter gating, smoothing, pause, phase, size bounds and visual-only attachments across sizes, deployments and tacks."
        );
    }

    private static void CheckGathered()
    {
        var data = LooseFootedSpritsailMkABakedAsset.Create();
        foreach (float size in new[] { 0.2f, 1f, 3f })
        {
            var rest = LooseFootedSpritsailFittingGeometry.Pose(
                data: data,
                baseScale: 1,
                height: size,
                width: size,
                mastRadius: 0.3f
            );
            var cloth = new LooseFootedSpritsailClothGeometry(data: data, rest: rest);
            float limit = Math.Min((cloth.Clew - cloth.Tack).magnitude * 0.03f, 0.25f);
            foreach (float deployment in new[] { 0.002f, 0.25f, 0.5f, 0.75f, 1f })
            {
                cloth.Pose(deployment: deployment, load: 0);
                float strength = LooseFootedSpritsailFlutter.GatheredStrength(
                    pressure: 1,
                    shadow: 1,
                    deployment: deployment
                );
                var wave = LooseFootedSpritsailFlutter.GatheredWaves(
                    time: 1,
                    phase: 0,
                    strength: strength
                );
                var later = LooseFootedSpritsailFlutter.GatheredWaves(
                    time: 2,
                    phase: 0,
                    strength: strength
                );
                var other = LooseFootedSpritsailFlutter.GatheredWaves(
                    time: 1,
                    phase: 1,
                    strength: strength
                );
                bool moves = false,
                    differs = false;
                for (int i = 0; i < cloth.Vertices.Length; i++)
                {
                    var physical = cloth.Vertices[i];
                    var visual = cloth.VisualVertex(
                        index: i,
                        flutter: Vector4.zero,
                        gathered: wave
                    );
                    Check(
                        Math.Abs(visual.x - physical.x) <= limit + 0.00001f,
                        "Gathered billow exceeded cap."
                    );
                    Near(actual: visual.y, expected: physical.y);
                    Near(actual: visual.z, expected: physical.z);
                    var reverse = cloth.VisualVertex(
                        index: i,
                        flutter: Vector4.zero,
                        gathered: -wave
                    );
                    Near(actual: visual.x - physical.x, expected: physical.x - reverse.x);
                    Check(cloth.Vertices[i] == physical, "Gathered motion changed physics.");
                    moves |=
                        Math.Abs(
                            visual.x
                                - cloth
                                    .VisualVertex(index: i, flutter: Vector4.zero, gathered: later)
                                    .x
                        ) > 0.00001f;
                    differs |=
                        Math.Abs(
                            visual.x
                                - cloth
                                    .VisualVertex(index: i, flutter: Vector4.zero, gathered: other)
                                    .x
                        ) > 0.00001f;
                }
                Check(moves == (deployment < 1), "Gathered folds must move until fully deployed.");
                Check(differs == (deployment < 1), "Gathered sails need independent phases.");
                foreach (
                    int corner in new[]
                    {
                        data.Tack,
                        data.Throat,
                        data.Peak,
                        data.Clew,
                        data.SheetVertex,
                    }
                )
                    Check(
                        cloth.VisualVertex(index: corner, flutter: Vector4.zero, gathered: wave)
                            == cloth.Vertices[corner],
                        "Gathered billow moved an attachment."
                    );
            }
            // Warm the exact managed visual loop before checking steady-state allocation.
            float checksum = 0;
            for (int pass = 0; pass < 2; pass++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int frame = 0; frame < 100; frame++)
                {
                    var wave = LooseFootedSpritsailFlutter.GatheredWaves(
                        time: frame / 60.0,
                        phase: 1,
                        strength: 1
                    );
                    for (int i = 0; i < cloth.Vertices.Length; i++)
                        checksum += cloth
                            .VisualVertex(index: i, flutter: Vector4.zero, gathered: wave)
                            .x;
                }
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                if (pass == 1)
                    Check(allocated == 0, "Gathered visual loop allocates after fitting.");
            }
            Check(float.IsFinite(checksum), "Invalid gathered allocation-test result.");
        }
        Near(
            actual: LooseFootedSpritsailFlutter.GatheredStrength(
                pressure: 0,
                shadow: 1,
                deployment: 0.1f
            ),
            expected: 0
        );
        Near(
            actual: LooseFootedSpritsailFlutter.GatheredStrength(
                pressure: 1,
                shadow: 0,
                deployment: 0.1f
            ),
            expected: 0
        );
        float settling = 1;
        for (int i = 0; i < 400; i++)
            settling = LooseFootedSpritsailFlutter.SmoothStrength(
                current: settling,
                target: 0,
                deltaTime: 1f / 60
            );
        Near(actual: settling, expected: 0);
        Check(settling == 0, "Calm gathered cloth must settle to exact rest.");
        Near(
            actual: LooseFootedSpritsailFlutter.SmoothStrength(
                current: 0.4f,
                target: -1,
                deltaTime: 0
            ),
            expected: 0.4f
        );
        float previous = 1;
        for (int i = 0; i <= 100; i++)
        {
            float strength = LooseFootedSpritsailFlutter.GatheredStrength(
                pressure: 1,
                shadow: 1,
                deployment: i / 100f
            );
            Check(
                strength <= previous && previous - strength < 0.016f,
                "Gathered transition is discontinuous."
            );
            previous = strength;
        }
        Console.WriteLine(
            "PASS: gathered billow limits, wind reversal, attachments, phase, calm settling, transitions and zero managed visual-loop allocations."
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
