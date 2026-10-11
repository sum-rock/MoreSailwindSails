using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MoreSailwindSails.Sails.Spritsail;
using MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail;
using MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.MkA;
using MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.MkB;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail.LooseFootedSpritsail;

// Compares pruning and authored weights with the frozen solver across live-shape inputs.
internal static class FlexOptimizationChecks
{
    private static readonly LooseFootedSpritsailDefinition[] definitions =
    {
        LooseFootedSpritsailMkA.Definition,
        LooseFootedSpritsailMkB.Definition,
    };

    private static (Vector3 Requested, Vector3[] Foot, Vector3[] Leech, float Limit)[] Cases()
    {
        var cases = new List<(Vector3, Vector3[], Vector3[], float)>();
        var random = new System.Random(731);
        foreach (var definition in definitions)
        foreach (
            var scale in new[] { Vector3.one, new Vector3(.6f, 1.3f, 1.5f), new Vector3(2, 2, 2) }
        )
        foreach (float amount in new[] { 0f, .05f, .25f, .7f, 1f })
        foreach (float camber in new[] { -1f, 0f, 1f })
        foreach (float obstruction in new[] { 0f, 1f })
        {
            var corners = definition.Corners(width: 6);
            var scaled = corners.Select(c => Vector3.Scale(c, scale)).ToArray();
            var pose = SpritsailDeployment.Evaluate(corners: scaled, unroll: .02f + .96f * amount);
            var foot = new Vector3[13];
            var leech = new Vector3[33];
            for (int i = 0; i < foot.Length; i++)
                foot[i] = LooseFootedSpritsailGathering.Point(
                    corners: scaled,
                    pose: pose,
                    amount: amount,
                    u: i / 12f,
                    v: 1,
                    camber: camber,
                    normalScale: scale.y / scale.z,
                    obstruction: obstruction
                );
            for (int i = 0; i < leech.Length; i++)
                leech[i] = LooseFootedSpritsailGathering.Point(
                    corners: scaled,
                    pose: pose,
                    amount: amount,
                    u: 1,
                    v: i / 32f,
                    camber: camber,
                    normalScale: scale.y / scale.z,
                    obstruction: obstruction
                );
            float limit =
                (scaled[3] - scaled[2]).magnitude
                * LooseFootedSpritsailFlex.MaximumDisplacement
                * amount;
            foreach (
                var direction in new[]
                {
                    Vector3.zero,
                    Vector3.up,
                    Vector3.down,
                    new Vector3(-.2f, 1, -.3f).normalized,
                    new Vector3(.4f, -1, .2f).normalized,
                    new Vector3(
                        (float)random.NextDouble() - .5f,
                        (float)random.NextDouble() - .5f,
                        (float)random.NextDouble() - .5f
                    ).normalized,
                }
            )
            foreach (float load in new[] { .01f, .5f, 1f, 2f })
                cases.Add((direction * limit * load, foot, leech, limit));
        }
        return cases.ToArray();
    }

    internal static void Run()
    {
        var cases = Cases();
        foreach (var c in cases)
        {
            var expected = FlexReference.Fit(
                requested: c.Requested,
                foot: c.Foot,
                leech: c.Leech,
                limit: c.Limit
            );
            var actual = LooseFootedSpritsailFlex.Fit(
                requested: c.Requested,
                foot: c.Foot,
                leech: c.Leech,
                limit: c.Limit
            );
            Require(
                value: actual.x == expected.x && actual.y == expected.y && actual.z == expected.z,
                message: $"Pruning changed fitted displacement: expected {expected.x}/{expected.y}/{expected.z}, got {actual.x}/{actual.y}/{actual.z}."
            );
            Require(
                value: actual.magnitude <= c.Limit + 1e-5f,
                message: "Flex exceeded its travel limit."
            );
            foreach (var edge in new[] { c.Foot, c.Leech })
            {
                float budget = FlexReference.Arc(edge: edge, displacement: Vector3.zero);
                Require(
                    value: FlexReference.Arc(edge: edge, displacement: actual)
                        <= budget + Math.Max(1e-5f, budget * 1e-6f),
                    message: "Optimized fit stretched a sampled edge."
                );
            }
        }
        foreach (var definition in definitions)
        {
            var corners = definition.Corners(width: 6);
            var weights = LooseFootedSpritsailFlex.Weights(corners: corners);
            int fixedBones = 0;
            for (int row = 0; row <= LooseFootedSpritsailGeometry.Rows; row++)
            for (int column = 0; column <= LooseFootedSpritsailGeometry.ShapeColumns; column++)
            {
                float u = column / (float)LooseFootedSpritsailGeometry.ShapeColumns;
                float v = row / (float)LooseFootedSpritsailGeometry.Rows;
                var rest = Vector3.Lerp(
                    Vector3.Lerp(corners[0], corners[2], v),
                    Vector3.Lerp(corners[1], corners[3], v),
                    u
                );
                float expected = FlexReference.Weight(
                    point: rest,
                    peak: corners[1],
                    tack: corners[2],
                    clew: corners[3]
                );
                Require(
                    value: weights[LooseFootedSpritsailGeometry.ShapeBone(row: row, column: column)]
                        == expected,
                    message: "Precomputed weight must match the original per-frame expression at every bone."
                );
                if (expected == 0)
                    fixedBones++;
            }
            Require(
                value: fixedBones > 0,
                message: "Fixed upper-panel bones must receive zero weight."
            );
        }
        var sample = cases.First(c => c.Limit > 0 && c.Requested.sqrMagnitude > 0);
        for (int i = 0; i < 100; i++)
            LooseFootedSpritsailFlex.Fit(sample.Requested, sample.Foot, sample.Leech, sample.Limit);
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
            LooseFootedSpritsailFlex.Fit(sample.Requested, sample.Foot, sample.Leech, sample.Limit);
        Require(
            value: GC.GetAllocatedBytesForCurrentThread() == allocated,
            message: "Warmed solver must not allocate."
        );
        Console.WriteLine(
            $"PASS: {cases.Length} flex fixtures exactly match the frozen solver across marks, scales, reefs, camber, obstruction, load and direction; authored weights match at every bone; warmed fitting allocates zero bytes."
        );
    }

    internal static void Benchmark()
    {
        var cases = Cases().Where(c => c.Limit > 0 && c.Requested.sqrMagnitude > 0).ToArray();
        float checksum = 0;
        double Batch(bool reference)
        {
            long start = Stopwatch.GetTimestamp();
            foreach (var c in cases)
            {
                var value = reference
                    ? FlexReference.Fit(c.Requested, c.Foot, c.Leech, c.Limit)
                    : LooseFootedSpritsailFlex.Fit(c.Requested, c.Foot, c.Leech, c.Limit);
                checksum += value.sqrMagnitude;
            }
            return (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency / cases.Length;
        }
        Batch(reference: true);
        Batch(reference: false);
        var original = new double[7];
        var optimized = new double[7];
        for (int round = 0; round < 7; round++)
        {
            if (round % 2 == 0)
            {
                original[round] = Batch(reference: true);
                optimized[round] = Batch(reference: false);
            }
            else
            {
                optimized[round] = Batch(reference: false);
                original[round] = Batch(reference: true);
            }
        }
        Array.Sort(original);
        Array.Sort(optimized);
        Console.WriteLine(
            $"Flex benchmark: {cases.Length} active fixtures, seven alternating rounds; median reference={original[3]:F4} ms/fit, optimized={optimized[3]:F4} ms/fit, speedup={original[3] / optimized[3]:F2}x, checksum={checksum:F3}. Offline .NET solver timing excludes Unity bone updates and is not game frame time."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }
}
