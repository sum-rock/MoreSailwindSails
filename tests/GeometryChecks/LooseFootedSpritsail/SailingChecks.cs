// Exercises live travel and aerodynamic axes across fitted sizes, offsets and raked masts.
using System;
using MoreSailwindSails.Sails.LooseFootedSpritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.LooseFootedSpritsail;

internal static class SailingChecks
{
    internal static void Run()
    {
        var data = LooseFootedSpritsailMkABakedAsset.Create();
        float scale = LooseFootedSpritsailFittingGeometry.BaseScale(
            data: data,
            fullGaffHeight: 8.8f
        );
        foreach (float height in new[] { 0.2f, 1f, 3f })
        foreach (float width in new[] { 0.2f, 1f, 3f })
        foreach (float rake in new[] { -15f, 0f, 22f })
        {
            var pose = LooseFootedSpritsailFittingGeometry.Pose(
                data: data,
                baseScale: scale,
                height: height,
                width: width,
                mastRadius: 0.3f
            );
            var center = new Vector3(x: 0.25f, y: 1.5f, z: -0.12f);
            var mast = LooseFootedSpritsailSailingGeometry.PivotRotation(
                center: Vector3.zero,
                axis: Vector3.right,
                degrees: rake
            );
            var neutral = Matrix4x4.Translate(vector: center) * mast;
            var axis = mast.MultiplyVector(vector: Vector3.up);
            var neutralCloth = neutral * pose.Sail;
            var stationaryPlate = neutral * pose.RotationPlate;
            var neutralClew = neutralCloth.MultiplyPoint3x4(point: data.Sail.Vertices[data.Clew]);
            var neutralWind = LooseFootedSpritsailSailingGeometry.FitWindCenter(
                authored: data.WindCenter,
                sail: neutralCloth,
                resizeScale: Vector3.one
            );
            var peakTie = LooseFootedSpritsailTransform.InversePoint(
                matrix: data.Sprit.RestTransform,
                point: LooseFootedSpritsailFittingGeometry.Corner(data: data, index: data.Peak)
            );
            foreach (float angle in new[] { -85f, -35f, 0f, 35f, 85f })
            {
                var travel = LooseFootedSpritsailSailingGeometry.PivotRotation(
                    center: center,
                    axis: axis,
                    degrees: angle
                );
                var cloth = travel * neutralCloth;
                var throat = cloth.MultiplyPoint3x4(point: data.Sail.Vertices[data.Throat]);
                var tack = cloth.MultiplyPoint3x4(point: data.Sail.Vertices[data.Tack]);
                var peak = cloth.MultiplyPoint3x4(point: data.Sail.Vertices[data.Peak]);
                var clew = cloth.MultiplyPoint3x4(point: data.Sail.Vertices[data.Clew]);
                Near(actual: travel.MultiplyPoint3x4(point: center), expected: center);
                Near(
                    actual: travel.MultiplyPoint3x4(point: center + axis * 7),
                    expected: center + axis * 7
                );
                Near(actual: (throat - tack).magnitude, expected: 8.8f * height);
                Near(
                    actual: (travel * neutral * pose.Sprit).MultiplyPoint3x4(point: peakTie),
                    expected: peak
                );
                // Both native ropes use their own zero-offset endpoint beneath this same tie.
                var sheet = cloth.MultiplyPoint3x4(point: data.SheetLineAttachment);
                Near(
                    actual: sheet,
                    expected: cloth.MultiplyPoint3x4(point: data.Sail.Vertices[data.SheetVertex])
                );
                Near(
                    actual: (sheet - clew).magnitude,
                    expected: (
                        neutralCloth.MultiplyPoint3x4(point: data.SheetLineAttachment) - neutralClew
                    ).magnitude
                );
                var wind = LooseFootedSpritsailSailingGeometry.FitWindCenter(
                    authored: data.WindCenter,
                    sail: cloth,
                    resizeScale: Vector3.one
                );
                Near(actual: (wind.Rotation * Vector3.forward), expected: axis);
                Near(actual: (wind.Rotation * Vector3.up).magnitude, expected: 1);
                Near(
                    actual: Vector3.Dot(
                        lhs: (wind.Rotation * Vector3.up),
                        rhs: (wind.Rotation * Vector3.forward)
                    ),
                    expected: 0
                );
                Near(
                    actual: wind.Position,
                    expected: travel.MultiplyPoint3x4(point: neutralWind.Position)
                );
                var chord = Vector3.Cross(
                    lhs: (wind.Rotation * Vector3.up),
                    rhs: (wind.Rotation * Vector3.forward)
                );
                // Native wind sensors require right=chord and forward=mast, including on a rake.
                Near(
                    actual: Vector3.Dot(lhs: chord, rhs: (wind.Rotation * Vector3.up)),
                    expected: 0
                );
                Check(
                    Vector3.Dot(lhs: chord, rhs: peak + clew - throat - tack) > 0,
                    "The native sensor's right axis points away from the sail chord."
                );
                foreach (float tackSign in new[] { -1f, 1f })
                {
                    var incoming = (wind.Rotation * Vector3.up) * tackSign;
                    Near(
                        actual: LooseFootedSpritsailSailingGeometry.ForceDirection(
                            wind: incoming,
                            normal: (wind.Rotation * Vector3.up)
                        ),
                        expected: incoming
                    );
                }
                // Counter-transforming the fixed plate into the rotating hierarchy must
                // recover its neutral world position, even with a displaced hinge origin.
                var platePoint = stationaryPlate.MultiplyPoint3x4(
                    point: data.RotationPlate.Vertices[0]
                );
                var plateInMovingRoot = LooseFootedSpritsailTransform.InversePoint(
                    matrix: travel,
                    point: platePoint
                );
                Near(
                    actual: travel.MultiplyPoint3x4(point: plateInMovingRoot),
                    expected: platePoint
                );
            }
        }
        ClothChecks.Run();
        FlutterChecks.Run();
        CheckScaledCenters();
        CheckTackPower();
        Check(
            LooseFootedSpritsailSailingGeometry.PowerMultiplier > 0.75f
                && LooseFootedSpritsailSailingGeometry.PowerMultiplier < 0.85f,
            "Equal-area spritsail power must fall between native junk and gaff power."
        );
        Console.WriteLine(
            "PASS: spritsail travel on 135 size/rake/tack combinations, fixed mast pivot, spar/clew alignment, fixed authored sensor poses."
        );
    }

    private static void CheckScaledCenters()
    {
        foreach (
            var data in new[]
            {
                LooseFootedSpritsailMkABakedAsset.Create(),
                LooseFootedSpritsailMkBBakedAsset.Create(),
                LooseFootedSpritsailMkCBakedAsset.Create(),
                LooseFootedSpritsailMkDBakedAsset.Create(),
            }
        )
        {
            float baseScale = LooseFootedSpritsailFittingGeometry.BaseScale(
                data: data,
                fullGaffHeight: 8.8f
            );
            foreach (float height in new[] { 0.2f, 1f, 1.25f, 3f })
            foreach (float width in new[] { 0.2f, 1f, 1.25f, 3f })
            foreach (bool flipped in new[] { false, true })
            foreach (float rake in new[] { -15f, 0f, 22f })
            {
                var pose = LooseFootedSpritsailFittingGeometry.Pose(
                    data: data,
                    baseScale: baseScale,
                    height: height,
                    width: width,
                    mastRadius: 0.3f
                );
                var mirror = Matrix4x4.Scale(
                    vector: LooseFootedSpritsailFittingGeometry.MirrorScale(flipped: flipped)
                );
                var mast = LooseFootedSpritsailSailingGeometry.PivotRotation(
                    center: Vector3.zero,
                    axis: Vector3.right,
                    degrees: rake
                );
                var fitted =
                    Matrix4x4.Translate(vector: new Vector3(x: 0.25f, y: 1.5f, z: -0.12f))
                    * mast
                    * mirror
                    * pose.Sail;
                // Include a distinct depth scale: every component of the resize parent
                // must be cancelled, even though only height/width drive cloth fitting.
                var resizeScale = new Vector3(x: 0.7f, y: height, z: width);
                var resize = Matrix4x4.Scale(vector: resizeScale);
                foreach (
                    var authored in new[]
                    {
                        data.WindCenter,
                        data.WindCenterMain ?? data.WindCenter,
                    }
                )
                {
                    var sensor = LooseFootedSpritsailSailingGeometry.FitWindCenter(
                        authored: authored,
                        sail: fitted,
                        resizeScale: resizeScale
                    );
                    Near(
                        actual: resize.MultiplyPoint3x4(point: sensor.Position),
                        expected: fitted.MultiplyPoint3x4(point: authored.Position)
                    );
                    Near(
                        actual: sensor.Rotation * Vector3.forward,
                        expected: mast.MultiplyVector(vector: Vector3.up)
                    );
                    Near(
                        actual: sensor.Rotation * Vector3.right,
                        expected: mast.MultiplyVector(vector: Vector3.back)
                    );
                    Near(
                        actual: sensor.Rotation * Vector3.up,
                        expected: mast.MultiplyVector(vector: Vector3.left)
                    );
                }
                var cloth = new LooseFootedSpritsailClothGeometry(data: data, rest: pose);
                var full = LooseFootedSpritsailSailingGeometry.FitWindCenter(
                    authored: data.WindCenter,
                    sail: fitted,
                    resizeScale: resizeScale
                );
                var main = LooseFootedSpritsailSailingGeometry.FitWindCenter(
                    authored: data.WindCenterMain ?? data.WindCenter,
                    sail: fitted,
                    resizeScale: resizeScale
                );
                if (data.HasTopsail)
                    Check(
                        Vector3.Dot(
                            lhs: resize.MultiplyVector(vector: main.Position - full.Position),
                            rhs: mast.MultiplyVector(vector: Vector3.up)
                        ) < 0,
                        "Main-only marker must lower the effort center along the mast."
                    );
                // Boundary and reverse travel share the cloth's visibility rule.
                foreach (
                    float deployment in new[]
                    {
                        1f,
                        0.8001f,
                        0.8f,
                        0.7999f,
                        0.4f,
                        0f,
                        0.8f,
                        0.8001f,
                        1f,
                    }
                )
                {
                    cloth.Pose(deployment: deployment);
                    Check(
                        cloth.TopsailVisible == (data.HasTopsail && deployment > 0.8f),
                        "Topsail boundary changed."
                    );
                    Near(
                        actual: resize.MultiplyPoint3x4(point: full.Position),
                        expected: fitted.MultiplyPoint3x4(point: data.WindCenter.Position)
                    );
                }
            }
        }
        Console.WriteLine(
            "PASS: Mk.A–D resize-local centers preserve fitted positions/axes through independent resizing, offsets, rake, flips and the topsail boundary."
        );
    }

    private static void CheckTackPower()
    {
        foreach (float angle in new[] { -85f, 0f, 35f, 180f })
        foreach (bool flipped in new[] { false, true })
        {
            var rotation = LooseFootedSpritsailSailingGeometry.PivotRotation(
                center: Vector3.zero,
                axis: Vector3.up,
                degrees: angle
            );
            var frame =
                rotation
                * Matrix4x4.Scale(
                    vector: LooseFootedSpritsailFittingGeometry.MirrorScale(flipped: flipped)
                );
            var side = frame.MultiplyVector(vector: Vector3.right);
            foreach (float airflow in new[] { -8f, 0f, 8f })
            {
                float expected = (flipped ? airflow < 0 : airflow > 0) ? 0.9f : 1f;
                float multiplier = LooseFootedSpritsailSailingGeometry.TackPowerMultiplier(
                    wind: rotation.MultiplyVector(vector: Vector3.right * airflow),
                    spritSide: side
                );
                Near(actual: multiplier, expected: expected);
                foreach (float reef in new[] { 0f, 0.4f, 1f })
                    Near(actual: 0.8f * reef * multiplier, expected: 0.8f * reef * expected);
            }
            Near(
                actual: LooseFootedSpritsailSailingGeometry.TackPowerMultiplier(
                    wind: Vector3.up * 8,
                    spritSide: side
                ),
                expected: 1
            );
        }
    }

    private static void Near(Vector3 actual, Vector3 expected) =>
        Check((actual - expected).sqrMagnitude < 1e-7f, $"Expected {expected}, got {actual}.");

    private static void Near(float actual, float expected) =>
        Check(Math.Abs(actual - expected) < 1e-4f, $"Expected {expected}, got {actual}.");

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
