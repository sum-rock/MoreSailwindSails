using System;
using MoreSailwindSails.Sails.FishermansFlyingSail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansFlyingSail;

internal static class MeshChecks
{
    internal static void Run()
    {
        foreach (float width in new[] { 0.25f, 3.80f, 3.81f, 3.82f, 4.6f, 6f, 13.8f, 40f, 100f })
        {
            CheckSail(width);
        }
        Require(
            FishermansFlyingSailGeometry.RenderState(0) == 0
                && FishermansFlyingSailGeometry.RenderState(0.02f) == 0,
            "A fully struck sail must select the hidden state."
        );
        Require(
            FishermansFlyingSailGeometry.RenderState(0.03f) == 1
                && FishermansFlyingSailGeometry.RenderState(0.5f) == 1,
            "Partly furled sails must use the procedural renderer without cloth simulation."
        );
        Require(
            FishermansFlyingSailGeometry.RenderState(0.98f) == 2
                && FishermansFlyingSailGeometry.RenderState(1) == 2,
            "Only fully set sails should use the cloth renderer."
        );
        foreach (float width in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, 101f })
        {
            try
            {
                FishermansFlyingSailGeometry.Create(width);
            }
            catch (ArgumentException)
            {
                continue;
            }
            throw new Exception("Invalid width accepted.");
        }
        Console.WriteLine(
            "PASS: four-corner outline, revised cut, circular full-height camber, spare head/foot cloth, area, winding, UVs, bone weights, cloth pins, render states, and independent mesh arrays."
        );
    }

    private static void CheckSail(float width)
    {
        var d = FishermansFlyingSailGeometry.Create(width);
        var second = FishermansFlyingSailGeometry.Create(width);
        Require(
            !ReferenceEquals(d.Vertices, second.Vertices)
                && !ReferenceEquals(d.Constraints, second.Constraints),
            "Sails share mutable geometry."
        );
        var c = d.Corners;
        Angle(c[2], c[0], c[1], 110);
        Angle(c[0], c[1], c[3], 70);
        Angle(c[0], c[2], c[3], 110);
        Angle(c[2], c[3], c[1], 70);
        Require(
            Math.Abs((c[2] - c[0]).magnitude / width - 2) < 1e-5,
            "The luff must be twice the fabric span."
        );
        Require(
            Math.Abs((c[1] - c[0]).magnitude - (c[3] - c[2]).magnitude) < width * 1e-5,
            "Isosceles trapezoid must have equal sloping edges."
        );
        Require(
            Math.Abs((c[1] - c[3]).magnitude / width - 2.7279405f) < 1e-5,
            "The longer aft edge has the wrong length."
        );
        Require(
            c[3].x < c[2].x && c[1].x > c[0].x,
            "The head must rise and the foot fall toward the aft mast."
        );
        double area = 0,
            projectedArea = 0;
        var centroid = Vector3.zero;
        for (int i = 0; i < d.Triangles.Length; i += 3)
        {
            var a = d.Vertices[d.Triangles[i]];
            var b = d.Vertices[d.Triangles[i + 1]];
            var e = d.Vertices[d.Triangles[i + 2]];
            var cross = Vector3.Cross(b - a, e - a);
            Require(cross.y > 0, "A triangle collapsed or inverted.");
            area += cross.magnitude * 0.5;
            projectedArea += cross.y * 0.5;
            centroid += (a + b + e) * (cross.magnitude / 6);
        }
        double expected = width * ((c[0] - c[2]).magnitude + (c[1] - c[3]).magnitude) / 2;
        Require(
            Math.Abs(expected / (width * width) - (2 + Math.Tan(20 * Math.PI / 180))) < 1e-5,
            "The nominal trapezoid area is wrong."
        );
        // The inward luff arc adds a narrow strip to the projected fabric area.
        for (int row = 1; row <= FishermansFlyingSailGeometry.Rows; row++)
            expected +=
                width
                / FishermansFlyingSailGeometry.Rows
                * (
                    FishermansFlyingSailGeometry.RestLuffBow(
                        width,
                        0,
                        (float)(row - 1) / FishermansFlyingSailGeometry.Rows
                    )
                    + FishermansFlyingSailGeometry.RestLuffBow(
                        width,
                        0,
                        (float)row / FishermansFlyingSailGeometry.Rows
                    )
                );
        Require(Math.Abs(projectedArea / expected - 1) < 1e-5, "Projected cut area is wrong.");
        Require(
            area > expected * 1.07 && area < expected * 1.12,
            "Camber must add actual cloth area."
        );
        Require(
            (centroid / (float)area - d.Center).magnitude < width * 1e-5f,
            "Wind center must use the revised surface centroid."
        );
        double topLength = 0;
        for (int col = 1; col <= FishermansFlyingSailGeometry.Columns; col++)
            topLength += (d.Vertices[col] - d.Vertices[col - 1]).magnitude;
        Require(
            topLength > (c[1] - c[0]).magnitude * 1.08
                && topLength < (c[1] - c[0]).magnitude * 1.10,
            "The head needs spare cloth between its fixed endpoints."
        );
        Require(
            Math.Abs(d.Vertices[FishermansFlyingSailGeometry.Columns / 2].y - width * 0.20f)
                < width * 1e-5f,
            "Camber must peak at twenty percent of width."
        );
        int stride = FishermansFlyingSailGeometry.Columns + 1;
        double footLength = 0;
        for (int col = 1; col < stride; col++)
            footLength += (
                d.Vertices[FishermansFlyingSailGeometry.Rows * stride + col]
                - d.Vertices[FishermansFlyingSailGeometry.Rows * stride + col - 1]
            ).magnitude;
        Require(
            Math.Abs(footLength - topLength) < width * 1e-5,
            "The foot needs the same spare fabric as the head."
        );
        // Each shaping station lies on one circle through the head, middle and foot.
        for (int row = 0; row <= FishermansFlyingSailGeometry.Rows; row++)
        for (
            int col = 0;
            col <= FishermansFlyingSailGeometry.Columns;
            col += FishermansFlyingSailGeometry.ShapeStride
        )
        {
            var point = d.Vertices[row * stride + col];
            float across = (float)col / FishermansFlyingSailGeometry.Columns - 0.5f;
            float height = point.y / width + 0.525f;
            Require(
                Math.Abs(across * across + height * height - 0.725f * 0.725f) < 1e-6,
                "The full-height billow must follow a circular section."
            );
        }
        double luffLength = 0;
        for (int row = 1; row <= FishermansFlyingSailGeometry.Rows; row++)
            luffLength += (
                d.Vertices[row * (FishermansFlyingSailGeometry.Columns + 1)]
                - d.Vertices[(row - 1) * (FishermansFlyingSailGeometry.Columns + 1)]
            ).magnitude;
        double luffChord = (c[2] - c[0]).magnitude;
        Require(
            luffLength > luffChord && luffLength < luffChord * 1.004,
            "Luff needs a small amount of actual extra cloth length, not just movement permission."
        );
        var boneSeen = new bool[FishermansFlyingSailGeometry.BoneCount];
        for (int row = 0; row <= FishermansFlyingSailGeometry.Rows; row++)
        for (int col = 0; col <= FishermansFlyingSailGeometry.ShapeColumns; col++)
        {
            int bone = FishermansFlyingSailGeometry.ShapeBone(row, col);
            Require(
                bone >= 0 && bone < boneSeen.Length && !boneSeen[bone],
                "Invalid or duplicate shaping bone index."
            );
            boneSeen[bone] = true;
        }
        Require(Array.TrueForAll(boneSeen, b => b), "Uninitialized shaping bone.");
        Require(
            d.Constraints[FishermansFlyingSailGeometry.Columns / 2].maxDistance < width * 0.12f,
            "Top travel must keep the loaded peak on the target side."
        );
        int pins = 0;
        for (int i = 0; i < d.Vertices.Length; i++)
        {
            var w = d.Weights[i];
            Require(
                w.weight0 >= w.weight1 && w.weight1 >= w.weight2 && w.weight2 >= w.weight3,
                "Unity skin weights must be sorted largest first."
            );
            Require(w.weight0 > 0, "A vertex must start with a nonzero influence.");
            Require(
                w.weight0 >= 0 && w.weight1 >= 0 && w.weight2 >= 0 && w.weight3 >= 0,
                "Negative skin weight."
            );
            Require(
                Math.Abs(w.weight0 + w.weight1 + w.weight2 + w.weight3 - 1) < 1e-5,
                "Skin weights do not sum to one."
            );
            var reconstructed =
                d.BonePositions[w.boneIndex0] * w.weight0
                + d.BonePositions[w.boneIndex1] * w.weight1
                + d.BonePositions[w.boneIndex2] * w.weight2
                + d.BonePositions[w.boneIndex3] * w.weight3;
            Require(
                (reconstructed - d.Vertices[i]).magnitude < width * 1e-5f,
                "Shaping bones must reconstruct the whole rest mesh without a one-sided residual."
            );
            var uv = d.UV[i];
            Require(uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1, "Invalid UV.");
            bool pinned = d.Constraints[i].maxDistance == 0;
            if (pinned)
                pins++;
            Require(
                pinned
                    == (
                        i == 0
                        || i == FishermansFlyingSailGeometry.Columns
                        || i
                            == FishermansFlyingSailGeometry.Rows
                                * (FishermansFlyingSailGeometry.Columns + 1)
                        || i == d.Vertices.Length - 1
                    ),
                "Wrong cloth attachment."
            );
        }
        Require(pins == 4, "Only the four sail corners should be pinned.");
        for (int row = 1; row < FishermansFlyingSailGeometry.Rows; row++)
        {
            int fore = row * (FishermansFlyingSailGeometry.Columns + 1);
            float travel = d.Constraints[fore].maxDistance;
            Require(
                travel > 0 && travel <= width * 0.04001f,
                "Intermediate luff vertices must be free with a modest travel limit."
            );
            int mirrored =
                (FishermansFlyingSailGeometry.Rows - row)
                * (FishermansFlyingSailGeometry.Columns + 1);
            Require(
                Math.Abs(travel - d.Constraints[mirrored].maxDistance) < width * 1e-6f,
                "Luff travel must taper symmetrically toward its fixed corners."
            );
            Require(
                travel <= FishermansFlyingSailFrameGeometry.TieLength * 0.25f + 1e-6f,
                "Luff travel must leave room for the inward arch and mast clearance."
            );
            int i =
                row * (FishermansFlyingSailGeometry.Columns + 1)
                + FishermansFlyingSailGeometry.Columns;
            Require(
                d.Constraints[i].maxDistance > 0
                    && d.Constraints[i].maxDistance <= width * 0.06001f,
                "Intermediate leech vertices need bounded movement away from their skin targets."
            );
        }
        Require(
            Math.Abs(
                d.Constraints[
                    (FishermansFlyingSailGeometry.Rows / 2)
                        * (FishermansFlyingSailGeometry.Columns + 1)
                ].maxDistance
                    - Math.Min(width * 0.04f, FishermansFlyingSailFrameGeometry.TieLength * 0.25f)
            )
                < width * 1e-6f,
            "Luff flex must respect the fixed mast gap at every width."
        );
        float leechMiddle = d.Constraints[
            (FishermansFlyingSailGeometry.Rows / 2) * (FishermansFlyingSailGeometry.Columns + 1)
                + FishermansFlyingSailGeometry.Columns
        ].maxDistance;
        Require(
            Math.Abs(leechMiddle - width * 0.06f) < width * 1e-6f,
            "Mid-leech must have six percent of width to flex naturally."
        );
        float nearClew = d.Constraints[
            (FishermansFlyingSailGeometry.Rows - 1) * (FishermansFlyingSailGeometry.Columns + 1)
                + FishermansFlyingSailGeometry.Columns
        ].maxDistance;
        Require(
            nearClew > 0 && nearClew < width * 0.002f,
            "The free leech must retain reinforcement beside the clew."
        );
        Require(
            d.Center.x < 0 && d.Center.z > -width && d.Center.z < 0,
            "Wind center lies outside the sail."
        );
    }

    private static void Angle(Vector3 a, Vector3 b, Vector3 c, double expected)
    {
        var u = (a - b).normalized;
        var v = (c - b).normalized;
        double actual = Math.Acos(Math.Max(-1, Math.Min(1, Vector3.Dot(u, v)))) * 180 / Math.PI;
        Require(Math.Abs(actual - expected) < 0.001, "Wrong corner angle: " + actual);
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
    }
}
