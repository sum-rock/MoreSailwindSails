using System;
using MoreSailwindSails.Sails.FishermansStaysail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail.MkB;

internal static class CutChecks
{
    internal static void Run()
    {
        foreach (float width in new[] { 0.25f, 3f, 13.8f, 40f })
        foreach (float slope in new[] { 0f, 8.483f, 20f, 45f, 75f })
        {
            var mesh = BehaviorCases.CreateB(width, slope);
            var a = BehaviorCases.CreateA(width, slope).Corners;
            var c = mesh.Corners;
            Near((c[0] - a[0]).magnitude, 0, 1e-6f, "fore head");
            Near((c[1] - a[1]).magnitude, 0, 1e-6f, "aft head");
            Near((c[2] - a[2]).magnitude, 0, 1e-6f, "fore foot");
            Near(c[3].x, c[2].x, 1e-6f, "level foot");
            Near(Vector3.Dot(c[2] - c[0], c[3] - c[2]), 0, 1e-6f, "fore 90° corner");
            Near(Vector3.Dot(c[1] - c[3], c[2] - c[3]), 0, 1e-5f, "aft 90° corner");
            Near((c[1] - c[3]).magnitude, width + c[1].x, width * 1e-5f, "leech");
            float centerU = (mesh.Center.z + width) / width;
            if (
                !float.IsFinite(mesh.Center.sqrMagnitude)
                || centerU <= 0
                || centerU >= 1
                || mesh.Center.x >= Vector3.Lerp(c[0], c[1], centerU).x
                || mesh.Center.x <= Vector3.Lerp(c[2], c[3], centerU).x
            )
                throw new Exception("Mk.B centroid is outside the nominal panel.");

            int pinned = 0;
            for (int row = 0; row <= FishermansStaysailGeometry.Rows; row++)
            for (int col = 0; col <= FishermansStaysailGeometry.Columns; col++)
            {
                int i = row * (FishermansStaysailGeometry.Columns + 1) + col;
                if (
                    mesh.Constraints[i].maxDistance != 0
                    || mesh.Weights[i].weight0 != 1
                    || mesh.Weights[i].boneIndex0 != 0
                )
                    throw new Exception("Authored cloth must use a fixed identity skin.");
                pinned++;
            }
            if (pinned != mesh.Vertices.Length)
                throw new Exception("Mk.B pin count changed.");
            for (int i = 0; i < mesh.Triangles.Length; i += 3)
            {
                var p = mesh.Vertices[mesh.Triangles[i]];
                var q = mesh.Vertices[mesh.Triangles[i + 1]];
                var r = mesh.Vertices[mesh.Triangles[i + 2]];
                if (Vector3.Cross(q - p, r - p).y <= 0)
                    throw new Exception("Inverted Mk.B panel.");
            }
        }
        if (ReferenceEquals(BehaviorCases.CreateB(3).Vertices, BehaviorCases.CreateB(3).Vertices))
            throw new Exception("Mk.B cuts share mutable vertices.");
        foreach (float bad in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, 101f })
        {
            try
            {
                BehaviorCases.CreateB(bad);
            }
            catch (ArgumentException)
            {
                continue;
            }
            throw new Exception("Mk.B accepted an invalid width.");
        }
        Console.WriteLine(
            "PASS: Mk.B shared stay head, level 90-degree foot, identity skin and geometry."
        );
    }

    private static void Near(float actual, float expected, float tolerance, string name)
    {
        if (float.IsNaN(actual) || Math.Abs(actual - expected) > tolerance)
            throw new Exception($"Mk.B {name}: {actual} expected {expected}.");
    }
}
