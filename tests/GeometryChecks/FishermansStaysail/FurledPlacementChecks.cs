// Verifies that the furled bundle follows the stay rather than offset animated corners.
using System;
using MoreSailwindSails.Sails.FishermansStaysail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail;

internal static class FurledPlacementChecks
{
    internal static void Run()
    {
        foreach (var axis in new[] { Vector3.up, new Vector3(0.3f, 0.9f, 0.2f).normalized })
        foreach (var origin in new[] { Vector3.zero, new Vector3(100, 20, -30) })
        foreach (float rise in new[] { 0f, 3f, 12f })
        foreach (float span in new[] { 3f, 10f, 20f })
        foreach (float fraction in new[] { 0.25f, 0.5f, 1f, 1.2f })
        {
            var across = Vector3.Cross(axis, Vector3.right).normalized;
            var aft = origin + across * span + axis * rise;
            var end = FishermansStaysailFrameGeometry.FurledAftPoint(
                fore: origin,
                aft: aft,
                mastAxis: axis,
                width: span * fraction
            );
            var expected = origin + (aft - origin) * Math.Min(fraction, 1f);
            if ((end - expected).magnitude > 0.0001f)
                throw new Exception(
                    "Furled endpoint left the physical stay or changed fitted span."
                );
            // Reproduce the old floating head: lowered and displaced sideways by the authored pose.
            var floatingHead = expected - axis * 0.15f + Vector3.Cross(axis, across) * 0.5f;
            if ((end - floatingHead).magnitude < 0.5f)
                throw new Exception("Furled endpoint retained the animated head offset.");
        }
        Console.WriteLine(
            "PASS: furled bundle follows physical stay endpoints across widths, slopes, rakes and boat positions."
        );
    }
}
