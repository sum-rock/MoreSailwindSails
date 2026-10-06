using System;
using MoreSailwindSails.Sails.Spritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Exercises rake and lean rejection, numeric noise, axis direction and scale invariance.
internal static class MastAlignmentChecks
{
    internal static void Run()
    {
        foreach (float scale in new[] { 0.01f, 1f, 100f })
        foreach (float direction in new[] { -1f, 1f })
        {
            Check(axis: new Vector3(0, scale * direction, 0), expected: true);
            foreach (float degrees in new[] { 0.05f, 0.2f, 5f, 16f, 45f, 90f })
            {
                float angle = degrees * (float)Math.PI / 180;
                float lateral = (float)Math.Sin(angle) * scale;
                float vertical = (float)Math.Cos(angle) * scale * direction;
                foreach (float side in new[] { -1f, 1f })
                {
                    Check(axis: new Vector3(lateral * side, vertical, 0), expected: degrees < 0.1f);
                    Check(axis: new Vector3(0, vertical, lateral * side), expected: degrees < 0.1f);
                }
            }
        }
        Check(axis: Vector3.zero, expected: false);
        Check(axis: new Vector3(float.NaN, 1, 0), expected: false);
        Check(axis: new Vector3(0, float.PositiveInfinity, 0), expected: false);
        Check(axis: new Vector3(0, 1, float.NegativeInfinity), expected: false);
        Console.WriteLine(
            "PASS: upright spritsail mast alignment, fore/aft rake and lateral lean rejection, transform noise, scale and reversed-axis invariance."
        );
    }

    private static void Check(Vector3 axis, bool expected)
    {
        if (SpritsailMastAlignment.IsUpright(boatLocalAxis: axis) != expected)
            throw new Exception("Unexpected spritsail mast eligibility: " + axis);
    }
}
