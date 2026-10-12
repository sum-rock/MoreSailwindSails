// Checks baked geometry ownership and fitted planar UVs against physical cut coordinates.
using System;
using System.Linq;
using MoreSailwindSails.Sails.FishermansStaysail;
using MoreSailwindSails.Sails.FishermansStaysail.MkA;
using MoreSailwindSails.Sails.FishermansStaysail.MkB;
using MoreSailwindSails.Sails.FishermansStaysail.MkC;
using UnityEngine;
using static MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail.MkA.CutChecks;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail;

internal static class BakedMeshChecks
{
    internal static void Run()
    {
        Check(
            cut: "A",
            bake: FishermansStaysailMkABakedMesh.Create,
            create: FishermansStaysailMkAGeometry.Create,
            luffRatio: 1,
            footSlope: -20
        );
        Check(
            cut: "B",
            bake: FishermansStaysailMkBBakedMesh.Create,
            create: FishermansStaysailMkBGeometry.Create,
            luffRatio: 1,
            footSlope: 0
        );
        Check(
            cut: "C",
            bake: FishermansStaysailMkCBakedMesh.Create,
            create: FishermansStaysailMkCGeometry.Create,
            luffRatio: 1.5f,
            footSlope: 40
        );
    }

    private static void Check(
        string cut,
        Func<FishermansStaysailMeshData> bake,
        Func<float, float, FishermansStaysailMeshData> create,
        float luffRatio,
        float footSlope
    )
    {
        var baked = bake();
        if (
            baked.Vertices.Length != 825
            || baked.UV.Length != 825
            || baked.Triangles.Length != 4608
        )
            throw new Exception($"Baked Mk.{cut} grid dimensions changed.");
        var nominal = create(13.8f, 20f);
        for (int i = 0; i < baked.Vertices.Length; i++)
        {
            Near(
                nominal.Vertices[i].x,
                baked.Vertices[i].x * 13.8f,
                0.00001f,
                "baked vertical position"
            );
            Near(
                nominal.Vertices[i].z,
                baked.Vertices[i].z * 13.8f,
                0.00001f,
                "baked span position"
            );
            Near((nominal.UV[i] - baked.UV[i]).magnitude, 0, 0.000002f, "authored nominal UV");
        }
        foreach (float width in new[] { 0.25f, 3f, 13.8f, 100f })
        foreach (float slope in new[] { 0f, 5f, 20f, 40f, 80f })
        {
            var fitted = create(width, slope);
            // Compare with the legacy analytical surface to guard fitting/physics compatibility.
            float rise = width * (float)Math.Tan(slope * Math.PI / 180);
            float footFore = -width * luffRatio;
            float footRise = width * (float)Math.Tan(footSlope * Math.PI / 180);
            for (int row = 0; row < 32; row++)
            for (int col = 0; col < 24; col++)
            {
                int i = row * 25 + col;
                int t = (row * 24 + col) * 6;
                var expected = new[] { i, i + 25, i + 1, i + 1, i + 25, i + 26 };
                if (!fitted.Triangles.Skip(t).Take(6).SequenceEqual(expected))
                    throw new Exception("Baking changed the legacy triangle diagonal or winding.");
            }
            for (int i = 0; i < fitted.Vertices.Length; i++)
            {
                float u = (i % 25) / 24f,
                    v = (i / 25) / 32f;
                var expected = new Vector3(
                    rise * u * (1 - v) + (footFore + footRise * u) * v,
                    FishermansStaysailGeometry.RestCamber(width: width, u: u, v: v),
                    -width * (1 - u)
                );
                Near(
                    (fitted.Vertices[i] - expected).magnitude,
                    0,
                    width * 0.000002f,
                    "legacy surface"
                );
                var uv = fitted.UV[i];
                if (
                    float.IsNaN(uv.x)
                    || float.IsNaN(uv.y)
                    || uv.x < 0.04999f
                    || uv.x > 0.95001f
                    || uv.y < 0.04999f
                    || uv.y > 0.95001f
                )
                    throw new Exception("Fitted UV escaped its margin.");
            }
            // Every triangle must have the same scale in both planar directions,
            // including sloping edges; rectangular UV stretching fails this check.
            float expectedScale =
                0.9f / Math.Max(width, rise - Math.Min(footFore, footFore + footRise));
            for (int t = 0; t < fitted.Triangles.Length; t += 3)
            for (int edge = 0; edge < 3; edge++)
            {
                int a = fitted.Triangles[t + edge],
                    b = fitted.Triangles[t + (edge + 1) % 3];
                var delta = fitted.Vertices[b] - fitted.Vertices[a];
                var uvDelta = fitted.UV[b] - fitted.UV[a];
                Near(uvDelta.x, delta.z * expectedScale, 0.000002f, "horizontal UV density");
                Near(uvDelta.y, delta.x * expectedScale, 0.000002f, "vertical UV density");
            }
            var other = create(width, slope);
            fitted.Vertices[0] = Vector3.one;
            fitted.UV[0] = Vector2.zero;
            fitted.Triangles[0] = 824;
            if (
                other.Vertices[0] == Vector3.one
                || other.UV[0] == Vector2.zero
                || other.Triangles[0] == 824
            )
                throw new Exception("Fitted instances share writable baked arrays.");
        }
        foreach (
            float bad in new[]
            {
                float.NaN,
                float.PositiveInfinity,
                float.NegativeInfinity,
                -1f,
                0f,
                0.249f,
                101f,
            }
        )
            Reject(action: () => create(bad, 20), label: "width");
        foreach (
            float bad in new[]
            {
                float.NaN,
                float.PositiveInfinity,
                float.NegativeInfinity,
                -1f,
                81f,
            }
        )
            Reject(action: () => create(13.8f, bad), label: "slope");
        var clean = bake();
        if (
            !clean.Vertices.SequenceEqual(baked.Vertices)
            || !clean.UV.SequenceEqual(baked.UV)
            || !clean.Triangles.SequenceEqual(baked.Triangles)
        )
            throw new Exception("Fitting mutated the baked source.");
        Console.WriteLine(
            $"PASS: baked Mk.{cut} topology, legacy surface, nominal authored UVs, proportional fitted UVs and independent arrays."
        );
    }

    private static void Reject(Action action, string label)
    {
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            return;
        }
        throw new Exception("Accepted invalid fitted " + label);
    }
}
