using System;
using System.Linq;
using MoreSailwindSails.Sails.Spritsail;
using MoreSailwindSails.Sails.Spritsail.BoomedSpritsail;
using MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail;
using UnityEngine;
using MkA = MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.MkA.LooseFootedSpritsailMkA;
using MkB = MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.MkB.LooseFootedSpritsailMkB;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Exercises the embedded mount across both cuts, both deployment paths and mast sizes.
internal static class MountChecks
{
    internal static void Run()
    {
        Require(
            value: SpritsailMountAsset.Positions.Length == 15015
                && SpritsailMountAsset.Triangles.Sum(t => t.Length) == 6566 * 3,
            message: "The complete authored mount must be embedded."
        );
        for (int eye = 0; eye < 7; eye++)
        {
            var rope = Enumerable
                .Range(start: 0, count: SpritsailMountAsset.Positions.Length)
                .Where(i => SpritsailMountAsset.Parts[i] == eye + 8)
                .Select(i => SpritsailMountAsset.Positions[i].x)
                .ToArray();
            float diameter = SpritsailMountAsset.RopeRadii[eye] * 2;
            Require(
                value: Math.Abs(diameter - (rope.Max() - rope.Min())) < 0.000001f
                    && diameter > 0.016f
                    && diameter < 0.017f,
                message: "Runtime rope thickness must follow the thicker authored mesh, not the old 6 mm constant."
            );
        }
        foreach (bool boomed in new[] { false, true })
        foreach (var definition in new[] { MkA.Definition, MkB.Definition })
        foreach (float width in new[] { 1f, 4f, 8f })
        foreach (float radius in new[] { 0.08f, 0.3f, 0.65f })
        foreach (float unroll in new[] { 0.04f, 0.3f, 0.75f, 1f })
        foreach (float rake in new[] { 0f, 0.2f })
        {
            var corners = definition.Corners(width: width);
            var pose = boomed
                ? BoomedSpritsailDeployment.Evaluate(corners: corners, unroll: unroll)
                : SpritsailDeployment.Evaluate(corners: corners, unroll: unroll);
            var luff = new Vector3[33];
            for (int row = 0; row < luff.Length; row++)
                luff[row] = boomed
                    ? BoomedSpritsailGathering.Point(
                        corners: corners,
                        pose: pose,
                        amount: SpritsailDeployment.Amount(unroll: unroll),
                        u: 0,
                        v: row / 32f
                    )
                    : LooseFootedSpritsailGathering.Point(
                        corners: corners,
                        pose: pose,
                        amount: SpritsailDeployment.Amount(unroll: unroll),
                        u: 0,
                        v: row / 32f
                    );
            var mast = new Vector3(
                0,
                -SpritsailSnotterGeometry.LuffDistance(mastRadius: radius),
                corners[0].z
            );
            var vertices = new Vector3[SpritsailMountAsset.Positions.Length];
            var normals = new Vector3[vertices.Length];
            var radii = Enumerable
                .Range(start: 0, count: 7)
                .Select(eye => radius * (0.85f + eye * 0.05f))
                .ToArray();
            var axis = new Vector3(1, rake, -rake * 0.5f).normalized;
            var eyelets = new Vector3[7];
            SpritsailMountGeometry.Fit(
                luff: luff,
                mastOrigin: mast,
                mastAxis: axis,
                radii: radii,
                vertices: vertices,
                normals: normals,
                eyelets: eyelets
            );
            var referenceVertices = new Vector3[vertices.Length];
            var referenceNormals = new Vector3[vertices.Length];
            MountReferenceGeometry.Fit(
                luff: luff,
                mastOrigin: mast,
                mastAxis: axis,
                radii: radii,
                vertices: referenceVertices,
                normals: referenceNormals
            );
            MountOptimizationChecks.Equivalent(
                vertices: vertices,
                normals: normals,
                expectedVertices: referenceVertices,
                expectedNormals: referenceNormals
            );
            for (int i = 0; i < vertices.Length; i++)
            {
                var point = vertices[i];
                Require(
                    value: SpritsailDeployment.Finite(value: point.x)
                        && SpritsailDeployment.Finite(value: point.y)
                        && SpritsailDeployment.Finite(value: point.z)
                        && Math.Abs(normals[i].sqrMagnitude - 1) < 0.001f,
                    message: "Mount positions and shading must remain finite while reefing."
                );
                if (SpritsailMountAsset.Parts[i] >= 8)
                {
                    var radial = point - mast;
                    radial -= axis * Vector3.Dot(lhs: radial, rhs: axis);
                    float localRadius = radii[SpritsailMountAsset.Parts[i] - 8];
                    Require(
                        value: radial.sqrMagnitude >= localRadius * localRadius,
                        message: "Authored rope must remain outside its mast envelope."
                    );
                }
            }
            Vector3 Strip(Vector3 source) =>
                new SpritsailMountFitData.StripSample(source: source, count: luff.Length).Evaluate(
                    points: luff,
                    eyelets: eyelets
                );
            foreach (float x in new[] { -3f, -2.95f, -1.5f, 0f, 1.5f, 2.95f, 3f })
            {
                var seam = Strip(source: new Vector3(x, 0, -0.1f));
                var expected =
                    new SpritsailMountFitData.LuffSample(sourceX: x, count: luff.Length).Evaluate(
                        luff: luff
                    )
                    + Vector3.forward * SpritsailMountGeometry.SeamOverlap;
                Require(
                    value: (seam - expected).sqrMagnitude < 1e-10f,
                    message: "The strip must meet the current luff throughout reefing."
                );
            }
            for (int eye = 1; eye <= 7; eye++)
            {
                float x = SpritsailMountAsset.EyeletX[eye - 1];
                var edgeOfHole = new Vector3(x + 0.015f, 0, -0.05f);
                Require(
                    value: (
                        Strip(source: edgeOfHole)
                        - SpritsailMountGeometry.Eyelet(points: luff, sourceX: x)
                        - Vector3.right * 0.015f
                    ).sqrMagnitude < 1e-10f,
                    message: "Strip holes must stay aligned with rigid eyelets after resizing/reefing."
                );
                var indices = Enumerable
                    .Range(start: 0, count: vertices.Length)
                    .Where(i => SpritsailMountAsset.Parts[i] == eye)
                    .ToArray();
                int a = indices[0],
                    b = indices[indices.Length / 2];
                Require(
                    value: Math.Abs(
                        (vertices[a] - vertices[b]).magnitude
                            - (
                                SpritsailMountAsset.Positions[a] - SpritsailMountAsset.Positions[b]
                            ).magnitude
                    ) < 0.00001f,
                    message: "Eyelets must retain their authored dimensions."
                );
            }
        }
        MountOptimizationChecks.Run();
        Console.WriteLine(
            "PASS: reference equivalence, authored mount topology, both cuts/types, sizes, reef seams, rigid eyelets and mast clearance; no Unity rendering or Cloth simulation."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message: message);
    }
}
