using System;
using System.Linq;
using MoreSailwindSails.Controls;
using MoreSailwindSails.Controls.Configuration;
using MoreSailwindSails.Controls.Configuration.Boats;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Controls;

// Checks Kakam's aft sheet replacement, symmetric mounting poses and generated reef fallbacks.
internal static class KakamWinchChecks
{
    internal static void Run()
    {
        var category = KakamControls.Definition.SheetFallbacks.Single();
        Check(
            category.Fallback?.InvalidSide == null,
            "Both Kakam mainmast variants must use the authored aft pair."
        );
        var port = category.Fallback.Port;
        var starboard = category.Fallback.Starboard;
        var p = port.Position(offset: category.Fallback.Offset);
        var s = starboard.Position(offset: category.Fallback.Offset);
        Check(
            (p - new Vector3(-s.x, s.y, s.z)).magnitude < 0.000001f
                && p.x < 0f
                && p.z < -6f
                && (port.Normal - starboard.Normal).magnitude < 0.000001f,
            "Kakam sheet mounts must remain opposite and parallel on the aft trim."
        );
        Check(
            (p - new Vector3(-0.8067125f, 2.7826826f, -6.028539f)).magnitude < 0.00001f,
            "Kakam sheets lost the measured native-template mounting depth."
        );
        var reef = KakamControls.Definition.HalyardFallbacks.Single(group => group.Mast == 7);
        Check(
            reef.Mast == 7
                && reef.Fallback.Valid
                && reef.Fallback.TemplateIndex == 0
                && Math.Abs(reef.Fallback.Offset - 0.063862f * 0.792f) < 0.000001f
                && (
                    reef.Fallback.Contact - new Vector3(-3.87228647e-06f, 2.38216734f, -4.20065403f)
                ).magnitude < 0.000001f,
            "Kakam mizzen must retain its native reef seats before the generated mast fallback."
        );
        Console.WriteLine(
            "PASS: Kakam aft sheet selection, symmetric parallel mounts, and generated mizzen halyard fallback."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
