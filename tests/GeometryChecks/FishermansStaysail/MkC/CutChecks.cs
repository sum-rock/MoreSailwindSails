using System;
using MoreSailwindSails.Sails.FishermansStaysail;
using MoreSailwindSails.Sails.FishermansStaysail.MkA;
using MoreSailwindSails.Sails.FishermansStaysail.MkC;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail.MkC;

internal static class CutChecks
{
    internal static void Run()
    {
        foreach (float width in new[] { 0.25f, 3f, 13.8f, 40f, 100f })
        foreach (float slope in new[] { 0f, 8.483f, 20f, 45f, 75f, 80f })
        {
            var mesh = FishermansStaysailMkCGeometry.Create(width, slope);
            var a = FishermansStaysailMkAGeometry.Create(width, slope).Corners;
            var c = mesh.Corners;
            Near((c[0] - a[0]).magnitude, 0, 1e-6f, "fore head");
            Near((c[1] - a[1]).magnitude, 0, 1e-6f, "aft head");
            Near(c[2].x, a[2].x * 1.5f, width * 1e-5f, "50% longer luff");
            Near(c[2].z, a[2].z, 1e-6f, "unchanged fore width");
            float footAngle = (float)(Math.Atan2(c[3].x - c[2].x, c[3].z - c[2].z) * 180 / Math.PI);
            Near(footAngle, 40, 1e-4f, "rising foot angle");
            Near((c[0] - c[2]).magnitude, width * 1.5f, width * 1e-5f, "luff length");
            float expectedLeech =
                width
                * (
                    1.5f
                    + (float)Math.Tan(slope * Math.PI / 180)
                    - (float)Math.Tan(40 * Math.PI / 180)
                );
            Near((c[1] - c[3]).magnitude, expectedLeech, width * 1e-5f, "leech");
            if (c[1].x <= c[3].x)
                throw new Exception("Mk.C leech must remain positive.");
            float centerU = (mesh.Center.z + width) / width;
            if (
                !float.IsFinite(mesh.Center.sqrMagnitude)
                || centerU <= 0
                || centerU >= 1
                || mesh.Center.x >= Vector3.Lerp(c[0], c[1], centerU).x
                || mesh.Center.x <= Vector3.Lerp(c[2], c[3], centerU).x
            )
                throw new Exception("Mk.C centroid is outside the nominal panel.");

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
                    throw new Exception("Mk.C pin mask changed.");
                if (expected)
                    pinned++;
                var weights = mesh.Weights[i];
                var rest =
                    mesh.BonePositions[weights.boneIndex0] * weights.weight0
                    + mesh.BonePositions[weights.boneIndex1] * weights.weight1;
                Near((rest - mesh.Vertices[i]).magnitude, 0, width * 1e-5f, "skin");
            }
            if (pinned != FishermansStaysailGeometry.Rows + 3)
                throw new Exception("Mk.C pin count changed.");
            float projectedArea = 0;
            for (int i = 0; i < mesh.Triangles.Length; i += 3)
            {
                var p = mesh.Vertices[mesh.Triangles[i]];
                var q = mesh.Vertices[mesh.Triangles[i + 1]];
                var r = mesh.Vertices[mesh.Triangles[i + 2]];
                float twiceArea = Vector3.Cross(q - p, r - p).y;
                projectedArea += twiceArea * 0.5f;
                if (twiceArea <= 0)
                    throw new Exception("Inverted Mk.C panel.");
            }
            Near(
                projectedArea,
                width * (width * 1.5f + expectedLeech) * 0.5f,
                width * width * 1e-4f,
                "projected trapezoid area"
            );
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
                throw new Exception("Mk.C nominal sheet and leech cannot fit.");
            var furledFore = FishermansStaysailReefingGeometry.Pose(c[2], c[0], c[1], 0, 0);
            var furledAft = FishermansStaysailReefingGeometry.Pose(c[3], c[0], c[1], 0, 0);
            if (furledFore.x <= c[2].x || furledAft.x <= c[3].x)
                throw new Exception("Mk.C foot does not gather toward the head.");
        }
        if (
            ReferenceEquals(
                FishermansStaysailMkCGeometry.Create(3).Vertices,
                FishermansStaysailMkCGeometry.Create(3).Vertices
            )
        )
            throw new Exception("Mk.C cuts share mutable vertices.");
        foreach (float bad in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, 101f })
        {
            try
            {
                FishermansStaysailMkCGeometry.Create(bad);
            }
            catch (ArgumentException)
            {
                continue;
            }
            throw new Exception("Mk.C accepted an invalid width.");
        }
        foreach (
            float bad in new[]
            {
                -1f,
                float.NaN,
                float.NegativeInfinity,
                float.PositiveInfinity,
                81f,
            }
        )
        {
            try
            {
                FishermansStaysailMkCGeometry.Create(3, bad);
            }
            catch (ArgumentException)
            {
                continue;
            }
            throw new Exception("Mk.C accepted an invalid head slope.");
        }
        var nominal = FishermansStaysailMkCGeometry.Create(3).Corners;
        Near(
            (float)(Math.Atan2(nominal[1].x - nominal[0].x, 3) * 180 / Math.PI),
            20,
            1e-4f,
            "nominal head angle"
        );
        float nominalLuff = -nominal[2].x;
        if (
            FishermansStaysailInstallationGeometry.FitError(3, nominalLuff, 0, 3.5f, 4)
                != "(LUFF EXCEEDS FORWARD MAST SECTION)"
            || FishermansStaysailInstallationGeometry.FitError(
                3,
                nominalLuff,
                0,
                nominalLuff + 0.1f,
                4
            ) != null
        )
            throw new Exception("Mk.C must respect its longer luff when checking mast clearance.");
        Console.WriteLine(
            "PASS: Mk.C shared stay head, 50% longer luff, rising 40-degree foot, pins, skin, reef and nominal tension."
        );
    }

    private static void Near(float actual, float expected, float tolerance, string name)
    {
        if (!float.IsFinite(actual) || Math.Abs(actual - expected) > tolerance)
            throw new Exception($"Mk.C {name}: {actual} expected {expected}.");
    }
}
