using System;
using MoreSailwindSails.Sails.Spritsail;
using MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail.LooseFootedSpritsail.MkA;

// Exercises the new cut, fixed-length spar and reversible deployment independently of Unity.
internal static class PrototypeChecks
{
    internal static void Run()
    {
        foreach (float width in new[] { 0.25f, 1f, 4f, 12f })
        {
            var data = LooseFootedSpritsailGeometry.Create(width: width);
            CheckMesh(data: data, width: width);
            foreach (
                var scale in new[]
                {
                    Vector3.one,
                    new Vector3(0.5f, 1.2f, 1.4f),
                    new Vector3(2, 2, 2),
                }
            )
            foreach (float drop in new[] { 0f, 3f, 10f })
            {
                var corners = Array.ConvertAll(
                    data.Corners,
                    point => Vector3.Scale(point, scale) + Vector3.right * drop
                );
                var full = SpritsailDeployment.Evaluate(corners: corners, unroll: 1);
                float length = (full.Tip - full.Heel).magnitude;
                Require(
                    value: (full.Peak - corners[1]).magnitude < width * 0.0001f,
                    message: "Full peak must match the authored cut."
                );
                if (Math.Abs(scale.x - scale.z) < 1e-6f)
                {
                    var luff = (full.Tack - full.Throat).normalized;
                    var head = (full.Peak - full.Throat).normalized;
                    double throatAngle = Math.Acos(Vector3.Dot(luff, head)) * 180 / Math.PI;
                    Require(
                        value: Math.Abs(throatAngle - 105) < 0.001,
                        message: "The fully deployed, uniformly scaled cut must have a 105-degree throat."
                    );
                    var spar = full.Tip - full.Heel;
                    double sparAngle = Math.Atan2(spar.z, spar.x);
                    double expectedAngle = Math.Atan2(1, 1.2 + Math.Tan(Math.PI / 12));
                    Require(
                        value: Math.Abs(sparAngle - expectedAngle) < 1e-5,
                        message: "The sprit must lean farther aft to reach the revised peak from its quarter-luff heel."
                    );
                    Require(
                        value: Math.Abs(
                            full.Heel.x - (corners[2].x + (corners[0].x - corners[2].x) * 0.25f)
                        )
                            < width * 0.0001f,
                        message: "The revised sprit must retain the quarter-luff snotter attachment."
                    );
                }
                float previousArea = 0;
                for (int frame = 0; frame <= 100; frame++)
                {
                    float unroll = frame / 100f;
                    var pose = SpritsailDeployment.Evaluate(corners: corners, unroll: unroll);
                    var repeat = SpritsailDeployment.Evaluate(
                        corners: corners,
                        unroll: 1 - (100 - frame) / 100f
                    );
                    Require(
                        value: Math.Abs((pose.Tip - pose.Heel).magnitude - length)
                            < length * 0.00001f,
                        message: "Sprit changed length during hoist or scaling."
                    );
                    Require(
                        value: (pose.Peak - repeat.Peak).magnitude < length * 0.00001f,
                        message: "Reversing the winch must reproduce the same pose."
                    );
                    Require(
                        value: (pose.Heel - full.Heel).magnitude < length * 0.00001f,
                        message: "The sprit heel must stay in its fixed mast socket."
                    );
                    Require(
                        value: Math.Abs(pose.Throat.z - pose.Tack.z) < width * 0.00001f,
                        message: "The luff must stay on its mast line."
                    );
                    Require(
                        value: Math.Abs((pose.Tip - pose.Peak).magnitude - width * scale.z * 0.04f)
                            < length * 0.00001f,
                        message: "The working peak must remain lashed beside the spar tip."
                    );
                    float area = SpritsailDeployment.ExposedArea(pose: pose);
                    Require(
                        value: SpritsailDeployment.Finite(value: area) && area >= 0,
                        message: "Non-finite deployment area."
                    );
                    Require(
                        value: area + length * length * 0.00001f >= previousArea,
                        message: $"Increasing deployment reduces area: width={width}, scale={scale.x}/{scale.y}/{scale.z}, drop={drop}, unroll={unroll}, area={area}, previous={previousArea}."
                    );
                    previousArea = area;
                    foreach (float angle in new[] { -89f, -60f, -40f, 0f, 40f, 60f, 89f })
                    {
                        var rotated = LooseFootedSpritsailFrameGeometry.RotateAroundMast(
                            point: pose.Tip,
                            pivot: pose.Heel,
                            axis: Vector3.right,
                            degrees: angle
                        );
                        Require(
                            value: Math.Abs((rotated - pose.Heel).magnitude - length)
                                < length * 0.00001f,
                            message: "Tacking stretched the spar."
                        );
                    }
                }
            }
            var points = new Vector3[33];
            var c = data.Corners;
            Require(
                value: LooseFootedSpritsailTension.Fit(
                    requested: c[3],
                    head: c[1],
                    tack: c[2],
                    bow: Vector3.zero,
                    leechLength: (c[1] - c[3]).magnitude,
                    footLength: (c[2] - c[3]).magnitude,
                    points: points
                ),
                message: "The deployed cut cannot satisfy coupled edge budgets."
            );
            Require(
                value: (points[0] - c[3]).magnitude < width * 0.0001f,
                message: "Entering full Cloth must not jump the clew."
            );
            Require(
                value: (points[32] - c[1]).sqrMagnitude < 1e-8f,
                message: "Edge fitting moved the sprit peak."
            );
            Require(
                value: (points[0] - c[2]).magnitude <= (c[3] - c[2]).magnitude + width * 0.00001f,
                message: "Edge fitting overstretched the foot."
            );
        }
        foreach (
            float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f }
        )
            Require(
                value: SpritsailDeployment.Amount(unroll: bad) == 0,
                message: "Invalid deployment must gather safely."
            );
        Require(
            value: LooseFootedSpritsailGeometry.RenderState(unroll: float.NaN) == 0,
            message: "Invalid deployment must not activate Cloth."
        );
        Console.WriteLine(
            "PASS: 105-degree spritsail throat, full-width head, revised spar angle, inscribed collision strips, mesh pins/weights/winding, scaled rigid spar, reversible hoist, both tacks, monotonic area and edge budgets."
        );
    }

    private static void CheckMesh(LooseFootedSpritsailMeshData data, float width)
    {
        float area = 0;
        for (int i = 0; i < data.Triangles.Length; i += 3)
        {
            var cross = Vector3.Cross(
                data.Vertices[data.Triangles[i + 1]] - data.Vertices[data.Triangles[i]],
                data.Vertices[data.Triangles[i + 2]] - data.Vertices[data.Triangles[i]]
            );
            Require(value: cross.y > 0, message: "Spritsail triangle inverted or collapsed.");
            area += cross.y * 0.5f;
        }
        Require(
            value: Math.Abs(area - width * width * 1.6939746f) < width * width * 0.0001f,
            message: "The spritsail silhouette area changed."
        );
        Require(
            value: Math.Abs(data.Corners[1].z - data.Corners[3].z) < width * 1e-6f
                && Math.Abs(data.Corners[0].x - data.Corners[2].x - width * 1.6f) < width * 1e-6f
                && Math.Abs(data.Corners[3].x - data.Corners[2].x - width * 0.08f) < width * 1e-6f,
            message: "The peak must lie above the clew while preserving luff length and foot slope."
        );
        for (int row = 0; row <= LooseFootedSpritsailGeometry.Rows; row++)
        for (int column = 0; column <= LooseFootedSpritsailGeometry.Columns; column++)
        {
            int index = row * (LooseFootedSpritsailGeometry.Columns + 1) + column;
            var weight = data.Weights[index];
            Require(
                value: Math.Abs(weight.weight0 + weight.weight1 - 1) < 1e-6f
                    && weight.weight0 >= weight.weight1,
                message: "Invalid skin weights."
            );
            var reconstructed =
                data.BonePositions[weight.boneIndex0] * weight.weight0
                + data.BonePositions[weight.boneIndex1] * weight.weight1;
            Require(
                value: (reconstructed - data.Vertices[index]).magnitude < width * 0.00001f,
                message: "Rest skin does not reconstruct the panel."
            );
            if (column == 0)
                Require(
                    value: data.Constraints[index].maxDistance == 0,
                    message: "The luff must be pinned along its full length."
                );
            if (
                row == LooseFootedSpritsailGeometry.Rows
                && column > 0
                && column < LooseFootedSpritsailGeometry.Columns
            )
                Require(
                    value: data.Constraints[index].maxDistance > 0,
                    message: "The loose foot must have cloth travel."
                );
        }
        for (int column = 0; column < LooseFootedSpritsailGeometry.Columns; column++)
        {
            Require(
                value: LooseFootedSpritsailMastInstallationGeometry.CollisionStrip(
                    width: width,
                    column: column,
                    center: out var center,
                    size: out var size
                ),
                message: "Every column of the full-width cut must provide a collision strip."
            );
            Require(
                value: size.x > 0 && size.y > 0 && size.z > 0 && center.z < 0,
                message: "Invalid sail collision strip."
            );
            foreach (float z in new[] { center.z - size.z / 2, center.z + size.z / 2 })
            {
                float u = (z - data.Corners[0].z) / width;
                float head = Vector3.Lerp(data.Corners[0], data.Corners[1], u).x;
                float foot = Vector3.Lerp(data.Corners[2], data.Corners[3], u).x;
                Require(
                    value: SpritsailDeployment.Finite(value: center.x)
                        && SpritsailDeployment.Finite(value: size.x)
                        && u >= 0
                        && u <= 1
                        && center.x + size.x / 2 <= head + width * 1e-6f
                        && center.x - size.x / 2 >= foot - width * 1e-6f,
                    message: "Collision strip must remain finite and inside the revised head and foot."
                );
            }
        }
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new Exception(message: message);
    }
}
