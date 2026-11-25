// Checks the emitted C# data's coordinate conversion, channels and independent factory ownership.
using System;
using MoreSailwindSails.Sails.LooseFootedSpritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.LooseFootedSpritsail;

internal static class BakedAssetChecks
{
    internal static void Run()
    {
        CheckWindCenters();
        var d = LooseFootedSpritsailMkDBakedAsset.Create();
        var dOther = LooseFootedSpritsailMkDBakedAsset.Create();
        Check(part: d.Sail, other: dOther.Sail);
        Check(part: d.Spar, other: dOther.Spar);
        if (
            !d.HasTopsail
            || d.ReefPoses.Length != 11
            || d.ReefFractions[4] != 0.2f
            || d.ReefFractions[5] != 0.25f
        )
            throw new Exception("Mk.D lost its staged topsail reefing.");
        for (int i = 0; i < d.Sail.Vertices.Length; i++)
            Near(actual: d.ReefPoses[4].Vertices[i], expected: d.ReefPoses[5].Vertices[i]);
        var b = LooseFootedSpritsailMkBBakedAsset.Create();
        var bOther = LooseFootedSpritsailMkBBakedAsset.Create();
        Check(part: b.Sail, other: bOther.Sail);
        Check(part: b.Struck, other: bOther.Struck, folded: true);
        Check(part: b.Sprit, other: bOther.Sprit);
        Check(part: b.SnotterAssembly, other: bOther.SnotterAssembly);
        Check(part: b.RotationPlate, other: bOther.RotationPlate);
        if (b.ReefPoses.Length != 5 || b.Sail.Vertices.Length != 272)
            throw new Exception("Mk.B lost its authored mesh or reef poses.");
        Near(actual: b.SheetLineAttachment, expected: b.Sail.Vertices[b.Clew]);
        var asset = LooseFootedSpritsailMkABakedAsset.Create();
        var other = LooseFootedSpritsailMkABakedAsset.Create();
        Check(part: asset.Struck, other: other.Struck, folded: true);
        if (asset.Struck.Materials.Length != 4)
            throw new Exception(
                "Struck cloth, wood, metal and black ties require separate materials."
            );
        Near(
            actual: asset.Struck.RestTransform.MultiplyPoint3x4(Vector3.zero),
            expected: asset.Sprit.RestTransform.MultiplyPoint3x4(Vector3.zero)
        );
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
            expected: new Vector3(x: 0.206746399f, y: 14.0048695f, z: -14.0332127f)
        );
        Near(
            actual: asset.Sprit.RestTransform.MultiplyPoint3x4(Vector3.zero),
            expected: new Vector3(x: 0.51f, y: 0.15f, z: 0)
        );
        Near(
            actual: asset.SnotterAssembly.RestTransform.MultiplyPoint3x4(Vector3.zero),
            expected: new Vector3(x: 0, y: 0.15f, z: 0)
        );
        Console.WriteLine(
            "PASS: spritsail generated channels, assembly frames, anchors and independent arrays."
        );
    }

    private static void CheckWindCenters()
    {
        var cuts = new[]
        {
            LooseFootedSpritsailMkABakedAsset.Create(),
            LooseFootedSpritsailMkBBakedAsset.Create(),
            LooseFootedSpritsailMkCBakedAsset.Create(),
            LooseFootedSpritsailMkDBakedAsset.Create(),
        };
        var centers = new[]
        {
            new Vector3(0, 7.252f, -6.996f),
            new Vector3(0, 10.962f, -8.865f),
            new Vector3(0, 13.769f, -7.157f),
            new Vector3(0, 15.243f, -8.661f),
        };
        var mainCenters = new[]
        {
            new Vector3(0, 9.675f, -8.226f),
            new Vector3(0, 10.715f, -9.570f),
        };
        for (int i = 0; i < cuts.Length; i++)
        {
            var cut = cuts[i];
            CheckWindCenter(pose: cut.WindCenter, sail: cut.Sail, expected: centers[i]);
            if (cut.WindCenterMain.HasValue != cut.HasTopsail)
                throw new Exception("Only topsail cuts need a main-only windcenter.");
            if (cut.HasTopsail)
                CheckWindCenter(
                    pose: cut.WindCenterMain.Value,
                    sail: cut.Sail,
                    expected: mainCenters[i - 2]
                );
        }
        Console.WriteLine(
            "PASS: spritsail baked windcenters, optional main-only poses and native sensor axes."
        );
    }

    private static void CheckWindCenter(
        LooseFootedSpritsailWindCenter pose,
        LooseFootedSpritsailMeshPart sail,
        Vector3 expected
    )
    {
        Near(actual: sail.RestTransform.MultiplyPoint3x4(pose.Position), expected: expected);
        Near(actual: pose.Rotation * Vector3.right, expected: Vector3.back);
        Near(actual: pose.Rotation * Vector3.up, expected: Vector3.left);
        Near(actual: pose.Rotation * Vector3.forward, expected: Vector3.up);
    }

    private static void Check(
        LooseFootedSpritsailMeshPart part,
        LooseFootedSpritsailMeshPart other,
        bool folded = false
    )
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
                    if (
                        !folded
                        && Vector3.Dot(lhs: cross, rhs: part.Normals[triangles[i + corner]]) <= 0
                    )
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
