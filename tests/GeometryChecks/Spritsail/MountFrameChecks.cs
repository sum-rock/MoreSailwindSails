using System;
using MoreSailwindSails.Sails.Spritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Exercises scale-preserving relative coordinates and envelope invalidation without Unity transforms.
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

        var state = new SpritsailMountFitState(luffCount: 33);
        var luff = new Vector3[33];
        var radii = new float[7];
        for (int i = 0; i < luff.Length; i++)
            luff[i] = expectedBone + Vector3.right * (3 - 6f * i / 32);
        for (int i = 0; i < radii.Length; i++)
            radii[i] = 0.3f;
        var origin = rigid.MultiplyPoint3x4(point: mastCommon);
        state.Commit(
            parts: SpritsailMountGeometry.AllParts,
            luff: luff,
            origin: origin,
            axis: Vector3.right,
            currentRadii: radii
        );
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
            Require(
                value: state.Changes(
                    luff: luff,
                    origin: origin,
                    axis: Vector3.right,
                    currentRadii: radii,
                    supportChanged: false,
                    reasons: out _
                ) == 0,
                message: "Moving the draw frame must not invalidate fitting inputs."
            );
        }
        Require(
            value: state.Changes(
                luff: luff,
                origin: origin + Vector3.up * 0.001f,
                axis: Vector3.right,
                currentRadii: radii,
                supportChanged: false,
                reasons: out _
            ) == 0x7f00,
            message: "Real mast-relative movement must still refit the ropes immediately."
        );

        var sample = new SpritsailMountEnvelopeSample();
        bool Hit(Vector3 point, Vector3 right, Vector3 forward, float fallback) =>
            sample.TryGet(
                currentOrigin: point,
                currentRight: right,
                currentForward: forward,
                currentFallback: fallback,
                result: out _
            );
        Require(
            value: !Hit(
                point: Vector3.zero,
                right: Vector3.right,
                forward: Vector3.forward,
                fallback: 0.2f
            ),
            message: "An uninitialized envelope must sample the mast."
        );
        sample.Store(
            currentOrigin: Vector3.zero,
            currentRight: Vector3.right,
            currentForward: Vector3.forward,
            currentFallback: 0.2f,
            result: 0.3f
        );
        Require(
            value: sample.TryGet(
                currentOrigin: Vector3.zero,
                currentRight: Vector3.right,
                currentForward: Vector3.forward,
                currentFallback: 0.2f,
                result: out float cached
            )
                && cached == 0.3f,
            message: "An unchanged sampling ring must retain its complete envelope."
        );
        foreach (float height in new[] { 0.00004f, 0.00008f })
            Require(
                value: Hit(
                    point: Vector3.up * height,
                    right: Vector3.right,
                    forward: Vector3.forward,
                    fallback: 0.2f
                ),
                message: "Small height changes must accumulate against the last sampled ring."
            );
        Require(
            value: !Hit(
                point: Vector3.up * 0.00012f,
                right: Vector3.right,
                forward: Vector3.forward,
                fallback: 0.2f
            ),
            message: "Accumulated height change must invalidate the envelope."
        );
        Require(
            value: !Hit(
                point: Vector3.zero,
                right: Vector3.right * 0.5f,
                forward: Vector3.forward,
                fallback: 0.2f
            ),
            message: "Mast scale changes must invalidate the envelope's metre-sized rays."
        );
        Require(
            value: !Hit(
                point: Vector3.zero,
                right: Vector3.forward,
                forward: -Vector3.right,
                fallback: 0.2f
            ),
            message: "Sampling orientation changes must invalidate noncircular mast envelopes."
        );
        Require(
            value: !Hit(
                point: Vector3.zero,
                right: Vector3.right,
                forward: Vector3.forward,
                fallback: 0.25f
            ),
            message: "Capsule fallback changes must invalidate the cached envelope."
        );
        sample = default;
        Require(
            value: !Hit(
                point: Vector3.zero,
                right: Vector3.right,
                forward: Vector3.forward,
                fallback: 0.2f
            ),
            message: "Replacing the mast mesh must discard its envelope."
        );
        Console.WriteLine(
            "PASS: metre-sized relative frames, remote drawing reuse, real mast motion, envelope height/orientation/scale/fallback invalidation and accumulated movement; Unity hierarchy execution needs in-game validation."
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
