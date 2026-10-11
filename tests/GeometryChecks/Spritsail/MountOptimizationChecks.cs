using System;
using System.Diagnostics;
using System.Linq;
using MoreSailwindSails.Sails.Spritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Compares fitted geometry across bone layouts and benchmarks both frozen predecessor fitters.
internal static class MountOptimizationChecks
{
    internal static void Equivalent(
        Vector3[] vertices,
        Vector3[] normals,
        Vector3[] expectedVertices,
        Vector3[] expectedNormals
    )
    {
        for (int i = 0; i < vertices.Length; i++)
            Require(
                value: (vertices[i] - expectedVertices[i]).sqrMagnitude < 1e-10f
                    && (normals[i] - expectedNormals[i]).sqrMagnitude < 1e-6f,
                message: $"Optimized mount must match original positions/normals at vertex {i}: position={(vertices[i] - expectedVertices[i]).magnitude}, normal={(normals[i] - expectedNormals[i]).magnitude}."
            );
    }

    internal static void ExactCached(
        Vector3[] vertices,
        Vector3[] normals,
        Vector3[] expectedVertices,
        Vector3[] expectedNormals
    )
    {
        for (int i = 0; i < vertices.Length; i++)
            Require(
                value: vertices[i].x == expectedVertices[i].x
                    && vertices[i].y == expectedVertices[i].y
                    && vertices[i].z == expectedVertices[i].z
                    && normals[i].x == expectedNormals[i].x
                    && normals[i].y == expectedNormals[i].y
                    && normals[i].z == expectedNormals[i].z,
                message: $"Scalar fitting changed the previous cached result at vertex {i}."
            );
    }

    private static Vector3[] Luff(int count = 33) =>
        Enumerable
            .Range(start: 0, count: count)
            .Select(i => new Vector3(3 - 6f * i / (count - 1), 0, 0))
            .ToArray();

    internal static void Run()
    {
        // Return to the first layout after another count to catch shared-cache contamination.
        foreach (int count in new[] { 33, 17, 33 })
        {
            var luff = Luff(count: count);
            var origin = new Vector3(0, -0.35f, -0.05f);
            var axis = Vector3.right;
            var radii = Enumerable.Repeat(element: 0.3f, count: 7).ToArray();
            var vertices = new Vector3[SpritsailMountAsset.Positions.Length];
            var normals = new Vector3[vertices.Length];
            var expectedVertices = new Vector3[vertices.Length];
            var expectedNormals = new Vector3[vertices.Length];
            var eyelets = new Vector3[7];
            void Fit() =>
                SpritsailMountGeometry.Fit(
                    luff: luff,
                    mastOrigin: origin,
                    mastAxis: axis,
                    radii: radii,
                    vertices: vertices,
                    normals: normals,
                    eyelets: eyelets
                );
            for (int step = 0; step < 4; step++)
            {
                Fit();
                MountCachedReferenceGeometry.Fit(
                    luff: luff,
                    mastOrigin: origin,
                    mastAxis: axis,
                    radii: radii,
                    vertices: expectedVertices,
                    normals: expectedNormals,
                    eyelets: eyelets
                );
                ExactCached(
                    vertices: vertices,
                    normals: normals,
                    expectedVertices: expectedVertices,
                    expectedNormals: expectedNormals
                );
                MountReferenceGeometry.Fit(
                    luff: luff,
                    mastOrigin: origin,
                    mastAxis: axis,
                    radii: radii,
                    vertices: expectedVertices,
                    normals: expectedNormals
                );
                Equivalent(
                    vertices: vertices,
                    normals: normals,
                    expectedVertices: expectedVertices,
                    expectedNormals: expectedNormals
                );
                radii[3] += 0.01f;
                origin += new Vector3(0, -0.02f, 0.01f);
                axis = new Vector3(1, 0.04f * (step + 1), 0.02f).normalized;
                luff[2] += Vector3.forward * 0.02f;
                luff[count / 2] += Vector3.up * 0.03f;
            }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 5; i++)
                Fit();
            Require(
                value: GC.GetAllocatedBytesForCurrentThread() == before,
                message: "Warmed full fitting must reuse its precomputed data and caller buffers."
            );
        }
        Console.WriteLine(
            "PASS: scalar full fits exactly match the previous cached fitter and remain equivalent to the original across changing luff/mast inputs and bone layouts, with allocation-free warmed fitting."
        );
    }

    internal static void Benchmark()
    {
        var luff = Luff();
        var origin = new Vector3(0, -0.35f, -0.05f);
        var axis = new Vector3(1, 0.1f, 0.05f).normalized;
        var radii = Enumerable.Range(start: 0, count: 7).Select(i => 0.2f + i * 0.015f).ToArray();
        var vertices = new Vector3[SpritsailMountAsset.Positions.Length];
        var normals = new Vector3[vertices.Length];
        var eyes = new Vector3[7];
        void Fit(bool original, bool cached = false)
        {
            if (cached)
                MountCachedReferenceGeometry.Fit(
                    luff: luff,
                    mastOrigin: origin,
                    mastAxis: axis,
                    radii: radii,
                    vertices: vertices,
                    normals: normals,
                    eyelets: eyes
                );
            else if (original)
                MountReferenceGeometry.Fit(
                    luff: luff,
                    mastOrigin: origin,
                    mastAxis: axis,
                    radii: radii,
                    vertices: vertices,
                    normals: normals
                );
            else
                SpritsailMountGeometry.Fit(
                    luff: luff,
                    mastOrigin: origin,
                    mastAxis: axis,
                    radii: radii,
                    vertices: vertices,
                    normals: normals,
                    eyelets: eyes
                );
        }
        for (int i = 0; i < 100; i++)
        {
            Fit(original: true);
            Fit(original: false);
            Fit(original: false, cached: true);
        }
        var originalTimes = new double[7];
        var optimizedTimes = new double[7];
        var cachedTimes = new double[7];
        const int iterations = 50;
        double Measure(bool original, bool cached = false)
        {
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < iterations; i++)
                Fit(original: original, cached: cached);
            return (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency / iterations;
        }
        for (int round = 0; round < 7; round++)
        {
            // Alternate order to limit bias from warmup and machine drift.
            if (round % 2 == 0)
            {
                originalTimes[round] = Measure(original: true);
                cachedTimes[round] = Measure(original: false, cached: true);
                optimizedTimes[round] = Measure(original: false);
            }
            else
            {
                optimizedTimes[round] = Measure(original: false);
                cachedTimes[round] = Measure(original: false, cached: true);
                originalTimes[round] = Measure(original: true);
            }
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++)
            Fit(original: false);
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Array.Sort(array: originalTimes);
        Array.Sort(array: optimizedTimes);
        Array.Sort(array: cachedTimes);
        Require(value: allocated == 0, message: "Warm fitting must not allocate managed objects.");
        Console.WriteLine(
            FormattableString.Invariant(
                $"Mount fitting benchmark (.NET, no Unity rendering): median original={originalTimes[3]:F3} ms/full fit previousCached={cachedTimes[3]:F3} ms/full fit optimized={optimizedTimes[3]:F3} ms/full fit speedupVsPrevious={cachedTimes[3] / optimizedTimes[3]:F2}x speedup={originalTimes[3] / optimizedTimes[3]:F2}x allocated={allocated} bytes/{iterations} optimized fits; 7 alternating rounds x {iterations}, after 100 warmups."
            )
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message: message);
    }
}
