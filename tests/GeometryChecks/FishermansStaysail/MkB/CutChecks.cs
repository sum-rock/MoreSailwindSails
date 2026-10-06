using System;
using MoreSailwindSails.Sails.FishermansStaysail;
using MoreSailwindSails.Sails.FishermansStaysail.MkA;
using MoreSailwindSails.Sails.FishermansStaysail.MkB;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail.MkB;

internal static class CutChecks
{
    internal static void Run()
    {
        foreach (float width in new[] { 0.25f, 3f, 13.8f, 40f })
        foreach (float slope in new[] { 0f, 8.483f, 20f, 45f, 75f })
        {
            var mesh = FishermansStaysailMkBGeometry.Create(width, slope);
            var a = FishermansStaysailMkAGeometry.Create(width, slope).Corners;
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
                bool expected =
                    col == 0
                    || (
                        col == FishermansStaysailGeometry.Columns
                        && (row == 0 || row == FishermansStaysailGeometry.Rows)
                    );
                if ((mesh.Constraints[i].maxDistance == 0) != expected)
                    throw new Exception("Mk.B pin mask changed.");
                if (expected)
                    pinned++;
                var weights = mesh.Weights[i];
                var rest =
                    mesh.BonePositions[weights.boneIndex0] * weights.weight0
                    + mesh.BonePositions[weights.boneIndex1] * weights.weight1;
                Near((rest - mesh.Vertices[i]).magnitude, 0, width * 1e-5f, "skin");
            }
            if (pinned != FishermansStaysailGeometry.Rows + 3)
                throw new Exception("Mk.B pin count changed.");
            for (int i = 0; i < mesh.Triangles.Length; i += 3)
            {
                var p = mesh.Vertices[mesh.Triangles[i]];
                var q = mesh.Vertices[mesh.Triangles[i + 1]];
                var r = mesh.Vertices[mesh.Triangles[i + 2]];
                if (Vector3.Cross(q - p, r - p).y <= 0)
                    throw new Exception("Inverted Mk.B panel.");
            }
            var leechPoints = new Vector3[33];
            if (
                !FishermansStaysailTension.Fit(
                    c[3],
                    c[1],
                    c[2],
                    Vector3.zero,
                    (c[1] - c[3]).magnitude,
                    (c[3] - c[2]).magnitude,
                    1,
                    leechPoints
                )
            )
                throw new Exception("Mk.B nominal sheet and leech cannot fit.");
            var furledFore = FishermansStaysailReefingGeometry.Pose(c[2], c[0], c[1], 0, 0);
            var furledAft = FishermansStaysailReefingGeometry.Pose(c[3], c[0], c[1], 0, 0);
            if (furledFore.x <= c[2].x || furledAft.x <= c[3].x)
                throw new Exception("Mk.B foot does not gather toward the head.");
        }
        if (
            ReferenceEquals(
                FishermansStaysailMkBGeometry.Create(3).Vertices,
                FishermansStaysailMkBGeometry.Create(3).Vertices
            )
        )
            throw new Exception("Mk.B cuts share mutable vertices.");
        foreach (float bad in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, 101f })
        {
            try
            {
                FishermansStaysailMkBGeometry.Create(bad);
            }
            catch (ArgumentException)
            {
                continue;
            }
            throw new Exception("Mk.B accepted an invalid width.");
        }
        Console.WriteLine(
            "PASS: Mk.B shared stay head, level 90-degree foot, pins, skin, reef and nominal tension."
        );
    }

    private static void Near(float actual, float expected, float tolerance, string name)
    {
        if (float.IsNaN(actual) || Math.Abs(actual - expected) > tolerance)
            throw new Exception($"Mk.B {name}: {actual} expected {expected}.");
    }
}
