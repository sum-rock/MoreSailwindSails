// Checks exported stay channels, dimensions, winding and instance ownership without Unity rendering.
using System;
using System.Linq;
using MoreSailwindSails.Stays.FishermansStay;
using MoreSailwindSails.Stays.FishermansStay.Configuration.Boats;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

internal static class AuthoredMeshChecks
{
    internal static void Run()
    {
        var rope = Check(create: FishermansStayBakedMesh.CreateRope);
        var collar = Check(create: FishermansStayBakedMesh.CreateCollar);
        Near(actual: FishermansStayBakedMesh.RopeLength, expected: 1);
        Near(actual: FishermansStayBakedMesh.RopeRadius, expected: 0.01f);
        Near(actual: FishermansStayBakedMesh.CollarInnerRadius, expected: 0.2037f);
        Near(actual: FishermansStayBakedMesh.CollarOuterRadius, expected: 0.21f);
        Near(actual: FishermansStayBakedMesh.CollarHalfHeight, expected: 0.05f);
        foreach (float radius in new[] { 0.05f, 0.1f, 0.25f, 0.6f })
        {
            var scale = FishermansStayGeometry.CollarScale(radius: radius);
            Near(actual: FishermansStayBakedMesh.CollarInnerRadius * scale.x, expected: radius);
            Near(actual: scale.y, expected: scale.x);
            Near(actual: scale.z, expected: 1);
        }
        // Installed Dhow mizzen: visible shaft at the authored height is offset
        // from the origin and about 0.08183 m in radius; its capsule is 0.20 m.
        var dhow = DhowStays.Definition.Stays.Single().Variants[0];
        var seated = dhow.AftPoint + dhow.AftCollarOffset;
        Near(actual: seated.x, expected: 0.01019f);
        Near(actual: seated.y, expected: -0.00352f);
        Near(actual: seated.z, expected: 0.616f);
        var fitted = FishermansStayGeometry.CollarScale(radius: dhow.AftCollarRadius);
        Near(actual: FishermansStayBakedMesh.CollarInnerRadius * fitted.x, expected: 0.08083f);
        // Brig's fallback endpoint sits above the rendered tip (2.94751 m).
        // Seat the complete band on the wood without changing the rope endpoint.
        var brig = BrigStays.Definition.Stays[0].Variants[1];
        if (
            brig.ForePoint.z < 3.08f
            || brig.ForePoint.z + brig.ForeCollarOffset.z + FishermansStayBakedMesh.CollarHalfHeight
                > 2.94751f
        )
            throw new Exception(
                "Masthead collar floats above the wood or moved the stay endpoint."
            );
        Near(actual: rope.Vertices.Min(v => v.z), expected: -FishermansStayBakedMesh.RopeLength);
        Near(actual: rope.Vertices.Max(v => v.z), expected: 0);
        foreach (var vertex in rope.Vertices)
            Near(actual: Radius(vertex), expected: FishermansStayBakedMesh.RopeRadius);
        Near(
            actual: collar.Vertices.Min(Radius),
            expected: FishermansStayBakedMesh.CollarInnerRadius
        );
        Near(
            actual: collar.Vertices.Max(Radius),
            expected: FishermansStayBakedMesh.CollarOuterRadius
        );
        Near(
            actual: collar.Vertices.Min(v => v.z),
            expected: -FishermansStayBakedMesh.CollarHalfHeight
        );
        Near(
            actual: collar.Vertices.Max(v => v.z),
            expected: FishermansStayBakedMesh.CollarHalfHeight
        );
        Console.WriteLine(
            "PASS: authored stay dimensions, mesh channels, outward winding and independent arrays."
        );
    }

    private static float Radius(Vector3 vertex) =>
        (float)Math.Sqrt(vertex.x * vertex.x + vertex.y * vertex.y);

    private static FishermansStayMeshData Check(Func<FishermansStayMeshData> create)
    {
        var mesh = create();
        if (
            mesh.Vertices.Length == 0
            || mesh.Normals.Length != mesh.Vertices.Length
            || mesh.UV.Length != mesh.Vertices.Length
            || mesh.Triangles.Length == 0
            || mesh.Triangles.Length % 3 != 0
        )
            throw new Exception("Incomplete authored stay channels.");
        for (int i = 0; i < mesh.Vertices.Length; i++)
        {
            if (!Finite(mesh.Vertices[i].sqrMagnitude) || !Finite(mesh.UV[i].sqrMagnitude))
                throw new Exception("Non-finite authored stay channel.");
            Near(actual: mesh.Normals[i].magnitude, expected: 1);
        }
        double volume = 0;
        for (int i = 0; i < mesh.Triangles.Length; i += 3)
        {
            var indices = mesh.Triangles.Skip(i).Take(3).ToArray();
            if (indices.Any(index => index < 0 || index >= mesh.Vertices.Length))
                throw new Exception("Invalid authored stay index.");
            var a = mesh.Vertices[indices[0]];
            var b = mesh.Vertices[indices[1]];
            var c = mesh.Vertices[indices[2]];
            var normal = Vector3.Cross(b - a, c - a);
            if (
                normal.magnitude <= 1e-12f
                || indices.Any(index => Vector3.Dot(normal, mesh.Normals[index]) <= 0)
            )
                throw new Exception(
                    "Authored stay triangle is degenerate or disagrees with its normals."
                );
            volume += Vector3.Dot(a, Vector3.Cross(b, c)) / 6.0;
        }
        if (volume <= 0)
            throw new Exception("Authored stay faces point inward.");
        var other = create();
        if (
            ReferenceEquals(mesh.Vertices, other.Vertices)
            || ReferenceEquals(mesh.Normals, other.Normals)
            || ReferenceEquals(mesh.UV, other.UV)
            || ReferenceEquals(mesh.Triangles, other.Triangles)
        )
            throw new Exception("Authored stay instances share writable arrays.");
        other.Vertices[0] = Vector3.one;
        other.Normals[0] = Vector3.zero;
        other.UV[0] = new Vector2(-1, -1);
        other.Triangles[0] = -1;
        var fresh = create();
        if (
            !mesh.Vertices.SequenceEqual(fresh.Vertices)
            || !mesh.Normals.SequenceEqual(fresh.Normals)
            || !mesh.UV.SequenceEqual(fresh.UV)
            || !mesh.Triangles.SequenceEqual(fresh.Triangles)
        )
            throw new Exception("Authored stay factory data was mutated.");
        return mesh;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private static void Near(float actual, float expected)
    {
        if (!Finite(actual) || Math.Abs(actual - expected) > 0.000001f)
            throw new Exception($"Authored stay measurement: expected {expected}, got {actual}.");
    }
}
