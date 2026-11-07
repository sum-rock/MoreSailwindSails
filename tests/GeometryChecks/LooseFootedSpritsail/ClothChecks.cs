// Checks authored cloth poses, attachment correction, rigid hardware and fitting clearance.
using System;
using System.Linq;
using MoreSailwindSails.Sails.LooseFootedSpritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.LooseFootedSpritsail;

internal static class ClothChecks
{
    internal static void Run()
    {
        var data = LooseFootedSpritsailMkABakedAsset.Create();
        Check(data.ReefPoses.Length == 5, "Expected exactly five authored poses.");
        CheckSamples(data: data);
        CheckSamples(data: LooseFootedSpritsailMkBBakedAsset.Create());
        float scale = LooseFootedSpritsailFittingGeometry.BaseScale(
            data: data,
            fullGaffHeight: 8.8f
        );
        foreach (float mastRadius in new[] { 0.1f, 0.3f, 0.6f })
        foreach (float height in new[] { 0.2f, 1f, 3f })
        foreach (float width in new[] { 0.2f, 1f, 3f })
        {
            var rest = LooseFootedSpritsailFittingGeometry.Pose(
                data: data,
                baseScale: scale,
                height: height,
                width: width,
                mastRadius: mastRadius
            );
            var cloth = new LooseFootedSpritsailClothGeometry(data: data, rest: rest);
            var throat = cloth.Throat;
            var tack = cloth.Tack;
            var peak = cloth.Peak;
            var clew = cloth.Clew;
            var tie = LooseFootedSpritsailTransform.InversePoint(matrix: rest.Sprit, point: peak);
            CheckClearance(data: data, rest: rest, cloth: cloth);
            CheckSamples(data: data, restPose: rest);
            var guide = new Vector3(x: 0, y: 5, z: 0);
            Check(
                cloth.CanRoute(guide: guide),
                "Fitted authored motion must shorten a masthead halyard."
            );
            Check(
                !cloth.CanRoute(guide: cloth.Pivot - Vector3.up),
                "A guide below the pivot cannot raise the sprit."
            );
            foreach (
                float deployment in new[]
                {
                    0f,
                    0.125f,
                    0.25f,
                    0.375f,
                    0.5f,
                    0.625f,
                    0.75f,
                    0.875f,
                    1f,
                }
            )
            {
                cloth.Pose(deployment: deployment);
                Near(
                    actual: cloth.Sprit.MultiplyPoint3x4(point: Vector3.zero),
                    expected: cloth.Pivot,
                    message: "Sprit detached from its mounting rod."
                );
                Near(
                    actual: cloth.Sprit.MultiplyPoint3x4(point: tie),
                    expected: cloth.Peak,
                    message: "Peak detached from sprit."
                );
                Near(
                    actual: LooseFootedSpritsailTransform.Scale(matrix: cloth.Sprit),
                    expected: LooseFootedSpritsailTransform.Scale(matrix: rest.Sprit),
                    message: "Brailing stretches the wooden spar."
                );
                foreach (var point in cloth.Vertices)
                {
                    Check(Finite(point: point), "Non-finite cloth vertex.");
                }
                foreach (var triangles in data.Sail.SubmeshTriangles)
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        var a = cloth.Vertices[triangles[i]];
                        var normal = Vector3
                            .Cross(
                                lhs: cloth.Vertices[triangles[i + 1]] - a,
                                rhs: cloth.Vertices[triangles[i + 2]] - a
                            )
                            .normalized;
                        Check(Finite(point: normal), "Non-finite folded normal.");
                    }
                var saved = (Vector3[])cloth.Vertices.Clone();
                cloth.Pose(deployment: 1 - deployment);
                cloth.Pose(deployment: deployment);
                for (int i = 0; i < saved.Length; i++)
                    Near(
                        actual: cloth.Vertices[i],
                        expected: saved[i],
                        message: "Deployment history changed the cloth shape."
                    );
                foreach (float rake in new[] { -15f, 0f, 22f })
                foreach (float trim in new[] { -85f, 0f, 85f })
                {
                    var world =
                        LooseFootedSpritsailSailingGeometry.PivotRotation(
                            center: Vector3.zero,
                            axis: Vector3.right,
                            degrees: rake
                        )
                        * LooseFootedSpritsailSailingGeometry.PivotRotation(
                            center: Vector3.zero,
                            axis: Vector3.up,
                            degrees: trim
                        );
                    Check(
                        LooseFootedSpritsailSailingGeometry.TryWindFrame(
                            throat: world.MultiplyPoint3x4(point: cloth.Throat),
                            tack: world.MultiplyPoint3x4(point: cloth.Tack),
                            peak: world.MultiplyPoint3x4(point: cloth.Peak),
                            clew: world.MultiplyPoint3x4(point: cloth.Clew),
                            out var frame
                        ),
                        "Posed corners lost their wind frame."
                    );
                }
            }
            cloth.Pose(deployment: 0);
            Check(
                cloth.Vertices.Any(p => Math.Abs(p.x) > 0.0001f),
                "Full brail lost its visible folds."
            );
        }
        float smoothed = LooseFootedSpritsailClothGeometry.SmoothLoad(
            current: 1,
            target: -1,
            deltaTime: 0.016f
        );
        Check(smoothed > 0 && smoothed < 1, "Billow snaps when tacking.");
        for (int i = 0; i < 300; i++)
            smoothed = LooseFootedSpritsailClothGeometry.SmoothLoad(
                current: smoothed,
                target: -1,
                deltaTime: 0.016f
            );
        Check(smoothed < -0.99f, "Billow does not settle after tacking.");
        Console.WriteLine(
            "PASS: spritsail authored cloth across independent sizes, deployments and rakes; rigid sprit, authored poses, reversibility and neutral clearance."
        );
    }

    private static void CheckSamples(
        LooseFootedSpritsailAssetData data,
        LooseFootedSpritsailPose? restPose = null
    )
    {
        var rest =
            restPose
            ?? new LooseFootedSpritsailPose
            {
                Sail = data.Sail.RestTransform,
                Sprit = data.Sprit.RestTransform,
            };
        var cloth = new LooseFootedSpritsailClothGeometry(data: data, rest: rest);
        var tack = data.Sail.Vertices[data.Tack];
        var clew = data.Sail.Vertices[data.Clew];
        var throat = data.Sail.Vertices[data.Throat];
        var peak = data.Sail.Vertices[data.Peak];
        for (int step = 0; step <= 8; step++)
        {
            float sample = step * 0.5f;
            int left = (int)sample,
                right = Math.Min(left + 1, 4);
            float blend = sample - left;
            cloth.Pose(deployment: 1 - sample / 4);
            Vector3 Expected(int index) =>
                rest.Sail.MultiplyPoint3x4(
                    point: Vector3.Lerp(
                        a: data.ReefPoses[left].Vertices[index],
                        b: data.ReefPoses[right].Vertices[index],
                        t: blend
                    )
                );
            for (int i = 0; i < cloth.Vertices.Length; i++)
                Near(
                    actual: cloth.VisualVertex(index: i, flutter: Vector4.zero),
                    expected: cloth.Vertices[i],
                    message: "Still cloth must retain its authored pose without added shaping."
                );
            var correction = cloth.Peak - Expected(data.Peak);
            for (int i = 0; i < cloth.Vertices.Length; i++)
            {
                var vertex = data.Sail.Vertices[i];
                float u = Mathf.Clamp01(value: (vertex.z - tack.z) / (clew.z - tack.z));
                float bottom = tack.y + (clew.y - tack.y) * u;
                float top = throat.y + (peak.y - throat.y) * u;
                float v = Mathf.Clamp01(value: (vertex.y - bottom) / (top - bottom));
                Near(
                    actual: cloth.Vertices[i],
                    expected: i == data.Peak ? cloth.Peak : Expected(i) + correction * u * v,
                    message: "Authored sample or midpoint was not reproduced."
                );
            }
        }
    }

    private static void CheckClearance(
        LooseFootedSpritsailAssetData data,
        LooseFootedSpritsailPose rest,
        LooseFootedSpritsailClothGeometry cloth
    )
    {
        var clearance = cloth.Clearance();
        var panels = clearance.Take(clearance.Length - 1).ToArray();
        foreach (var point in data.Sail.Vertices)
            Check(
                panels.Any(b => Inside(box: b, point: rest.Sail.MultiplyPoint3x4(point: point))),
                "Neutral cloth escaped installation clearance."
            );
        var spar = clearance[clearance.Length - 1];
        foreach (var point in data.Sprit.Vertices)
            Check(
                Inside(box: spar, point: rest.Sprit.MultiplyPoint3x4(point: point)),
                "Deployed sprit escaped its oriented clearance box."
            );
        foreach (var panel in panels)
        {
            Check(panel.Size.x <= 0.021f, "Billow must not inflate neutral cloth clearance.");
            Check(
                panel.Center.z + panel.Size.z / 2 <= cloth.Throat.z + 0.0001f,
                "Cloth clearance crosses the mounting mast plane."
            );
        }
        cloth.Pose(deployment: 0.5f);
        var saved = (Vector3[])cloth.Vertices.Clone();
        var again = cloth.Clearance();
        for (int i = 0; i < clearance.Length; i++)
        {
            Near(
                actual: again[i].Center,
                expected: clearance[i].Center,
                message: "Reefing changed installation clearance."
            );
            Near(
                actual: again[i].Size,
                expected: clearance[i].Size,
                message: "Billow changed installation clearance."
            );
        }
        for (int i = 0; i < saved.Length; i++)
            Near(
                actual: cloth.Vertices[i],
                expected: saved[i],
                message: "Computing clearance changed the animated cloth."
            );
        Check(
            cloth.Vertices.Any(p => !panels.Any(b => Inside(box: b, point: p))),
            "Installation panels still encompass the reefing and billow envelope."
        );
        cloth.Pose(deployment: 1);
    }

    private static bool Inside(LooseFootedSpritsailClearance box, Vector3 point)
    {
        var local = LooseFootedSpritsailTransform.InversePoint(
            matrix: LooseFootedSpritsailTransform.TRS(
                pos: box.Center,
                q: box.Rotation,
                s: Vector3.one
            ),
            point: point
        );
        return Inside(bounds: new Bounds(center: Vector3.zero, size: box.Size), point: local);
    }

    private static bool Inside(Bounds bounds, Vector3 point) =>
        point.x >= bounds.min.x - 0.0001f
        && point.x <= bounds.max.x + 0.0001f
        && point.y >= bounds.min.y - 0.0001f
        && point.y <= bounds.max.y + 0.0001f
        && point.z >= bounds.min.z - 0.0001f
        && point.z <= bounds.max.z + 0.0001f;

    private static bool Finite(Vector3 point) =>
        float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.z);

    private static void Near(Vector3 actual, Vector3 expected, string message) =>
        Check((actual - expected).magnitude < 0.0001f, message);

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
