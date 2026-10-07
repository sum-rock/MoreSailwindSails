using System;
using System.Collections.Generic;
using System.IO;
using MoreSailwindSails.Sails.Spritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Checks the embedded three-part fit, independent mounting motion and forward sprit tail.
internal static class SnotterChecks
{
    internal static void Run()
    {
        Require(
            value: Math.Abs(
                SpritsailSpritGeometry.RadiusAtPivot(radius: 1, pivotToTip: 0.9144f) - 0.925f
            ) < 1e-6f,
            message: "Bolt reach must use the tapered sprit section at the 12-inch pivot, not its midpoint radius."
        );
        foreach (float sparRadius in new[] { 0.025f, 0.08f, 0.2f })
        foreach (float mastRadius in new[] { 0.1f, 0.3f, 0.8f })
        {
            float pivot =
                SpritsailSnotterGeometry.LuffDistance(mastRadius: mastRadius) + sparRadius;
            SpritsailSnotterGeometry.Create(
                sparRadius: sparRadius,
                mastRadius: mastRadius,
                pivotDistance: pivot,
                vertices: out var vertices,
                normals: out var normals,
                uv: out var uv,
                triangles: out var triangles
            );
            Require(
                value: vertices.Length == 3164
                    && triangles.Length == 4902
                    && uv.Length == vertices.Length,
                message: "The authored mesh must be embedded with its full triangulated topology and UVs."
            );
            Require(
                value: pivot > mastRadius + sparRadius,
                message: "The starboard pivot must leave room for the sprit beside the mast."
            );
            bool pinRoot = false,
                head = false;
            var partCounts = new int[3];
            float mountingOuter = 0,
                sleeveOuter = 0,
                boltEnd = 0;
            float sleeveBottom = float.MaxValue,
                mountingTop = float.MinValue;
            for (int i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                Require(
                    value: float.IsFinite(vertex.x)
                        && float.IsFinite(vertex.y)
                        && float.IsFinite(vertex.z),
                    message: "Fitted snotter coordinates must be finite."
                );
                Require(
                    value: float.IsFinite(normals[i].sqrMagnitude)
                        && Math.Abs(normals[i].sqrMagnitude - 1) < 1e-5f,
                    message: "Authored shading normals must remain finite unit vectors after fitting every part."
                );
                int part = SpritsailSnotterGeometry.Part(vertex: i);
                partCounts[part]++;
                if (part != SpritsailSnotterGeometry.BoltPart)
                {
                    float radial = (float)Math.Sqrt(vertex.x * vertex.x + vertex.z * vertex.z);
                    Require(
                        value: radial > mastRadius,
                        message: "Both sleeve and mounting openings must clear the mast."
                    );
                    if (part == SpritsailSnotterGeometry.SleevePart)
                    {
                        sleeveBottom = Math.Min(sleeveBottom, vertex.y);
                        sleeveOuter = Math.Max(sleeveOuter, radial);
                    }
                    else
                    {
                        mountingTop = Math.Max(mountingTop, vertex.y);
                        mountingOuter = Math.Max(mountingOuter, radial);
                    }
                }
                else
                {
                    boltEnd = Math.Max(boltEnd, vertex.z);
                    pinRoot |= vertex.z < mastRadius;
                    head |= vertex.z > pivot + sparRadius;
                }
                // Exercise the same part-dependent posing used by the renderer.
                var center = new Vector3(4, 7, -2);
                var neutral = SpritsailSnotterGeometry.PosePoint(
                    vertex: i,
                    point: vertex,
                    center: center,
                    axis: Vector3.up,
                    rotatingDirection: Vector3.forward,
                    fixedDirection: Vector3.forward
                );
                foreach (float degrees in new[] { -89f, -40f, 40f, 89f })
                {
                    float angle = degrees * (float)Math.PI / 180;
                    var rotating = new Vector3((float)Math.Sin(angle), 0, (float)Math.Cos(angle));
                    var posed = SpritsailSnotterGeometry.PosePoint(
                        vertex: i,
                        point: vertex,
                        center: center,
                        axis: Vector3.up,
                        rotatingDirection: rotating,
                        fixedDirection: Vector3.forward
                    );
                    Require(
                        value: part == SpritsailSnotterGeometry.MountingPart
                            ? (posed - neutral).sqrMagnitude < 1e-10f
                            : (posed - neutral).sqrMagnitude > 1e-8f,
                        message: "Only sleeve and bolt vertices may rotate when the sail tacks."
                    );
                    // Turn and translate the entire boat: even the fixed clip must follow it.
                    var moved = SpritsailSnotterGeometry.PosePoint(
                        vertex: i,
                        point: vertex,
                        center: center + Vector3.up * 5,
                        axis: Vector3.forward,
                        rotatingDirection: new Vector3(rotating.x, -rotating.z, 0),
                        fixedDirection: Vector3.down
                    );
                    var relative = posed - center;
                    var expected =
                        center + Vector3.up * 5 + new Vector3(relative.x, -relative.z, relative.y);
                    Require(
                        value: (moved - expected).sqrMagnitude < 1e-9f,
                        message: "All parts must follow boat motion, including the mast-fixed mounting."
                    );
                }
            }
            Require(
                value: Math.Abs(2 * (mountingOuter - mastRadius) - 0.2032f) < 1e-5f,
                message: "The mounting outside diameter must exceed the mast diameter by eight inches at every size."
            );
            float spritGap = pivot - sparRadius - sleeveOuter;
            Require(
                value: spritGap >= 0.0029f && spritGap < 0.004f,
                message: "The sprit must sit within 3–4 mm of the sleeve envelope regardless of sail size."
            );
            Require(
                value: Math.Abs(boltEnd - (pivot + sparRadius) - 0.005f) < 1e-5f,
                message: "Only 5 mm of bolt may project beyond the sprit; mast size must not stretch the head."
            );
            Require(
                value: partCounts[0] == 536 && partCounts[1] == 2240 && partCounts[2] == 388,
                message: "The bake must preserve all three independently identified objects."
            );
            Require(
                value: Math.Abs(sleeveBottom - mountingTop) < 1e-6f && mountingTop < 0,
                message: "The mounting must remain directly below the sleeve at every mast size."
            );
            Require(
                value: pinRoot && head,
                message: "The bolt must enter the mast and extend through the sprit to its head."
            );
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var cross = Vector3.Cross(
                    vertices[triangles[i + 1]] - vertices[triangles[i]],
                    vertices[triangles[i + 2]] - vertices[triangles[i]]
                );
                Require(
                    value: cross.sqrMagnitude > 1e-16f,
                    message: "Fitting must not collapse authored triangles."
                );
            }
            var original = vertices[0];
            var originalUv = uv[0];
            var originalNormal = normals[0];
            normals[0] = Vector3.zero;
            int originalIndex = triangles[0];
            vertices[0] = Vector3.zero;
            uv[0] = Vector2.zero;
            triangles[0] = -1;
            SpritsailSnotterGeometry.Create(
                sparRadius: sparRadius,
                mastRadius: mastRadius,
                pivotDistance: pivot,
                vertices: out var fresh,
                normals: out var freshNormals,
                uv: out var freshUv,
                triangles: out var freshTriangles
            );
            Require(
                value: freshNormals[0] == originalNormal
                    && fresh[0] == original
                    && freshUv[0] == originalUv
                    && freshTriangles[0] == originalIndex,
                message: "Posing one fitting must not modify the cached mesh or another instance."
            );
        }
        CheckMaterials();
        CheckSurfaceData();
        foreach (float size in new[] { 0.5f, 1f, 3f })
        foreach (float unroll in new[] { 0f, 0.25f, 0.75f, 1f })
        {
            var corners = new[]
            {
                new Vector3(4, 0, 0),
                new Vector3(5, 0, 3),
                Vector3.zero,
                new Vector3(0.2f, 0, 3),
            };
            for (int i = 0; i < corners.Length; i++)
                corners[i] *= size;
            var poses = new[]
            {
                SpritsailDeployment.Evaluate(corners: corners, unroll: unroll),
                MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.BoomedSpritsailDeployment.Evaluate(
                    corners: corners,
                    unroll: unroll
                ),
            };
            foreach (var pose in poses)
            {
                // The mast axis is inboard of the luff; the bolt orbits it with the sail.
                float luffDistance = SpritsailSnotterGeometry.LuffDistance(mastRadius: 0.3f);
                var mastCenter = new Vector3(pose.Heel.x, -luffDistance, 0);
                float boltDistance = (pose.Heel - mastCenter).magnitude;
                foreach (float angle in new[] { -89f, -40f, 0f, 40f, 89f })
                {
                    Vector3 Rotate(Vector3 point) =>
                        MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.LooseFootedSpritsailFrameGeometry.RotateAroundMast(
                            point: point,
                            pivot: mastCenter,
                            axis: Vector3.right,
                            degrees: angle
                        );
                    var heel = Rotate(point: pose.Heel);
                    var tip = Rotate(point: pose.Tip);
                    var bolt = (heel - mastCenter).normalized;
                    Require(
                        value: Math.Abs(Vector3.Dot((tip - heel).normalized, bolt)) < 1e-5f,
                        message: "The sprit must remain perpendicular to the radial bolt on both tacks, including fully struck."
                    );
                    Require(
                        value: Math.Abs((heel - mastCenter).magnitude - boltDistance) < 1e-5f
                            && (heel - bolt * boltDistance - mastCenter).magnitude < 1e-5f,
                        message: "The bolt must orbit while the fitting remains centered on the mast."
                    );
                    if (angle != 0)
                        Require(
                            value: (heel - pose.Heel).magnitude > 0.01f,
                            message: "Tacking must carry the bolt around the mast rather than leave it fixed to starboard."
                        );
                    var forwardEnd = SpritsailSpritGeometry.ForwardEnd(pivot: heel, tip: tip);
                    Require(
                        value: Math.Abs((forwardEnd - heel).magnitude - 0.3048f) < 1e-5f
                            && Math.Abs(Vector3.Dot(forwardEnd - heel, bolt)) < 1e-5f,
                        message: "The rotating forward tail must stay 12 inches long and perpendicular to the bolt."
                    );
                }
                var end = SpritsailSpritGeometry.ForwardEnd(pivot: pose.Heel, tip: pose.Tip);
                Require(
                    value: Math.Abs((end - pose.Heel).magnitude - 0.3048f) < 1e-5f
                        && Vector3.Dot(end - pose.Heel, pose.Tip - pose.Heel) < 0
                        && Vector3.Cross(end - pose.Heel, pose.Tip - pose.Heel).sqrMagnitude
                            < 1e-8f,
                    message: "Both types must retain a collinear 12-inch forward tail at every size and reef pose."
                );
            }
        }
        Console.WriteLine(
            "PASS: embedded sleeve/bolt/fixed mounting, authored wood/metal face assignments, mast clearance, independent tacking/boat motion, rotating bolt orbit, perpendicular sprit, 12-inch tail and independent mesh data."
        );
    }

    private static void CheckMaterials()
    {
        SpritsailSnotterGeometry.Create(
            sparRadius: 0.08f,
            mastRadius: 0.3f,
            pivotDistance: 0.5f,
            vertices: out _,
            normals: out _,
            uv: out _,
            triangles: out var all
        );
        var remaining = new HashSet<(int, int, int)>();
        for (int i = 0; i < all.Length; i += 3)
            Require(
                value: remaining.Add((all[i], all[i + 1], all[i + 2])),
                message: "The baked topology must not repeat a triangle."
            );
        // Expected from the authored assignments, including the overlapping mounting faces.
        var expected = new[,]
        {
            { 20, 0, 144 },
            { 248, 1144, 78 },
        };
        for (int material = 0; material < SpritsailSnotterGeometry.MaterialCount; material++)
        {
            var triangles = SpritsailSnotterGeometry.MaterialTriangles(material: material);
            var counts = new int[3];
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int part = SpritsailSnotterGeometry.Part(vertex: triangles[i]);
                Require(
                    value: SpritsailSnotterGeometry.Part(vertex: triangles[i + 1]) == part
                        && SpritsailSnotterGeometry.Part(vertex: triangles[i + 2]) == part,
                    message: "Material triangles must stay within their independently moving part."
                );
                counts[part]++;
                Require(
                    value: remaining.Remove((triangles[i], triangles[i + 1], triangles[i + 2])),
                    message: "Each source triangle must occur in exactly one material slot with its winding intact."
                );
            }
            for (int part = 0; part < counts.Length; part++)
                Require(
                    value: counts[part] == expected[material, part],
                    message: "The actual baked faces must retain their authored DarkWood/Metal assignments."
                );
            int original = triangles[0];
            triangles[0] = -1;
            Require(
                value: SpritsailSnotterGeometry.MaterialTriangles(material: material)[0]
                    == original,
                message: "An instance must not modify another instance's material indices."
            );
        }
        Require(
            value: remaining.Count == 0,
            message: "Material splitting must not lose any source faces."
        );
    }

    private static void CheckSurfaceData()
    {
        SpritsailSnotterGeometry.Create(
            sparRadius: 0.08f,
            mastRadius: 0.3f,
            pivotDistance: 0.5f,
            vertices: out var vertices,
            normals: out var normals,
            uv: out var mapped,
            triangles: out var triangles
        );
        // Compare against the embedded export, independently of the runtime UV cache.
        using var stream = typeof(SpritsailSnotterGeometry).Assembly.GetManifestResourceStream(
            name: SpritsailSnotterGeometry.ResourceName
        );
        using var reader = new BinaryReader(input: stream);
        stream.Position = 12;
        var original = new Vector2[mapped.Length];
        var sourceNormals = new Vector3[mapped.Length];
        bool adjustedMountingNormal = false;
        for (int i = 0; i < original.Length; i++)
        {
            float x = reader.ReadSingle(),
                y = reader.ReadSingle(),
                z = reader.ReadSingle();
            var point = new Vector3(z, y, -x);
            original[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            float nx = reader.ReadSingle(),
                ny = reader.ReadSingle(),
                nz = reader.ReadSingle();
            var normal = new Vector3(nz, ny, -nx).normalized;
            sourceNormals[i] = normal;
            int part = reader.ReadInt32();
            if (part == SpritsailSnotterGeometry.SleevePart)
                Require(
                    value: (normals[i] - normal).sqrMagnitude < 1e-10f,
                    message: "Uniform sleeve fitting must preserve every exported shading normal, including UV seams."
                );
            if (part == SpritsailSnotterGeometry.MountingPart)
            {
                // Derive the actual fit from positions, independently of fit constants.
                float radial = (float)
                    Math.Sqrt(
                        (vertices[i].x * vertices[i].x + vertices[i].z * vertices[i].z)
                            / (point.x * point.x + point.z * point.z)
                    );
                float vertical = vertices[i].y / point.y;
                var tangent = Vector3.Cross(
                    normal,
                    Math.Abs(normal.y) < 0.9f ? Vector3.up : Vector3.right
                );
                var bitangent = Vector3.Cross(normal, tangent);
                foreach (var direction in new[] { tangent, bitangent })
                {
                    var fitted = new Vector3(
                        direction.x * radial,
                        direction.y * vertical,
                        direction.z * radial
                    );
                    Require(
                        value: Math.Abs(Vector3.Dot(normals[i], fitted.normalized)) < 1e-5f,
                        message: "Mounting normals must stay perpendicular to the authored tangent plane after unequal radial/vertical fitting."
                    );
                }
                adjustedMountingNormal |= (normals[i] - normal).sqrMagnitude > 1e-6f;
            }
        }
        Require(
            value: adjustedMountingNormal,
            message: "The smooth mounting export must exercise inverse-transpose normal fitting."
        );
        bool smoothSleeve = false;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i],
                b = triangles[i + 1],
                c = triangles[i + 2];
            if (SpritsailSnotterGeometry.Part(vertex: a) != SpritsailSnotterGeometry.SleevePart)
                continue;
            var faceNormal = Vector3
                .Cross(vertices[b] - vertices[a], vertices[c] - vertices[a])
                .normalized;
            smoothSleeve |= (sourceNormals[a] - faceNormal).sqrMagnitude > 0.001f;
        }
        Require(
            value: smoothSleeve,
            message: "The sleeve bake must retain smooth vertex normals distinct from flat triangle normals."
        );
        foreach (
            int index in SpritsailSnotterGeometry.MaterialTriangles(
                material: SpritsailSnotterGeometry.MetalMaterial
            )
        )
            Require(
                value: mapped[index] == original[index],
                message: "Mast trim remapping must preserve every metal UV from the Blender export."
            );
        foreach (
            int index in SpritsailSnotterGeometry.MaterialTriangles(
                material: SpritsailSnotterGeometry.DarkWoodMaterial
            )
        )
        {
            var uv = mapped[index];
            Require(
                value: float.IsFinite(uv.x)
                    && float.IsFinite(uv.y)
                    && uv.x >= 0.350f - 1e-6f
                    && uv.x <= 0.352f + 1e-6f
                    && uv.y >= 0.870f - 1e-6f
                    && uv.y <= 0.900f + 1e-6f,
                message: "Every wood face must sample only the inspected dark mast trim patch, including shared triangle corners."
            );
        }
        Console.WriteLine(
            "PASS: authored smooth normals, nonuniform mounting normal fit, dark mast trim UV containment and unchanged authored metal UVs."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
    }
}
