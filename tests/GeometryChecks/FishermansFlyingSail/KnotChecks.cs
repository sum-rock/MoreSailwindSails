using System;
using System.Linq;
using MoreSailwindSails.Sails.FishermansFlyingSail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansFlyingSail;

// Exercises native knot isolation and the optional-channel policy without Unity rendering.
internal static class KnotChecks
{
    internal static void Run()
    {
        CheckChannels();
        // Synthetic disconnected tube and compact attachment; no game mesh is
        // copied into the repository. Include an unused vertex near the knot.
        var vertices = new[]
        {
            new Vector3(0.02f, 0, 0),
            new Vector3(-0.02f, 0, 0),
            new Vector3(0, 0.02f, 1),
            new Vector3(0.08f, 0, 0),
            new Vector3(0, 0.08f, 0),
            new Vector3(0, 0, -0.08f),
            new Vector3(-0.08f, 0, 0),
            Vector3.zero,
        };
        var triangles = new[] { 0, 1, 2, 3, 4, 5, 4, 6, 5 };
        foreach (float scale in new[] { 0.1f, 1f, 10f })
        foreach (bool reversed in new[] { false, true })
        {
            var offset = new Vector3(9, -3, 18);
            var posed = vertices
                .Select(v =>
                    offset
                    + FishermansFlyingSailFrameGeometry.RotateAroundMast(
                        v * scale,
                        Vector3.zero,
                        new Vector3(1, 2, 3),
                        57
                    )
                )
                .ToArray();
            var ordered = reversed ? posed.Reverse().ToArray() : posed;
            var inputTriangles = triangles
                .Select(i => reversed ? vertices.Length - 1 - i : i)
                .ToArray();
            var originalTriangles = (int[])inputTriangles.Clone();
            var selected = FishermansFlyingSailKnotGeometry.Select(
                ordered,
                inputTriangles,
                offset,
                out var compact
            );
            Check(
                selected.Length == 4 && compact.Length == 6,
                "Knot extraction retained tube or unused vertices."
            );
            Check(
                selected.All(i =>
                {
                    int original = reversed ? vertices.Length - 1 - i : i;
                    return original >= 3 && original <= 6;
                }),
                "Knot extraction depends on source vertex order."
            );
            Check(
                inputTriangles.SequenceEqual(originalTriangles),
                "Extraction modified donor triangle indices."
            );
            for (int i = 0; i < compact.Length; i++)
                Check(
                    selected[compact[i]] == inputTriangles[i + 3],
                    "Compaction changed triangle winding or UV/normal mapping."
                );
            var normals = Enumerable
                .Range(0, vertices.Length)
                .Select(i => new Vector3(i, i + 1, i + 2))
                .ToArray();
            var uv = Enumerable
                .Range(0, vertices.Length)
                .Select(i => new Vector2(i / 10f, i / 20f))
                .ToArray();
            var orderedNormals = reversed ? normals.Reverse().ToArray() : normals;
            var orderedUV = reversed ? uv.Reverse().ToArray() : uv;
            var savedNormals = (Vector3[])orderedNormals.Clone();
            var savedUV = (Vector2[])orderedUV.Clone();
            var knotNormals = FishermansFlyingSailKnotChannels.Remap(
                channel: orderedNormals,
                selected: selected
            );
            var knotUV = FishermansFlyingSailKnotChannels.Remap(
                channel: orderedUV,
                selected: selected
            );
            for (int i = 0; i < selected.Length; i++)
            {
                int original = reversed ? vertices.Length - 1 - selected[i] : selected[i];
                Check(
                    knotNormals[i].Equals(normals[original]) && knotUV[i].Equals(uv[original]),
                    "Knot channel remapping lost the original vertex association."
                );
            }
            Check(
                orderedNormals.SequenceEqual(savedNormals) && orderedUV.SequenceEqual(savedUV),
                "Knot channel remapping modified its source arrays."
            );
        }
        Reject(vertices, new[] { 0, 1, 2 }); // Missing knot.
        Reject(vertices, new[] { 0, 1, 2, 2, 3, 4 }); // One connected section.
        Reject(vertices, new[] { 0, 1, 99 });
        Reject(vertices, new[] { 0, 1 });
        Reject(
            new[]
            {
                Vector3.zero,
                Vector3.one,
                Vector3.right,
                Vector3.one,
                Vector3.right,
                Vector3.up,
            },
            new[] { 0, 1, 2, 3, 4, 5 }
        ); // Equally large sections are ambiguous.
        var invalid = (Vector3[])vertices.Clone();
        invalid[3] = new Vector3(float.NaN, 0, 0);
        Reject(invalid, triangles);
        Console.WriteLine(
            "PASS: isolated knot selection, compact indices, preserved winding and channel mapping, UV-less native material policy and incompatible donor rejection."
        );
    }

    private static void CheckChannels()
    {
        // Counts match the installed asset, but these cases contain no game asset data.
        foreach (
            var test in new (
                int vertices,
                int normals,
                int uv,
                string shader,
                bool textures,
                bool? useUVs,
                string error
            )[]
            {
                (162, 162, 0, "Standard", false, false, null),
                (162, 162, 162, "Standard", false, true, null),
                (162, 162, 162, "Standard", true, true, null),
                (162, 162, 162, "Other", true, true, null),
                (162, 162, 0, "Standard", true, null, "UVs are missing"),
                (162, 162, 0, "Other", false, null, "UVs are missing"),
                (162, 162, 0, null, false, null, "UVs are missing"),
                (162, 0, 0, "Standard", false, null, "normal count"),
                (162, 161, 162, "Standard", false, null, "normal count"),
                (162, 163, 162, "Standard", false, null, "normal count"),
                (162, 162, 161, "Standard", false, null, "UV count"),
                (162, 162, 163, "Standard", false, null, "UV count"),
                (0, 0, 0, "Standard", false, null, "vertices are missing"),
            }
        )
        {
            bool useUVs;
            try
            {
                useUVs = FishermansFlyingSailKnotChannels.Validate(
                    vertexCount: test.vertices,
                    normalCount: test.normals,
                    uvCount: test.uv,
                    shaderName: test.shader,
                    hasAssignedTextures: test.textures
                );
            }
            catch (ArgumentException exception)
            {
                Check(
                    test.useUVs == null && exception.Message.Contains(test.error),
                    "Knot channel validation rejected a supported donor or misidentified the failure."
                );
                continue;
            }
            Check(
                test.useUVs.HasValue && useUVs == test.useUVs.Value,
                "Knot channel validation accepted an incompatible donor or chose the wrong UV policy."
            );
        }
    }

    private static void Reject(Vector3[] vertices, int[] triangles)
    {
        try
        {
            FishermansFlyingSailKnotGeometry.Select(vertices, triangles, Vector3.zero, out _);
        }
        catch (ArgumentException)
        {
            return;
        }
        throw new Exception(
            "Incompatible donor should omit its decoration instead of including the rope tube."
        );
    }

    private static void Check(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
    }
}
