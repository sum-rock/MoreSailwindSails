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
            var neutralThroat = neutralCloth.MultiplyPoint3x4(
                point: data.Sail.Vertices[data.Throat]
            );
            var neutralTack = neutralCloth.MultiplyPoint3x4(point: data.Sail.Vertices[data.Tack]);
            var neutralPeak = neutralCloth.MultiplyPoint3x4(point: data.Sail.Vertices[data.Peak]);
            var neutralClew = neutralCloth.MultiplyPoint3x4(point: data.Sail.Vertices[data.Clew]);
            Check(
                LooseFootedSpritsailSailingGeometry.TryWindFrame(
                    throat: neutralThroat,
                    tack: neutralTack,
                    peak: neutralPeak,
                    clew: neutralClew,
                    out var neutralWind
                ),
                "Fitted sail has no aerodynamic frame."
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
                Check(
                    LooseFootedSpritsailSailingGeometry.TryWindFrame(
                        throat: throat,
                        tack: tack,
                        peak: peak,
                        clew: clew,
                        out var wind
                    ),
                    "Trimmed sail lost its wind frame."
                );
                Near(actual: wind.Axis, expected: axis);
                Near(actual: wind.Normal.magnitude, expected: 1);
                Near(actual: Vector3.Dot(lhs: wind.Normal, rhs: wind.Axis), expected: 0);
                Near(
                    actual: wind.Center,
                    expected: travel.MultiplyPoint3x4(point: neutralWind.Center)
                );
                var chord = Vector3.Cross(lhs: wind.Normal, rhs: wind.Axis);
                // Native wind sensors require right=chord and forward=mast, including on a rake.
                Near(actual: Vector3.Dot(lhs: chord, rhs: wind.Normal), expected: 0);
                Check(
                    Vector3.Dot(lhs: chord, rhs: peak + clew - throat - tack) > 0,
                    "The native sensor's right axis points away from the sail chord."
                );
                foreach (float tackSign in new[] { -1f, 1f })
                {
                    var incoming = wind.Normal * tackSign;
                    Near(
                        actual: LooseFootedSpritsailSailingGeometry.ForceDirection(
                            wind: incoming,
                            normal: wind.Normal
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
        CheckCentroid();
        Check(
            !LooseFootedSpritsailSailingGeometry.TryWindFrame(
                throat: Vector3.zero,
                tack: Vector3.zero,
                peak: Vector3.right,
                clew: Vector3.one,
                out _
            ),
            "Zero luff must suppress propulsion."
        );
        Check(
            !LooseFootedSpritsailSailingGeometry.TryWindFrame(
                throat: Vector3.up,
                tack: Vector3.zero,
                peak: Vector3.up * 2,
                clew: Vector3.up * 3,
                out _
            ),
            "Collinear corners must suppress propulsion."
        );
        Check(
            !LooseFootedSpritsailSailingGeometry.TryWindFrame(
                throat: new Vector3(float.NaN, 1, 0),
                tack: Vector3.zero,
                peak: Vector3.one,
                clew: Vector3.right,
                out _
            ),
            "Invalid geometry must suppress propulsion."
        );
        Check(
            LooseFootedSpritsailSailingGeometry.PowerMultiplier > 0.75f
                && LooseFootedSpritsailSailingGeometry.PowerMultiplier < 0.85f,
            "Equal-area spritsail power must fall between native junk and gaff power."
        );
        Console.WriteLine(
            "PASS: spritsail travel on 135 size/rake/tack combinations, fixed mast pivot, spar/clew alignment, aerodynamic centroid and invalid geometry."
        );
    }

    private static void CheckCentroid()
    {
        Check(
            LooseFootedSpritsailSailingGeometry.TryWindFrame(
                throat: new Vector3(x: 0, y: 4, z: 0),
                tack: Vector3.zero,
                peak: new Vector3(x: 0, y: 4, z: -6),
                clew: new Vector3(x: 0, y: 0, z: -2),
                out var wind
            ),
            "Trapezoid must have a wind frame."
        );
        Near(actual: wind.Center, expected: new Vector3(x: 0, y: 7f / 3, z: -13f / 6));
        Near(actual: wind.Normal, expected: Vector3.left);
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
