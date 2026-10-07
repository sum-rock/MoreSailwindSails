using System;
using MoreSailwindSails.Sails.Spritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Exercises rake and lean acceptance, numeric noise, axis direction and scale invariance.
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
                    Check(axis: new Vector3(lateral * side, vertical, 0), expected: degrees < 90f);
                    Check(axis: new Vector3(0, vertical, lateral * side), expected: degrees < 90f);
                }
            }
        }
        Check(axis: Vector3.zero, expected: false);
        Check(axis: new Vector3(float.NaN, 1, 0), expected: false);
        Check(axis: new Vector3(0, float.PositiveInfinity, 0), expected: false);
        Check(axis: new Vector3(0, 1, float.NegativeInfinity), expected: false);
        foreach (
            var top in new[]
            {
                new Vector3(0, 10, 0),
                new Vector3(0, 10, 3),
                new Vector3(2, 10, -3),
            }
        )
        foreach (float height in new[] { 0f, 2.5f, 10f })
        {
            var expected = top * (height / 10f);
            var loose =
                MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.LooseFootedSpritsailMastInstallationGeometry.AtHeight(
                    bottom: Vector3.zero,
                    top: top,
                    height: height
                );
            var boomed =
                MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.BoomedSpritsailMastInstallationGeometry.AtHeight(
                    bottom: Vector3.zero,
                    top: top,
                    height: height
                );
            if ((loose - expected).magnitude > 0.00001f || (boomed - expected).magnitude > 0.00001f)
                throw new Exception(
                    "Raked/leaning spritsail luff must remain on its mast axis at the saved height."
                );
        }
        Console.WriteLine(
            "PASS: spritsail mast alignment, fore/aft rake and lateral lean acceptance, transform noise, scale and reversed-axis invariance."
        );
    }

    private static void Check(Vector3 axis, bool expected)
    {
        if (SpritsailMastAlignment.IsUsable(boatLocalAxis: axis) != expected)
            throw new Exception("Unexpected spritsail mast eligibility: " + axis);
    }
}
