using System;
using MoreSailwindSails.Controls;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Controls;

internal static class WinchCaptureChecks
{
    internal static void Run()
    {
        var ray = new Ray(new Vector3(1f, 2f, 3f), new Vector3(0.6f, 0f, 0.8f));
        var unchanged = WinchCaptureGeometry.TransformRay(Matrix4x4.identity, ray, out float unit);
        Near(unchanged.GetPoint(5f), new Vector3(4f, 2f, 7f), "Identity ray changed.");
        Near(unit, 1f, "Identity range changed.");

        // A remote walking model, with a 90-degree turn and unequal axis scales.
        // Explicit matrix entries avoid Unity's native TRS/inverse calls in .NET.
        var toWalk = new Matrix4x4
        {
            m02 = 2f,
            m03 = 400f,
            m11 = 3f,
            m13 = 204f,
            m20 = -4f,
            m23 = -20f,
            m33 = 1f,
        };
        var toWorld = new Matrix4x4
        {
            m02 = -0.25f,
            m03 = -5f,
            m11 = 1f / 3f,
            m13 = -68f,
            m20 = 0.5f,
            m23 = -200f,
            m33 = 1f,
        };
        var walkRay = WinchCaptureGeometry.TransformRay(toWalk, ray, out float scale);
        Near(walkRay.origin, new Vector3(406f, 210f, -24f), "Walking ray origin is wrong.");
        // The old world ray cannot hit this walking-model plane in front of it.
        float originalDistance = (-36f - ray.origin.z) / ray.direction.z;
        if (originalDistance >= 0f)
            throw new Exception("Displaced-plane reproducer no longer misses the original ray.");
        float walkingDistance = (-36f - walkRay.origin.z) / walkRay.direction.z;
        Near(walkingDistance / scale, 5f, "Hit distance must be compared in visual-world units.");
        Near(
            walkRay.GetPoint(walkingDistance),
            new Vector3(414f, 210f, -36f),
            "Missed walking plane."
        );
        Near(
            toWorld.MultiplyPoint3x4(walkRay.GetPoint(walkingDistance)),
            new Vector3(4f, 2f, 7f),
            "Captured point did not return to the visible boat."
        );
        Near(
            walkRay.GetPoint(10f * scale),
            new Vector3(422f, 210f, -48f),
            "Ten-metre capture range was not preserved across model scales."
        );
        Console.WriteLine(
            "PASS: capture ray reaches a displaced walking-model plane; identity, rotated/scaled frames, returned points and world-distance limits. Live collider selection remains an in-game check."
        );
    }

    private static void Near(Vector3 actual, Vector3 expected, string message)
    {
        if ((actual - expected).magnitude > 0.0001f)
            throw new Exception(message);
    }

    private static void Near(float actual, float expected, string message)
    {
        if (Math.Abs(actual - expected) > 0.0001f)
            throw new Exception(message);
    }
}
