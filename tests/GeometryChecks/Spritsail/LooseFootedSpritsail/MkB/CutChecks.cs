using System;
using MoreSailwindSails.Sails.Spritsail;
using MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail;
using MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.MkA;
using MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.MkB;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail.LooseFootedSpritsail.MkB;

// Exercises the tapered Mk.B cut and its shared skin, edge fit and reefing geometry.
internal static class CutChecks
{
    internal static void Run()
    {
        foreach (float baseWidth in new[] { 0.25f, 1f, 6f, 12f })
        {
            var a = LooseFootedSpritsailGeometry.Create(
                width: baseWidth,
                definition: LooseFootedSpritsailMkA.Definition
            );
            var b = LooseFootedSpritsailGeometry.Create(
                width: baseWidth,
                definition: LooseFootedSpritsailMkB.Definition
            );
            var c = b.Corners;
            float width = baseWidth * 1.3f;
            Require(
                c[0].x == a.Corners[0].x
                    && c[2].x == a.Corners[2].x
                    && c[3].x == a.Corners[3].x
                    && Math.Abs(c[0].z - a.Corners[0].z * 1.3f) < 1e-5f
                    && Math.Abs(c[1].x - baseWidth * LooseFootedSpritsailMkB.Definition.PeakRise)
                        < 1e-5f,
                "Mk.B must widen 30% while retaining every corner height."
            );
            float ratio = (c[3] - c[2]).magnitude / (c[1] - c[0]).magnitude;
            Require(
                Math.Abs(
                    ratio
                        - Math.Sqrt(1.3 * 1.3 + 0.08 * 0.08)
                            / (Math.Sqrt(1 + 0.08 * 0.08) / 1.25 * Math.Sqrt(0.5 * 1.3 * 1.3 + 0.5))
                ) < 1e-5f,
                "The stretched edge ratio must follow the horizontal-only transformation."
            );
            float angle = (float)(
                Math.Acos(Vector3.Dot((c[2] - c[0]).normalized, (c[1] - c[0]).normalized))
                * 180
                / Math.PI
            );
            Require(
                Math.Abs(angle - (90 + Math.Atan(Math.Tan(Math.PI / 4) / 1.3) * 180 / Math.PI))
                    < 0.001,
                "Throat must follow the horizontal stretch of the 135-degree base cut."
            );
            Require(
                c[1].z < c[3].z
                    && b.Vertices.Length == a.Vertices.Length
                    && b.BonePositions.Length == a.BonePositions.Length,
                "Marks share skin topology while Mk.B has a sloping leech."
            );
            for (int row = 0; row <= LooseFootedSpritsailGeometry.Rows; row++)
            for (int col = 0; col <= LooseFootedSpritsailGeometry.Columns; col++)
            {
                int index = row * (LooseFootedSpritsailGeometry.Columns + 1) + col;
                var weight = b.Weights[index];
                var skinned =
                    b.BonePositions[weight.boneIndex0] * weight.weight0
                    + b.BonePositions[weight.boneIndex1] * weight.weight1;
                Require(
                    (skinned - b.Vertices[index]).magnitude < width * 1e-5f,
                    "Mk.B's rest skin must reproduce its mesh."
                );
            }
            var fitted = new Vector3[33];
            Require(
                LooseFootedSpritsailTension.Fit(
                    requested: c[3],
                    head: c[1],
                    tack: c[2],
                    bow: Vector3.zero,
                    leechLength: (c[3] - c[1]).magnitude,
                    footLength: (c[3] - c[2]).magnitude,
                    points: fitted
                ),
                "Mk.B must satisfy the shared coupled edge fit."
            );
            Require(
                (fitted[0] - c[3]).magnitude < width * 1e-4f,
                "Full deployment must not jump the clew."
            );
            for (int column = 0; column < 24; column++)
            {
                bool active = LooseFootedSpritsailMastInstallationGeometry.CollisionStrip(
                    width: width,
                    column: column,
                    center: out var center,
                    size: out var size,
                    corners: c
                );
                Require(
                    SpritsailDeployment.Finite(value: center.sqrMagnitude)
                        && SpritsailDeployment.Finite(value: size.sqrMagnitude),
                    "Tapered collision strips must stay finite."
                );
                if (!active)
                    continue;
                foreach (float z in new[] { center.z - size.z / 2, center.z + size.z / 2 })
                {
                    float u = (z + width) / width;
                    float foot = c[2].x + (c[3].x - c[2].x) * u;
                    float peakU = (c[1].z + width) / width;
                    float top =
                        u <= peakU
                            ? c[1].x * u / peakU
                            : c[1].x + (c[3].x - c[1].x) * (u - peakU) / (1 - peakU);
                    Require(
                        center.x - size.x / 2 >= foot - 1e-5f
                            && center.x + size.x / 2 <= top + 1e-5f,
                        "Panel collision must stay inside the sloping head/leech."
                    );
                }
            }
            var full = SpritsailDeployment.Evaluate(corners: c, unroll: 1);
            float length = (full.Tip - full.Heel).magnitude;
            for (int step = 0; step <= 100; step++)
            {
                var pose = SpritsailDeployment.Evaluate(corners: c, unroll: step / 100f);
                Require(
                    (pose.Heel - full.Heel).magnitude < 1e-6f
                        && Math.Abs((pose.Tip - pose.Heel).magnitude - length) < width * 1e-5f,
                    "Mk.B must reef around the fixed heel with a rigid sprit."
                );
                Require(
                    (pose.Clew - pose.Tack).magnitude <= (c[3] - c[2]).magnitude + 1e-4f
                        && (pose.Clew - pose.Peak).magnitude <= (c[3] - c[1]).magnitude + 1e-4f,
                    "Mk.B reefing must respect both edge budgets."
                );
            }
        }
        Console.WriteLine(
            "PASS: Mk.B 30% horizontal stretch with unchanged corner heights, shared topology, tapered collision and fixed-heel reefing."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }
}
