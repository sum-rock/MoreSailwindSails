using System;
using MoreSailwindSails.Sails.Spritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Exercises scale-preserving relative fitting and drawing coordinates without Unity transforms.
internal static class MountFrameChecks
{
    internal static void Run()
    {
        // Inverse of a 90-degree cloth rotation, followed by ancestor stretch (2,3,4).
        // Explicit matrices keep these checks independent of Unity native TRS/inverse calls.
        var linear = new Matrix4x4
        {
            m01 = 3,
            m10 = -2,
            m22 = 4,
            m33 = 1,
        };
        var clothOrigin = new Vector3(1, 2, 3);
        var rigid = SpritsailMountFrame.RigidFromCommon(clothOrigin: clothOrigin, linear: linear);
        Near(actual: rigid.MultiplyPoint3x4(point: clothOrigin), expected: Vector3.zero);
        // A local bone (1,2,3) under cloth scale (1.5,.5,2) and its 90-degree rotation.
        var boneCommon = clothOrigin + new Vector3(-1, 1.5f, 6);
        var expectedBone = new Vector3(4.5f, 2, 24);
        Near(actual: rigid.MultiplyPoint3x4(point: boneCommon), expected: expectedBone);
        var mastCommon = new Vector3(1, 0, 3);
        Near(actual: rigid.MultiplyPoint3x4(point: mastCommon), expected: new Vector3(-6, 0, 0));

        // Large world movement changes only the drawing matrix; stable fit inputs stay reusable.
        foreach (float translation in new[] { 0f, 10000f, 100000f })
        {
            var draw = new Matrix4x4
            {
                m00 = -1,
                m11 = -1,
                m22 = 1,
                m03 = translation - 6,
                m13 = translation + 2,
                m23 = translation + 12,
                m33 = 1,
            };
            Near(
                actual: draw.MultiplyPoint3x4(point: expectedBone),
                expected: new Vector3(translation - 10.5f, translation, translation + 36)
            );
        }
        Console.WriteLine(
            "PASS: metre-sized relative frames preserve scaled/rotated fitting and remote drawing; Unity hierarchy execution needs in-game validation."
        );
    }

    private static void Near(Vector3 actual, Vector3 expected) =>
        Require(
            value: (actual - expected).sqrMagnitude < 1e-8f,
            message: "Relative fitting must preserve the established scaled and rotated geometry."
        );

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message: message);
    }
}
