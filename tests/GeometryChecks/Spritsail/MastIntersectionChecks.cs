using System;
using System.Collections.Generic;
using System.Diagnostics;
using MoreSailwindSails.Sails.Spritsail;
using UnityEngine;
using Random = System.Random;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Checks exact intersection behavior against the preceding scan and times uncached mount queries.
internal static class MastIntersectionChecks
{
    internal static void Run()
    {
        int queries = 0;
        foreach (int axis in new[] { 0, 1, 2 })
        foreach (int sides in new[] { 4, 8, 32 })
        foreach (bool reverse in new[] { false, true })
        {
            var mesh = Timber(sides: sides, sections: 8, axis: axis, reverse: reverse);
            var geometry = new SpritsailMastGeometry(
                vertices: mesh.Vertices,
                triangles: mesh.Indices
            );
            var random = new Random(Seed: 741);
            for (int i = 0; i < 2000; i++)
            {
                // Include rays from inside/outside, axial components, and transformed ray lengths.
                var origin = new Vector3(Next(random), Next(random), 7 + 9 * Next(random));
                var direction = new Vector3(Next(random), Next(random), Next(random));
                Equivalent(
                    mesh: mesh,
                    geometry: geometry,
                    origin: Permute(value: origin, axis: axis),
                    direction: Permute(value: direction * (i % 2 == 0 ? 0.25f : 4), axis: axis)
                );
                queries++;
            }
        }
        Boundaries();
        Snapshot();
        var fixture = Timber(sides: 8, sections: 8);
        var snapshot = new SpritsailMastGeometry(
            vertices: fixture.Vertices,
            triangles: fixture.Indices
        );
        var rays = MountRays();
        for (int i = 0; i < 100; i++)
            Query(mesh: fixture, geometry: snapshot, rays: rays, original: false);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
            Query(mesh: fixture, geometry: snapshot, rays: rays, original: false);
        Require(
            value: GC.GetAllocatedBytesForCurrentThread() == before,
            message: "Warmed mast queries must not allocate."
        );
        Console.WriteLine(
            $"PASS: {queries} mast rays exactly match the preceding scan; edge tolerances, nearest hits, misses, snapshot ownership and allocation-free queries checked."
        );
    }

    private static void Boundaries()
    {
        var mesh = (
            Vertices: new[] { new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(0, 1, 1) },
            Indices: new[] { 0, 1, 2 }
        );
        var geometry = new SpritsailMastGeometry(vertices: mesh.Vertices, triangles: mesh.Indices);
        foreach (
            float x in new[] { -2e-5f, -1e-5f, -5e-6f, 0, 0.5f, 1, 1.000005f, 1.00001f, 1.00002f }
        )
        foreach (float y in new[] { -2e-5f, -1e-5f, 0, 0.5f, 1 })
        foreach (float z in new[] { -1f, 0, 1, 2 })
        foreach (float speed in new[] { -1f, 0, 5e-11f, 1e-10f, 2e-10f, 0.5f, 2 })
            Equivalent(
                mesh: mesh,
                geometry: geometry,
                origin: new Vector3(x, y, z),
                direction: new Vector3(0, 0, speed)
            );
        Require(
            value: geometry.TryDistance(
                origin: new Vector3(0.25f, 0.25f, 0),
                direction: Vector3.forward * 2,
                distance: out var scaled
            )
                && scaled == 0.5f,
            message: "Ray length must remain unnormalized."
        );
        Require(
            value: !geometry.TryDistance(
                origin: new Vector3(0.25f, 0.25f, 1),
                direction: Vector3.forward,
                distance: out _
            ),
            message: "An origin on the surface must not count as a positive hit."
        );

        // Far face first, nearer reversed face second, degenerate face and a face behind the ray.
        var layered = (
            Vertices: new[]
            {
                new Vector3(0, 0, 3),
                new Vector3(1, 0, 3),
                new Vector3(0, 1, 3),
                new Vector3(0, 0, 1),
                new Vector3(1, 0, 1),
                new Vector3(0, 1, 1),
                new Vector3(0, 0, -1),
                new Vector3(1, 0, -1),
                new Vector3(0, 1, -1),
            },
            Indices: new[] { 0, 1, 2, 3, 5, 4, 3, 3, 3, 6, 7, 8 }
        );
        geometry = new SpritsailMastGeometry(
            vertices: layered.Vertices,
            triangles: layered.Indices
        );
        Require(
            value: geometry.TryDistance(
                origin: new Vector3(0.25f, 0.25f, 0),
                direction: Vector3.forward,
                distance: out var nearest
            )
                && nearest == 1,
            message: "Return the nearest positive two-sided hit, ignoring degenerate faces."
        );
        Equivalent(
            mesh: layered,
            geometry: geometry,
            origin: new Vector3(0.25f, 0.25f, 0),
            direction: Vector3.forward
        );
        geometry = new SpritsailMastGeometry(
            vertices: Array.Empty<Vector3>(),
            triangles: Array.Empty<int>()
        );
        Require(
            value: !geometry.TryDistance(
                origin: Vector3.zero,
                direction: Vector3.right,
                distance: out var missing
            ) && float.IsPositiveInfinity(missing),
            message: "An empty mesh must remain a miss for the caller's fallback."
        );
    }

    private static void Snapshot()
    {
        var mesh = Timber(sides: 4, sections: 2);
        var original = new SpritsailMastGeometry(vertices: mesh.Vertices, triangles: mesh.Indices);
        var origin = new Vector3(0, 0, 6);
        Require(
            value: original.TryDistance(
                origin: origin,
                direction: Vector3.right,
                distance: out var before
            ),
            message: "Snapshot fixture must hit."
        );
        for (int i = 0; i < mesh.Vertices.Length; i++)
        {
            mesh.Vertices[i].x *= 2;
            mesh.Vertices[i].y *= 2;
        }
        var replacement = new SpritsailMastGeometry(
            vertices: mesh.Vertices,
            triangles: mesh.Indices
        );
        Array.Clear(array: mesh.Indices, index: 0, length: mesh.Indices.Length);
        Array.Clear(array: mesh.Vertices, index: 0, length: mesh.Vertices.Length);
        Require(
            value: original.TryDistance(
                origin: origin,
                direction: Vector3.right,
                distance: out var retained
            )
                && retained == before,
            message: "The original snapshot must own its triangle data."
        );
        Require(
            value: replacement.TryDistance(
                origin: origin,
                direction: Vector3.right,
                distance: out var changed
            )
                && changed == 2 * before,
            message: "A replacement snapshot must use the new mesh without changing the old one."
        );
    }

    internal static void Benchmark()
    {
        var rays = MountRays();
        foreach (
            var size in new[]
            {
                (Sides: 4, Sections: 2),
                (Sides: 8, Sections: 8),
                (Sides: 32, Sections: 16),
            }
        )
        {
            var mesh = Timber(sides: size.Sides, sections: size.Sections);
            var geometry = new SpritsailMastGeometry(
                vertices: mesh.Vertices,
                triangles: mesh.Indices
            );
            foreach (var ray in rays)
                Equivalent(
                    mesh: mesh,
                    geometry: geometry,
                    origin: ray.Origin,
                    direction: ray.Direction
                );
            double checksum = 0;
            for (int i = 0; i < 100; i++)
            {
                checksum += Query(mesh: mesh, geometry: geometry, rays: rays, original: true);
                checksum += Query(mesh: mesh, geometry: geometry, rays: rays, original: false);
            }
            var previous = new double[7];
            var optimized = new double[7];
            double Measure(bool original)
            {
                long start = Stopwatch.GetTimestamp();
                for (int i = 0; i < 100; i++)
                    checksum += Query(
                        mesh: mesh,
                        geometry: geometry,
                        rays: rays,
                        original: original
                    );
                return (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency / 100;
            }
            for (int round = 0; round < 7; round++)
                if (round % 2 == 0)
                {
                    previous[round] = Measure(original: true);
                    optimized[round] = Measure(original: false);
                }
                else
                {
                    optimized[round] = Measure(original: false);
                    previous[round] = Measure(original: true);
                }
            Array.Sort(array: previous);
            Array.Sort(array: optimized);
            Console.WriteLine(
                $"Mast intersections: {mesh.Indices.Length / 3} triangles, 112 rays/batch; previous={previous[3]:F4} ms, optimized={optimized[3]:F4} ms, speedup={previous[3] / optimized[3]:F2}x; checksum={checksum:F3}"
            );
        }
    }

    private static double Query(
        (Vector3[] Vertices, int[] Indices) mesh,
        SpritsailMastGeometry geometry,
        (Vector3 Origin, Vector3 Direction)[] rays,
        bool original
    )
    {
        double sum = 0;
        foreach (var ray in rays)
        {
            float distance;
            bool hit = original
                ? MastReferenceGeometry.TryDistance(
                    vertices: mesh.Vertices,
                    triangles: mesh.Indices,
                    origin: ray.Origin,
                    direction: ray.Direction,
                    distance: out distance
                )
                : geometry.TryDistance(
                    origin: ray.Origin,
                    direction: ray.Direction,
                    distance: out distance
                );
            if (hit)
                sum += distance;
        }
        return sum;
    }

    private static void Equivalent(
        (Vector3[] Vertices, int[] Indices) mesh,
        SpritsailMastGeometry geometry,
        Vector3 origin,
        Vector3 direction
    )
    {
        bool expected = MastReferenceGeometry.TryDistance(
            vertices: mesh.Vertices,
            triangles: mesh.Indices,
            origin: origin,
            direction: direction,
            distance: out var expectedDistance
        );
        bool actual = geometry.TryDistance(
            origin: origin,
            direction: direction,
            distance: out var actualDistance
        );
        Require(
            value: actual == expected && actualDistance == expectedDistance,
            message: $"Mast ray changed: expected {expected}/{expectedDistance:R}, actual {actual}/{actualDistance:R}."
        );
    }

    private static (Vector3 Origin, Vector3 Direction)[] MountRays()
    {
        var rays = new (Vector3 Origin, Vector3 Direction)[112];
        for (int eye = 0; eye < 7; eye++)
        for (int sample = 0; sample < 16; sample++)
        {
            double angle = (sample + 0.17) * Math.PI / 8;
            rays[eye * 16 + sample] = (
                new Vector3(0.01f, -0.02f, 1 + eye * 2),
                new Vector3((float)Math.Cos(angle), (float)Math.Sin(angle), 0)
            );
        }
        return rays;
    }

    private static (Vector3[] Vertices, int[] Indices) Timber(
        int sides,
        int sections,
        int axis = 2,
        bool reverse = false
    )
    {
        var vertices = new Vector3[(sections + 1) * sides];
        var indices = new List<int>();
        for (int ring = 0; ring <= sections; ring++)
        for (int side = 0; side < sides; side++)
        {
            float height = 14f * ring / sections;
            float radius = 0.5f - 0.02f * height;
            double angle = 2 * Math.PI * side / sides;
            vertices[ring * sides + side] = Permute(
                value: new Vector3(
                    radius * (float)Math.Cos(angle),
                    0.8f * radius * (float)Math.Sin(angle),
                    height
                ),
                axis: axis
            );
            if (ring == sections)
                continue;
            int a = ring * sides + side;
            int b = ring * sides + (side + 1) % sides;
            indices.AddRange(collection: new[] { a, b, a + sides, b, b + sides, a + sides });
        }
        if (reverse)
            indices.Reverse();
        return (vertices, indices.ToArray());
    }

    private static Vector3 Permute(Vector3 value, int axis) =>
        axis == 0 ? new Vector3(value.z, value.x, value.y)
        : axis == 1 ? new Vector3(value.y, value.z, value.x)
        : value;

    private static float Next(Random random) => (float)(2 * random.NextDouble() - 1);

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
    }
}
