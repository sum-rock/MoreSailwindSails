// Checks the emitted C# data's coordinate conversion, channels and independent factory ownership.
using System;
using MoreSailwindSails.Sails.LooseFootedSpritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.LooseFootedSpritsail;

internal static class BakedAssetChecks
{
    internal static void Run()
    {
        var asset = LooseFootedSpritsailMkABakedAsset.Create();
        var other = LooseFootedSpritsailMkABakedAsset.Create();
        Check(part: asset.Sail, other: other.Sail);
        Check(part: asset.Sprit, other: other.Sprit);
        Check(part: asset.SnotterAssembly, other: other.SnotterAssembly);
        Check(part: asset.RotationPlate, other: other.RotationPlate);
        if (asset.Sail.Vertices.Length != 320 || asset.SheetVertex != asset.Clew)
            throw new Exception("Lost authored cloth vertex correspondence.");
        Near(actual: asset.SheetLineAttachment, expected: asset.Sail.Vertices[asset.Clew]);
        Near(
            actual: asset.Sail.RestTransform.MultiplyPoint3x4(asset.SheetLineAttachment),
            expected: new Vector3(x: 0, y: -2, z: -14.3f)
        );
        Near(
            actual: asset.Sprit.RestTransform.MultiplyPoint3x4(asset.ReefLineAttachment),
            expected: new Vector3(x: 0.15495883f, y: 13.9923897f, z: -13.9955626f)
        );
        Near(
            actual: asset.Sprit.RestTransform.MultiplyPoint3x4(Vector3.zero),
            expected: new Vector3(x: 0.45f, y: 0.1f, z: 0)
        );
        Near(
            actual: asset.SnotterAssembly.RestTransform.MultiplyPoint3x4(Vector3.zero),
            expected: new Vector3(x: 0, y: 0.1f, z: 0)
        );
        Console.WriteLine(
            "PASS: spritsail generated channels, assembly frames, anchors and independent arrays."
        );
    }

    private static void Check(LooseFootedSpritsailMeshPart part, LooseFootedSpritsailMeshPart other)
    {
        if (
            part.Vertices.Length == 0
            || part.Vertices.Length != part.Normals.Length
            || part.Vertices.Length != part.UV.Length
            || part.Materials.Length == 0
            || part.Materials.Length != part.SubmeshTriangles.Length
            || ReferenceEquals(part.Vertices, other.Vertices)
            || ReferenceEquals(part.Normals, other.Normals)
            || ReferenceEquals(part.UV, other.UV)
            || ReferenceEquals(part.Materials, other.Materials)
            || ReferenceEquals(part.SubmeshTriangles, other.SubmeshTriangles)
        )
            throw new Exception("Incomplete or shared spritsail channels.");
        for (int submesh = 0; submesh < part.Materials.Length; submesh++)
        {
            var triangles = part.SubmeshTriangles[submesh];
            if (
                triangles.Length == 0
                || triangles.Length % 3 != 0
                || ReferenceEquals(triangles, other.SubmeshTriangles[submesh])
            )
                throw new Exception("Incomplete or shared spritsail submesh.");
            foreach (int index in triangles)
                if (index < 0 || index >= part.Vertices.Length)
                    throw new Exception("Invalid spritsail vertex index.");
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = part.Vertices[triangles[i]];
                var b = part.Vertices[triangles[i + 1]];
                var c = part.Vertices[triangles[i + 2]];
                var cross = Vector3.Cross(lhs: b - a, rhs: c - a);
                if (cross.sqrMagnitude <= 1e-20f)
                    throw new Exception("Degenerate generated spritsail triangle.");
                for (int corner = 0; corner < 3; corner++)
                    if (Vector3.Dot(lhs: cross, rhs: part.Normals[triangles[i + corner]]) <= 0)
                        throw new Exception("Generated spritsail winding disagrees with normals.");
            }
        }
    }

    private static void Near(Vector3 actual, Vector3 expected)
    {
        if ((actual - expected).sqrMagnitude > 1e-10f)
            throw new Exception($"Expected {expected}, got {actual}.");
    }
}
