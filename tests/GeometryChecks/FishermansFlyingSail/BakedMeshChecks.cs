// Verifies the authored cut retains the analytical rest surface, topology and proportional UVs.
using System;
using System.Linq;
using MoreSailwindSails.Sails.FishermansFlyingSail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansFlyingSail;

internal static class BakedMeshChecks
{
    internal static void Run()
    {
        var baked = FishermansFlyingSailBakedMesh.Create();
        if (
            baked.Vertices.Length != 825
            || baked.UV.Length != 825
            || baked.Triangles.Length != 4608
        )
            throw new Exception("Flying Sail baked grid changed.");
        float slope = (float)Math.Tan(20 * Math.PI / 180);
        float uvScale = 0.9f / (2 + 2 * slope);
        foreach (float width in new[] { 0.25f, 3.80f, 3.81f, 3.82f, 4.6f, 13.8f, 100f })
        {
            var data = FishermansFlyingSailGeometry.Create(width: width);
            if (!data.UV.SequenceEqual(baked.UV) || !data.Triangles.SequenceEqual(baked.Triangles))
                throw new Exception("Flying Sail must retain baked UVs and topology.");
            for (int row = 0; row <= 32; row++)
            for (int col = 0; col <= 24; col++)
            {
                int i = row * 25 + col;
                float u = col / 24f,
                    v = row / 32f;
                float flatHeight = slope * u * (1 - v) + (-2 - slope * u) * v;
                var expected = new Vector3(
                    width * flatHeight,
                    FishermansFlyingSailGeometry.RestCamber(width: width, u: u, v: v),
                    -width * (1 - u)
                        - FishermansFlyingSailGeometry.RestLuffBow(width: width, u: u, v: v)
                );
                Near(
                    value: (data.Vertices[i] - expected).magnitude,
                    tolerance: width * 0.000002f,
                    label: "analytical rest surface"
                );
                var expectedUV = new Vector2(
                    (1 - uvScale) / 2 + u * uvScale,
                    0.05f + (flatHeight + 2 + slope) * uvScale
                );
                Near(
                    value: (data.UV[i] - expectedUV).magnitude,
                    tolerance: 0.000002f,
                    label: "proportional flat UVs"
                );
                if (row == 32 || col == 24)
                    continue;
                int t = (row * 24 + col) * 6;
                if (
                    !data
                        .Triangles.Skip(t)
                        .Take(6)
                        .SequenceEqual(new[] { i, i + 25, i + 1, i + 1, i + 25, i + 26 })
                )
                    throw new Exception("Flying Sail triangle diagonal or winding changed.");
            }
            Near(
                value: -data.Vertices[400].z - width - Math.Min(width * 0.06f, 0.2286f),
                tolerance: width * 0.000002f,
                label: "mastward bow cap"
            );
            var other = FishermansFlyingSailGeometry.Create(width: width);
            data.Vertices[0] = Vector3.one;
            data.UV[0] = Vector2.zero;
            data.Triangles[0] = 824;
            if (
                other.Vertices[0] == Vector3.one
                || other.UV[0] == Vector2.zero
                || other.Triangles[0] == 824
            )
                throw new Exception("Flying Sail instances share writable mesh arrays.");
        }
        var clean = FishermansFlyingSailBakedMesh.Create();
        if (
            !clean.Vertices.SequenceEqual(baked.Vertices)
            || !clean.UV.SequenceEqual(baked.UV)
            || !clean.Triangles.SequenceEqual(baked.Triangles)
        )
            throw new Exception("Flying Sail fitting mutated the baked source.");
        Console.WriteLine(
            "PASS: Flying Sail bake preserves analytical surface, bow cap, planar UVs, topology and array ownership."
        );
    }

    private static void Near(float value, float tolerance, string label)
    {
        if (!float.IsFinite(value) || Math.Abs(value) > tolerance)
            throw new Exception(label + ": " + value);
    }
}
