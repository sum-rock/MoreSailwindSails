using System;
using System.Diagnostics;
using System.Linq;
using MoreSailwindSails.Sails.Spritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Compares selective fitting against full geometry and benchmarks the frozen original fitter.
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

    private static Vector3[] Luff(int count = 33) =>
        Enumerable
            .Range(start: 0, count: count)
            .Select(i => new Vector3(3 - 6f * i / (count - 1), 0, 0))
            .ToArray();

    internal static void Run()
    {
        // A second bone count verifies that interpolation caches cannot leak between layouts.
        foreach (int count in new[] { 33, 17 })
        {
            var luff = Luff(count: count);
            var origin = new Vector3(0, -0.35f, -0.05f);
            var axis = Vector3.right;
            var radii = Enumerable.Repeat(element: 0.3f, count: 7).ToArray();
            var state = new SpritsailMountFitState(luffCount: count);
            var vertices = new Vector3[SpritsailMountAsset.Positions.Length];
            var normals = new Vector3[vertices.Length];
            var fullVertices = new Vector3[vertices.Length];
            var fullNormals = new Vector3[vertices.Length];
            var eyelets = new Vector3[7];
            int Changes(bool support, out int reasons) =>
                state.Changes(
                    luff: luff,
                    origin: origin,
                    axis: axis,
                    currentRadii: radii,
                    supportChanged: support,
                    reasons: out reasons
                );
            void Update(int parts)
            {
                var before = (Vector3[])vertices.Clone();
                SpritsailMountGeometry.Fit(
                    luff: luff,
                    mastOrigin: origin,
                    mastAxis: axis,
                    radii: radii,
                    vertices: vertices,
                    normals: normals,
                    eyelets: eyelets,
                    parts: parts
                );
                state.Commit(
                    parts: parts,
                    luff: luff,
                    origin: origin,
                    axis: axis,
                    currentRadii: radii
                );
                MountReferenceGeometry.Fit(
                    luff: luff,
                    mastOrigin: origin,
                    mastAxis: axis,
                    radii: radii,
                    vertices: fullVertices,
                    normals: fullNormals
                );
                Equivalent(
                    vertices: vertices,
                    normals: normals,
                    expectedVertices: fullVertices,
                    expectedNormals: fullNormals
                );
                for (int i = 0; i < vertices.Length; i++)
                    if ((parts & (1 << SpritsailMountAsset.Parts[i])) == 0)
                        Require(
                            value: (vertices[i] - before[i]).sqrMagnitude == 0,
                            message: "Selective fitting must leave unrelated vertices untouched."
                        );
            }

            int parts = Changes(support: false, reasons: out int reasons);
            Require(
                value: parts == SpritsailMountGeometry.AllParts
                    && reasons == SpritsailMountFitState.Initial,
                message: "Initial fitting must initialize every part."
            );
            Update(parts: parts);
            Require(
                value: Changes(support: false, reasons: out _) == 0,
                message: "Unchanged geometry must reuse its mesh."
            );

            radii[3] += 0.01f;
            parts = Changes(support: false, reasons: out reasons);
            Require(
                value: parts == 1 << 11 && reasons == SpritsailMountFitState.Radius,
                message: "A single changed mast radius must update only its rope."
            );
            Update(parts: parts);

            origin += new Vector3(0, -0.02f, 0.01f);
            axis = new Vector3(1, 0.04f, 0.02f).normalized;
            parts = Changes(support: false, reasons: out reasons);
            Require(
                value: parts == 0x7f00 && reasons == SpritsailMountFitState.Mast,
                message: "Mast motion must update all ropes without refitting cloth or eyelets."
            );
            Update(parts: parts);

            // Move a luff row between eyelets: only the strip depends on it.
            luff[2] += Vector3.forward * 0.02f;
            parts = Changes(support: false, reasons: out reasons);
            if (count == 33)
                Require(
                    value: parts == 1 && reasons == SpritsailMountFitState.Luff,
                    message: "Luff movement between fittings must update only the edging."
                );
            Update(parts: parts);

            // Represents several skipped draw calls in BYPASS: do not advance snapshots.
            for (int row = 0; row < count; row++)
                luff[row] += Vector3.up * 0.03f;
            parts = Changes(support: false, reasons: out reasons);
            Require(
                value: parts == SpritsailMountGeometry.AllParts
                    && (reasons & SpritsailMountFitState.Eyelet) != 0,
                message: "Resuming after movement during bypass must update all affected parts."
            );
            Update(parts: parts);

            Require(
                value: Changes(support: true, reasons: out reasons)
                    == SpritsailMountGeometry.AllParts
                    && reasons == SpritsailMountFitState.Support,
                message: "Replacing the mast must invalidate the entire mount even at the same pose."
            );
            // No commit simulates an interrupted update; invalidation must remain pending.
            Require(
                value: Changes(support: true, reasons: out _) == SpritsailMountGeometry.AllParts,
                message: "An uncommitted support replacement must remain dirty."
            );
            Update(parts: SpritsailMountGeometry.AllParts);
        }
        AccumulatedMovement();
        Console.WriteLine(
            "PASS: mount selective updates, unchanged reuse, support replacement, bypass resumption and accumulated sub-threshold motion."
        );
    }

    private static void AccumulatedMovement()
    {
        var luff = Luff();
        var origin = new Vector3(0, -0.35f, 0);
        var axis = Vector3.right;
        var radii = Enumerable.Repeat(element: 0.3f, count: 7).ToArray();
        var state = new SpritsailMountFitState(luffCount: luff.Length);
        void Commit(int parts) =>
            state.Commit(parts: parts, luff: luff, origin: origin, axis: axis, currentRadii: radii);
        int Changes() =>
            state.Changes(
                luff: luff,
                origin: origin,
                axis: axis,
                currentRadii: radii,
                supportChanged: false,
                reasons: out _
            );
        Commit(parts: SpritsailMountGeometry.AllParts);
        for (int step = 1; step <= 3; step++)
        {
            // Refit rope zero every step; other ropes must retain their own older snapshots.
            radii[0] += 0.001f;
            origin += Vector3.up * 0.00004f;
            int parts = Changes();
            Require(
                value: parts == (step < 3 ? 1 << 8 : 0x7f00),
                message: "Small mast movements must accumulate across unrelated rope refits."
            );
            Commit(parts: parts);
        }
        for (int step = 1; step <= 3; step++)
        {
            // Keep the strip dirty elsewhere while the central eyelet moves in smaller steps.
            luff[2] += Vector3.forward * 0.001f;
            luff[16] += Vector3.up * 0.00004f;
            int parts = Changes();
            int center = (1 << 4) | (1 << 11);
            Require(
                value: (parts & center) == (step < 3 ? 0 : center),
                message: "Updating the strip cannot consume a rigid fitting's accumulated movement."
            );
            Commit(parts: parts);
        }
        for (int step = 1; step <= 3; step++)
        {
            radii[0] += 0.001f;
            radii[6] += 0.00004f;
            int parts = Changes();
            Require(
                value: (parts & (1 << 14)) == (step < 3 ? 0 : 1 << 14),
                message: "Each rope must accumulate radius changes against its last fitted radius."
            );
            Commit(parts: parts);
        }
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
        void Fit(bool original, int parts)
        {
            if (original)
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
                    eyelets: eyes,
                    parts: parts
                );
        }
        for (int i = 0; i < 100; i++)
        {
            Fit(original: true, parts: SpritsailMountGeometry.AllParts);
            Fit(original: false, parts: SpritsailMountGeometry.AllParts);
        }
        var originalTimes = new double[7];
        var optimizedTimes = new double[7];
        var ropeTimes = new double[7];
        const int iterations = 50;
        double Measure(bool original, int parts)
        {
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < iterations; i++)
                Fit(original: original, parts: parts);
            return (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency / iterations;
        }
        for (int round = 0; round < 7; round++)
        {
            // Alternate order to limit bias from warmup and machine drift.
            if (round % 2 == 0)
            {
                originalTimes[round] = Measure(
                    original: true,
                    parts: SpritsailMountGeometry.AllParts
                );
                optimizedTimes[round] = Measure(
                    original: false,
                    parts: SpritsailMountGeometry.AllParts
                );
            }
            else
            {
                optimizedTimes[round] = Measure(
                    original: false,
                    parts: SpritsailMountGeometry.AllParts
                );
                originalTimes[round] = Measure(
                    original: true,
                    parts: SpritsailMountGeometry.AllParts
                );
            }
            ropeTimes[round] = Measure(original: false, parts: 0x7f00);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++)
            Fit(original: false, parts: SpritsailMountGeometry.AllParts);
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Array.Sort(array: originalTimes);
        Array.Sort(array: optimizedTimes);
        Array.Sort(array: ropeTimes);
        Require(value: allocated == 0, message: "Warm fitting must not allocate managed objects.");
        Console.WriteLine(
            FormattableString.Invariant(
                $"Mount fitting benchmark (.NET, no Unity rendering): median original={originalTimes[3]:F3} ms/full fit optimized={optimizedTimes[3]:F3} ms/full fit ropesOnly={ropeTimes[3]:F3} ms/fit speedup={originalTimes[3] / optimizedTimes[3]:F2}x allocated={allocated} bytes/{iterations} optimized fits; 7 alternating rounds x {iterations}, after 100 warmups."
            )
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message: message);
    }
}
