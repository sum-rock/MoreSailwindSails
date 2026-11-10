// Exercises neutral mast fitting, independent resizing, rigid hardware and attachment preservation.
using System;
using MoreSailwindSails.Sails.LooseFootedSpritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.LooseFootedSpritsail;

internal static class FittingChecks
{
    private static void CheckTopsailFit()
    {
        foreach (
            var data in new[]
            {
                LooseFootedSpritsailMkABakedAsset.Create(),
                LooseFootedSpritsailMkBBakedAsset.Create(),
            }
        )
            Near(
                actual: LooseFootedSpritsailFittingGeometry.TopsailRise(data: data, scale: 2),
                expected: 0
            );
        var cut = LooseFootedSpritsailMkCBakedAsset.Create();
        float rise = LooseFootedSpritsailFittingGeometry.TopsailRise(data: cut, scale: 1);
        if (rise <= 0)
            throw new Exception("Mk.C requires topsail support above the throat.");
        Near(
            actual: LooseFootedSpritsailFittingGeometry.TopsailRise(data: cut, scale: 2),
            expected: rise * 2
        );
        bool Fits(float height, float mast) =>
            LooseFootedSpritsailFittingGeometry.FitsMast(
                luff: 8,
                rise: rise,
                installHeight: height,
                mastHeight: mast
            );
        if (
            !Fits(height: 8, mast: 8 + rise)
            || Fits(height: 8, mast: 8 + rise - 0.01f)
            || Fits(height: 9, mast: 8 + rise)
            || Fits(height: 7, mast: 8 + rise)
        )
            throw new Exception("Mk.C must fit both its tack and deployed topsail on the mast.");
    }

    private static void CheckMirroring()
    {
        foreach (
            var data in new[]
            {
                LooseFootedSpritsailMkABakedAsset.Create(),
                LooseFootedSpritsailMkBBakedAsset.Create(),
                LooseFootedSpritsailMkCBakedAsset.Create(),
            }
        )
        foreach (float size in new[] { 0.5f, 1f, 2f })
        {
            var pose = LooseFootedSpritsailFittingGeometry.Pose(
                data: data,
                baseScale: 1,
                height: size,
                width: 1.3f,
                mastRadius: 0.2f
            );
            var mirror = LooseFootedSpritsailFittingGeometry.MirrorScale(flipped: true);
            foreach (
                var part in new[]
                {
                    pose.Sail,
                    pose.Sprit,
                    pose.Struck,
                    pose.SnotterAssembly,
                    pose.RotationPlate,
                }
            )
            {
                var point = part.MultiplyPoint3x4(point: new Vector3(x: 0.1f, y: 0.2f, z: 0.3f));
                var reflected = Vector3.Scale(a: mirror, b: point);
                Near(actual: reflected.x, expected: -point.x);
                Near(actual: reflected.y, expected: point.y);
                Near(actual: reflected.z, expected: point.z);
                Near(actual: reflected.magnitude, expected: point.magnitude);
                if (
                    (Vector3.Scale(a: mirror, b: reflected) - point).sqrMagnitude > 1e-8f
                    || (
                        Vector3.Scale(
                            a: LooseFootedSpritsailFittingGeometry.MirrorScale(flipped: false),
                            b: point
                        ) - point
                    ).sqrMagnitude > 1e-8f
                )
                    throw new Exception(
                        "Spritsail mirroring must preserve and restore the authored pose."
                    );
            }
        }
    }

    internal static void Run()
    {
        CheckMirroring();
        CheckMastRadius();
        CheckTopsailFit();
        var data = LooseFootedSpritsailMkABakedAsset.Create();
        float scale = LooseFootedSpritsailFittingGeometry.BaseScale(
            data: data,
            fullGaffHeight: 8.8f
        );
        Near(actual: scale, expected: 8.8f / 20.8f);
        var originalPeak = LooseFootedSpritsailFittingGeometry.Corner(data: data, index: data.Peak);
        var reference = LooseFootedSpritsailTransform.InversePoint(
            matrix: data.Sprit.RestTransform,
            point: originalPeak
        );
        foreach (float radius in new[] { 0.025f, 0.1f, 0.3f, 0.75f, 1.5f })
        foreach (float height in new[] { 0.2f, 0.5f, 1f, 2f, 3f })
        foreach (float width in new[] { 0.2f, 0.7f, 1f, 1.8f, 3f })
        {
            var pose = LooseFootedSpritsailFittingGeometry.Pose(
                data: data,
                baseScale: scale,
                height: height,
                width: width,
                mastRadius: radius
            );
            Near(
                actual: pose.Struck.MultiplyPoint3x4(point: Vector3.zero),
                expected: pose.Sprit.MultiplyPoint3x4(point: Vector3.zero)
            );
            Near(
                actual: LooseFootedSpritsailTransform.Scale(matrix: pose.Struck),
                expected: LooseFootedSpritsailTransform.Scale(matrix: pose.Sprit)
            );
            var throat = pose.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[data.Throat]);
            var tack = pose.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[data.Tack]);
            var peak = pose.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[data.Peak]);
            Near(actual: throat.y, expected: 0);
            Near(actual: throat.y - tack.y, expected: 8.8f * height);
            Near(
                actual: throat.z,
                expected: -radius - LooseFootedSpritsailFittingGeometry.Clearance
            );
            Near(actual: Math.Abs(throat.z - peak.z), expected: 14 * scale * width);
            Near(actual: pose.Sprit.MultiplyPoint3x4(point: reference), expected: peak);
            Near(
                actual: LooseFootedSpritsailTransform.Scale(matrix: pose.Sprit).x,
                expected: LooseFootedSpritsailTransform.Scale(matrix: pose.Sprit).y
            );
            Near(
                actual: LooseFootedSpritsailTransform.Scale(matrix: pose.SnotterAssembly).x,
                expected: LooseFootedSpritsailTransform.Scale(matrix: pose.SnotterAssembly).y
            );
            Near(
                actual: LooseFootedSpritsailTransform.Scale(matrix: pose.SnotterAssembly).x,
                expected: LooseFootedSpritsailTransform.Scale(matrix: pose.SnotterAssembly).z
            );
            // Measured from the authored collar alone; the rod reaches closer to the axis.
            float inner =
                0.303631538f * LooseFootedSpritsailTransform.Scale(matrix: pose.SnotterAssembly).x;
            Near(actual: inner, expected: radius + 0.001f);
            // The rod/snotter hinge retains its position relative to the fitted collar.
            var hingeInCollar = LooseFootedSpritsailTransform.InversePoint(
                matrix: pose.SnotterAssembly,
                point: pose.Sprit.MultiplyPoint3x4(point: Vector3.zero)
            );
            var expectedHinge = LooseFootedSpritsailTransform.InversePoint(
                matrix: data.SnotterAssembly.RestTransform,
                point: data.Sprit.RestTransform.MultiplyPoint3x4(point: Vector3.zero)
            );
            Near(actual: hingeInCollar, expected: expectedHinge);
            for (int i = 0; i < LooseFootedSpritsailFittingGeometry.CollisionStrips; i++)
            {
                if (
                    !LooseFootedSpritsailFittingGeometry.CollisionStrip(
                        data: data,
                        pose: pose,
                        column: i,
                        out var center,
                        out var size
                    )
                )
                    throw new Exception("Valid spritsail collision strip disabled.");
                if (
                    size.x <= 0
                    || size.y <= 0
                    || size.z <= 0
                    || center.z + size.z / 2 > throat.z + 1e-5f
                )
                    throw new Exception(
                        "Collision strip crosses the luff/mast or has invalid dimensions."
                    );
            }
            var frame = LooseFootedSpritsailTransform.TRS(
                pos: new Vector3(x: 13, y: -8, z: 2),
                q: new Quaternion(x: 0.1825742f, y: 0.3651484f, z: -0.5477226f, w: 0.7302967f),
                s: new Vector3(x: 1.1f, y: 0.9f, z: 1.3f)
            );
            Near(
                actual: (frame * pose.Sprit).MultiplyPoint3x4(point: reference),
                expected: frame.MultiplyPoint3x4(point: peak)
            );
        }
        foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
        {
            Reject(action: () =>
                LooseFootedSpritsailFittingGeometry.BaseScale(data: data, fullGaffHeight: invalid)
            );
            Reject(action: () =>
                LooseFootedSpritsailFittingGeometry.Pose(
                    data: data,
                    baseScale: scale,
                    height: invalid,
                    width: 1,
                    mastRadius: 0.3f
                )
            );
            Reject(action: () =>
                LooseFootedSpritsailFittingGeometry.Pose(
                    data: data,
                    baseScale: scale,
                    height: 1,
                    width: 1,
                    mastRadius: invalid
                )
            );
        }
        Near(actual: LooseFootedSpritsailFittingGeometry.ClampScale(value: 0.1f), expected: 0.2f);
        Near(actual: LooseFootedSpritsailFittingGeometry.ClampScale(value: 7), expected: 3);
        Console.WriteLine(
            "PASS: 125 spritsail radius/height/width fittings, rigid collar/rod, circular spar, peak alignment, clearance strips and transformed frames."
        );
    }

    private static void CheckMastRadius()
    {
        // A tapered square shaft: the fitting must follow the section, not its full bounds.
        var vertices = new Vector3[8];
        var triangles = new int[24];
        for (int i = 0; i < 4; i++)
        {
            float x = i < 2 ? 1 : -1;
            float z = i == 0 || i == 3 ? 1 : -1;
            vertices[i] = new Vector3(x: x, y: 0, z: z);
            vertices[i + 4] = new Vector3(x: x / 2, y: 10, z: z / 2);
            int next = (i + 1) % 4;
            Array.Copy(
                sourceArray: new[] { i, next, i + 4, next, next + 4, i + 4 },
                sourceIndex: 0,
                destinationArray: triangles,
                destinationIndex: i * 6,
                length: 6
            );
        }
        var matrix = LooseFootedSpritsailTransform.TRS(
            pos: new Vector3(x: 3, y: 4, z: 5),
            q: new Quaternion(x: 0.5f, y: 0.5f, z: 0.5f, w: 0.5f),
            s: new Vector3(x: 2, y: 3, z: 2)
        );
        foreach (float height in new[] { 0f, 2f, 5f, 10f, 11f })
            Near(
                actual: LooseFootedSpritsailFittingGeometry.MastRadius(
                    vertices: vertices,
                    triangles: triangles,
                    matrix: matrix,
                    center: matrix.MultiplyPoint3x4(point: new Vector3(x: 0, y: height, z: 0)),
                    axis: matrix.MultiplyVector(vector: Vector3.up).normalized
                ),
                expected: height > 10 ? 0 : 2 * (1 - height / 20) * (float)Math.Sqrt(2)
            );
        Console.WriteLine(
            "PASS: visible mast radius follows taper, transformed frames and missing sections."
        );
    }

    private static void Reject(Action action)
    {
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            return;
        }
        throw new Exception("Accepted invalid spritsail fitting dimensions.");
    }

    private static void Near(float actual, float expected)
    {
        if (
            !LooseFootedSpritsailFittingGeometry.Finite(value: actual)
            || Math.Abs(actual - expected) > 0.0001f
        )
            throw new Exception($"Expected {expected}, got {actual}.");
    }

    private static void Near(Vector3 actual, Vector3 expected)
    {
        if (
            !LooseFootedSpritsailFittingGeometry.Finite(value: actual.sqrMagnitude)
            || (actual - expected).sqrMagnitude > 1e-8f
        )
            throw new Exception($"Expected {expected}, got {actual}.");
    }
}
