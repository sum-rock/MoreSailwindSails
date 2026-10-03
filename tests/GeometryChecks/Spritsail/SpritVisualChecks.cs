using System;
using MoreSailwindSails.Sails.Spritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Checks the common sprit surface and mast-fitting clearance without instantiating Unity objects.
internal static class SpritVisualChecks
{
    internal static void Run()
    {
        var samples = new SpritsailSurfaceSample[7];
        for (int i = 0; i < samples.Length; i++)
        {
            Require(
                value: !samples[i]
                    .TryGet(origin: new Vector3(0, i, 0), direction: Vector3.right, radius: out _),
                message: "A new mast sample must not return an uninitialized radius."
            );
            samples[i]
                .Store(origin: new Vector3(0, i, 0), direction: Vector3.right, radius: i + 0.5f);
        }
        for (int frame = 0; frame < 3; frame++)
        for (int i = 0; i < samples.Length; i++)
            Require(
                value: samples[i]
                    .TryGet(
                        origin: new Vector3(0, i, 0),
                        direction: Vector3.right,
                        radius: out var radius
                    )
                    && radius == i + 0.5f,
                message: "Sequential tie queries must retain all seven cached radii across frames."
            );
        Require(
            value: !samples[3]
                .TryGet(origin: new Vector3(0, 3.01f, 0), direction: Vector3.right, radius: out _)
                && !samples[3]
                    .TryGet(origin: new Vector3(0, 3, 0), direction: Vector3.forward, radius: out _)
                && !samples[3]
                    .TryGet(
                        origin: new Vector3(0, 3, 0),
                        direction: Vector3.right * 2,
                        radius: out _
                    ),
            message: "Changed attachment position, direction or transformed scale must invalidate the sample."
        );
        SpritsailSpritGeometry.Spar(
            vertices: out var vertices,
            uv: out var uv,
            triangles: out var triangles
        );
        Require(value: vertices.Length == uv.Length, message: "Every spar vertex needs UVs.");
        for (int ring = 0; ring < 3; ring++)
        for (int side = 0; side <= 12; side++)
        {
            var v = vertices[ring * 13 + side];
            float radius =
                (float)Math.Sqrt(v.x * v.x + v.y * v.y)
                * SpritsailSpritGeometry.ThicknessMultiplier;
            float expected = 1.03f * (ring == 1 ? 1 : 0.85f);
            Require(
                value: Math.Abs(radius - expected) < 0.00001f,
                message: "Sprit diameter or end taper changed."
            );
        }
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i],
                b = triangles[i + 1],
                c = triangles[i + 2];
            var normal = Vector3
                .Cross(vertices[b] - vertices[a], vertices[c] - vertices[a])
                .normalized;
            Require(value: normal.sqrMagnitude > 0.99f, message: "Sprit triangle is degenerate.");
            if (a >= 65)
            {
                Require(
                    value: b >= 39 && c >= 39,
                    message: "Cap vertices must not share smoothed side normals."
                );
                Require(
                    value: Math.Abs(normal.z - (a == 65 ? -1 : 1)) < 0.00001f,
                    message: "Flat cap must face outward."
                );
            }
            else
            {
                var center = (vertices[a] + vertices[b] + vertices[c]) / 3;
                Require(
                    value: normal.x * center.x + normal.y * center.y > 0,
                    message: "Sprit side faces inward."
                );
            }
        }
        foreach (float backDepth in new[] { 1.1f, 2f, 4f })
        foreach (float mastRadius in new[] { 2f, 5f, 12f })
        {
            SpritsailSnotterGeometry.Create(
                backDepth: backDepth,
                mastRadius: mastRadius,
                vertices: out vertices,
                uv: out uv,
                wood: out var wood,
                iron: out var iron
            );
            Require(
                value: wood.Length > 0 && iron.Length > 0 && vertices.Length == uv.Length,
                message: "Curved timber and ironwork require separate complete submeshes."
            );
            foreach (int index in wood)
                Require(
                    value: vertices[index].z >= -backDepth - 0.0001f,
                    message: "Wooden back must seat on the mast tangent plane."
                );
            foreach (var indices in new[] { wood, iron })
                for (int i = 0; i < indices.Length; i += 3)
                    Require(
                        value: Vector3
                            .Cross(
                                vertices[indices[i + 1]] - vertices[indices[i]],
                                vertices[indices[i + 2]] - vertices[indices[i]]
                            )
                            .sqrMagnitude > 1e-10f,
                        message: "Curved fitting must have non-degenerate surfaces."
                    );
        }
        // A tapered square timber: a larger collision capsule must not determine seating.
        var taper = new[]
        {
            new Vector3(-1, -1, 0),
            new Vector3(1, -1, 0),
            new Vector3(1, 1, 0),
            new Vector3(-1, 1, 0),
            new Vector3(-0.5f, -0.5f, 2),
            new Vector3(0.5f, -0.5f, 2),
            new Vector3(0.5f, 0.5f, 2),
            new Vector3(-0.5f, 0.5f, 2),
        };
        var faces = new[]
        {
            0,
            1,
            4,
            1,
            5,
            4,
            1,
            2,
            5,
            2,
            6,
            5,
            2,
            3,
            6,
            3,
            7,
            6,
            3,
            0,
            7,
            0,
            4,
            7,
        };
        foreach (float height in new[] { 0.1f, 1f, 1.9f })
        foreach (float directionScale in new[] { 0.5f, 1f, 2f })
        {
            Require(
                value: SpritsailMastGeometry.TryDistance(
                    vertices: taper,
                    triangles: faces,
                    origin: new Vector3(0, 0, height),
                    direction: Vector3.right * directionScale,
                    distance: out var distance
                ),
                message: "Rendered mast ray must hit the timber."
            );
            Require(
                value: Math.Abs(distance - (1 - height * 0.25f) / directionScale) < 1e-5f,
                message: "Seating must follow rendered taper and transformed ray scale."
            );
        }
        Console.WriteLine(
            "PASS: shared sprit 3% thickness, 85% blunt ends, cap separation/winding and curved wood/iron submeshes and rendered mast seating."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
    }
}
