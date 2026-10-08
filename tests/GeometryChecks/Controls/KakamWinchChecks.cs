using System;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Controls;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Controls;

// Checks Kakam's aft sheet replacement, symmetric mounting poses and captured reef fallback.
internal static class KakamWinchChecks
{
    internal static void Run()
    {
        var category = Kakam.Definition.SheetCategory(mast: 5);
        Check(
            category == Kakam.Definition.SheetCategory(mast: 6)
                && category.Sources.Count == 0
                && category.Fallback?.InvalidSide == null,
            "Both Kakam mainmast variants must use the authored aft pair."
        );
        var port = category.Fallback.Port;
        var starboard = category.Fallback.Starboard;
        var p = port.Contact + port.Normal.normalized * port.Offset;
        var s = starboard.Contact + starboard.Normal.normalized * starboard.Offset;
        Check(
            (p - new Vector3(-s.x, s.y, s.z)).magnitude < 0.000001f
                && p.x < 0f
                && p.z < -6f
                && (port.Normal - starboard.Normal).magnitude < 0.000001f
                && (port.SourceNormal - starboard.SourceNormal).magnitude < 0.000001f,
            "Kakam sheet mounts must remain opposite and parallel on the aft trim."
        );
        Check(
            Math.Abs(port.Offset - 0.044704f) < 0.000001f
                && port.Offset == starboard.Offset
                && port.TemplateMast == 5
                && starboard.TemplateMast == 5
                && port.TemplateRole == WinchRole.Left
                && starboard.TemplateRole == WinchRole.Right
                && port.TemplateIndex == 0
                && starboard.TemplateIndex == 0,
            "Kakam sheets lost the measured depth or matching native template rotations."
        );
        var templates = new[] { new object(), new object() };
        Check(
            WinchBootstrapPolicy
                .Sheets(
                    sources: category.Sources,
                    port: _ => throw new Exception("Forward native port seat selected."),
                    starboard: _ => throw new Exception("Forward native starboard seat selected."),
                    usable: (object template) => template != null,
                    fallback: () => templates
                )
                .SequenceEqual(templates),
            "Kakam startup must obtain both aft fallback templates."
        );
        var reef = Kakam.Definition.HalyardGroups.Single();
        Check(
            reef.Mast == 7
                && reef.Sources.SequenceEqual(new[] { 7 })
                && reef.Fallback.Valid
                && reef.Fallback.TemplateIndex == 0
                && Math.Abs(reef.Fallback.Offset - 0.063862f * 0.792f) < 0.000001f
                && (
                    reef.Fallback.Contact - new Vector3(-0.000950f, 2.005890f, -4.232710f)
                ).magnitude < 0.000001f,
            "Kakam mizzen must retain its native reef seats before the measured mast-holder fallback."
        );
        Console.WriteLine(
            "PASS: Kakam aft sheet selection, symmetric parallel mounts, fallback startup and measured mizzen halyard fallback."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
