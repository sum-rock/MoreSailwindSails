using System;
using MoreSailwindSails.Sails.Spritsail;
using MoreSailwindSails.Sails.Spritsail.BoomedSpritsail;
using UnityEngine;
using BoomedA = MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.MkA.BoomedSpritsailMkA;
using BoomedB = MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.MkB.BoomedSpritsailMkB;
using LooseA = MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.MkA.LooseFootedSpritsailMkA;
using LooseB = MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.MkB.LooseFootedSpritsailMkB;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail.BoomedSpritsail;

// Exercises companion cuts, fixed spar pivots, boom-attached skin and reversible reefing without Unity physics.
internal static class GeometryChecks
{
    internal static void Run()
    {
        foreach (var definition in new[] { BoomedA.Definition, BoomedB.Definition })
        foreach (float width in new[] { 0.25f, 1f, 4f, 12f })
        {
            var mesh = BoomedSpritsailGeometry.Create(width: width, definition: definition);
            var companion = (
                definition == BoomedA.Definition ? LooseA.Definition : LooseB.Definition
            ).Corners(width: width);
            for (int i = 0; i < 4; i++)
                Near(
                    actual: mesh.Corners[i],
                    expected: companion[i],
                    epsilon: 1e-5f,
                    message: "Boomed companions must retain their loose-footed mark's deployed cut."
                );
            CheckSkin(mesh: mesh, width: width);
            foreach (
                var scale in new[]
                {
                    Vector3.one,
                    new Vector3(0.5f, 1.2f, 1.4f),
                    new Vector3(2, 2, 2),
                    new Vector3(3, 1, 0.4f),
                }
            )
            foreach (float height in new[] { 0f, 10f })
                CheckDeployment(
                    corners: Array.ConvertAll(
                        mesh.Corners,
                        point => Vector3.Scale(point, scale) + Vector3.right * height
                    )
                );
        }
        foreach (
            float unroll in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f }
        )
        {
            var pose = BoomedSpritsailDeployment.Evaluate(
                corners: BoomedA.Definition.Corners(width: 1),
                unroll: unroll
            );
            Require(
                value: SpritsailDeployment.ExposedArea(pose: pose) < 1e-5f,
                message: "Invalid deployment must fall back to a finite struck pose."
            );
        }
        string line = new string('x', 60) + " Boomed Spritsail Mk.B -> changed order";
        Require(
            value: BoomedSpritsailOrderText.NeedsWrapping(line: line),
            message: "Long boomed order lines require iterative wrapping."
        );
        foreach (var wrapped in BoomedSpritsailOrderText.Wrap(line: line))
            Require(
                value: wrapped.Length <= 45,
                message: "Wrapped order lines must be safe for NANDFixes."
            );
        Console.WriteLine(
            "PASS: both boomed companion cuts, pinned straight feet, skin weights, rigid spars, fixed pivots, edge budgets, reef reversals, scaling, both tacks, exposed area and order wrapping."
        );
    }

    private static void CheckSkin(BoomedSpritsailMeshData mesh, float width)
    {
        for (int row = 0; row <= BoomedSpritsailGeometry.Rows; row++)
        for (int col = 0; col <= BoomedSpritsailGeometry.Columns; col++)
        {
            int index = row * (BoomedSpritsailGeometry.Columns + 1) + col;
            var weights = mesh.Weights[index];
            Require(
                value: weights.weight0 >= weights.weight1
                    && Math.Abs(weights.weight0 + weights.weight1 - 1) < 1e-6f
                    && weights.boneIndex0 >= 0
                    && weights.boneIndex0 < mesh.BonePositions.Length
                    && weights.boneIndex1 >= 0
                    && weights.boneIndex1 < mesh.BonePositions.Length,
                message: "Skin influences must be valid, normalized and ordered."
            );
            Near(
                actual: mesh.BonePositions[weights.boneIndex0] * weights.weight0
                    + mesh.BonePositions[weights.boneIndex1] * weights.weight1,
                expected: mesh.Vertices[index],
                epsilon: width * 1e-5f,
                message: "Rest skin must reconstruct every mesh vertex without a topology jump."
            );
            bool pinned =
                row == BoomedSpritsailGeometry.Rows
                || col == 0
                || (row == 0 && col == BoomedSpritsailGeometry.Columns);
            Require(
                value: (mesh.Constraints[index].maxDistance == 0) == pinned,
                message: "Pin the full foot/luff and peak while retaining panel solver travel."
            );
            if (row == BoomedSpritsailGeometry.Rows)
                Near(
                    actual: mesh.Vertices[index],
                    expected: Vector3.Lerp(
                        a: mesh.Corners[2],
                        b: mesh.Corners[3],
                        t: col / (float)BoomedSpritsailGeometry.Columns
                    ),
                    epsilon: width * 1e-5f,
                    message: "The foot must lie on the rigid boom."
                );
        }
        var full = BoomedSpritsailDeployment.Evaluate(corners: mesh.Corners, unroll: 1);
        for (int row = 0; row <= BoomedSpritsailGeometry.Rows; row++)
        for (int col = 0; col <= BoomedSpritsailGeometry.ShapeColumns; col++)
            Near(
                actual: BoomedSpritsailGathering.Point(
                    corners: mesh.Corners,
                    pose: full,
                    amount: 1,
                    u: col / (float)BoomedSpritsailGeometry.ShapeColumns,
                    v: row / (float)BoomedSpritsailGeometry.Rows
                ),
                expected: mesh.BonePositions[
                    BoomedSpritsailGeometry.ShapeBone(row: row, column: col)
                ],
                epsilon: width * 1e-5f,
                message: "Fully deployed pose must match initialized skin."
            );
    }

    private static void CheckDeployment(Vector3[] corners)
    {
        var full = BoomedSpritsailDeployment.Evaluate(corners: corners, unroll: 1);
        float boomLength = (corners[3] - corners[2]).magnitude;
        float sparLength = (full.Tip - full.Heel).magnitude;
        float epsilon = Math.Max(boomLength, sparLength) * 3e-5f;
        float previousArea = 0;
        for (int frame = 0; frame <= 100; frame++)
        {
            float unroll = frame / 100f;
            float amount = SpritsailDeployment.Amount(unroll: unroll);
            var pose = BoomedSpritsailDeployment.Evaluate(corners: corners, unroll: unroll);
            Near(
                actual: pose.Tack,
                expected: corners[2],
                epsilon: epsilon,
                message: "Boom pivot must stay at the tack."
            );
            Near(
                actual: pose.Heel,
                expected: full.Heel,
                epsilon: epsilon,
                message: "Sprit pocket must remain fixed."
            );
            Near(
                actual: pose.Throat,
                expected: corners[0],
                epsilon: epsilon,
                message: "Throat must remain fixed."
            );
            Require(
                value: Math.Abs((pose.Clew - pose.Tack).magnitude - boomLength) < epsilon
                    && Math.Abs((pose.Tip - pose.Heel).magnitude - sparLength) < epsilon,
                message: "Both spars must remain rigid during deployment."
            );
            Near(
                actual: pose.Tip - pose.Peak,
                expected: full.Tip - full.Peak,
                epsilon: epsilon,
                message: "Peak must stay lashed alongside the sprit."
            );
            Require(
                value: (pose.Peak - pose.Clew).magnitude
                    <= (corners[1] - corners[3]).magnitude + epsilon
                    && (pose.Peak - pose.Throat).magnitude
                        <= (corners[1] - corners[0]).magnitude + epsilon,
                message: "Coordinated spar rotation must not stretch the leech or head."
            );
            float area = SpritsailDeployment.ExposedArea(pose: pose);
            Require(
                value: area + epsilon >= previousArea,
                message: "Exposed area must increase monotonically with deployment."
            );
            previousArea = area;
            if (frame == 0)
            {
                Near(
                    actual: pose.Clew - pose.Tack,
                    expected: Vector3.right * boomLength,
                    epsilon: epsilon,
                    message: "Struck boom must stand along the mast."
                );
                Near(
                    actual: pose.Tip - pose.Heel,
                    expected: Vector3.right * sparLength,
                    epsilon: epsilon,
                    message: "Struck sprit must stand along the mast."
                );
                Require(
                    value: area < epsilon,
                    message: "A struck sail must have zero projected area."
                );
            }
            foreach (float load in new[] { -1f, 0f, 1f })
            foreach (float obstruction in new[] { 0f, 1f })
                for (int column = 0; column <= 12; column++)
                {
                    float u = column / 12f;
                    Near(
                        actual: BoomedSpritsailGathering.Point(
                            corners: corners,
                            pose: pose,
                            amount: amount,
                            u: u,
                            v: 1,
                            camber: load,
                            obstruction: obstruction
                        ),
                        expected: Vector3.Lerp(a: pose.Tack, b: pose.Clew, t: u),
                        epsilon: epsilon,
                        message: "Tack, clew and all foot bones must follow the boom under reefing and either tack load."
                    );
                    Near(
                        actual: BoomedSpritsailGathering.Point(
                            corners: corners,
                            pose: pose,
                            amount: amount,
                            u: 0,
                            v: u,
                            camber: load,
                            obstruction: obstruction
                        ),
                        expected: Vector3.Lerp(a: pose.Throat, b: pose.Tack, t: u),
                        epsilon: epsilon,
                        message: "The entire luff must remain on the mast."
                    );
                }
            foreach (float angle in new[] { -89f, -40f, 0f, 40f, 89f })
            {
                Vector3 Rotate(Vector3 point) =>
                    BoomedSpritsailFrameGeometry.RotateAroundMast(
                        point: point,
                        pivot: corners[0],
                        axis: Vector3.right,
                        degrees: angle
                    );
                Require(
                    value: Math.Abs(
                        (Rotate(point: pose.Clew) - Rotate(point: pose.Tack)).magnitude - boomLength
                    ) < epsilon,
                    message: "Tacking must preserve rigid boom length."
                );
                Require(
                    value: Math.Abs(
                        (Rotate(point: pose.Heel) - corners[0]).magnitude
                            - (pose.Heel - corners[0]).magnitude
                    ) < epsilon,
                    message: "Tacking must preserve the sprit socket's orbit radius."
                );
                Near(
                    actual: Rotate(point: pose.Tack),
                    expected: pose.Tack,
                    epsilon: epsilon,
                    message: "Tacking must preserve the mast pivot."
                );
            }
        }
        Require(
            value: Math.Abs(
                previousArea
                    - SpritsailDeployment.Area(
                        throat: corners[0],
                        peak: corners[1],
                        tack: corners[2],
                        clew: corners[3]
                    )
            ) < epsilon,
            message: "Fully deployed projected area must equal the companion's planar area."
        );
        foreach (float unroll in new[] { 0.7f, 0.2f, 0.8f, 0f, 1f, 0.2f })
        {
            var first = BoomedSpritsailDeployment.Evaluate(corners: corners, unroll: unroll);
            BoomedSpritsailDeployment.Evaluate(corners: corners, unroll: 1 - unroll);
            var reversed = BoomedSpritsailDeployment.Evaluate(corners: corners, unroll: unroll);
            Near(
                actual: reversed.Clew,
                expected: first.Clew,
                epsilon: epsilon,
                message: "Reef reversals must not retain prior-frame pose state."
            );
        }
    }

    private static void Near(Vector3 actual, Vector3 expected, float epsilon, string message) =>
        Require(value: (actual - expected).magnitude <= epsilon, message: message);

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message: message);
    }
}
