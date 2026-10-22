// Checks per-pair donor identities, measured mounting depths, and invalid configuration rejection.
using System;
using System.Linq;
using MoreSailwindSails.Controls;
using MoreSailwindSails.Controls.Configuration;
using MoreSailwindSails.Controls.Configuration.Boats;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Controls;

internal static class SheetFallbackDonorChecks
{
    internal static void Run()
    {
        // Installed assets inspected 2026-10-20: Sailwind 0.39.2, OldChronian 0.6.0,
        // Shattered Seas 0.1.3. Rear depths below are mesh-local; scale is native world scale.
        var donors = new[]
        {
            (BrigControls.Definition, "Mainmast", 5, WinchRole.Left, 0.095856f, 0.8f),
            (BrigControls.Definition, "Mizzenmast", 7, WinchRole.Left, 0.095856f, 0.8f),
            (KakamControls.Definition, "Mainmast", 5, WinchRole.Left, 0.044704f, 1f),
            // Junk donor inspected 2026-10-21; rear depth uses native Z scale.
            (JunkControls.Definition, "Forward", 10, WinchRole.Left, 0.044704f, 1.34204996f),
            (JunkControls.Definition, "Aft", 10, WinchRole.Left, 0.044704f, 1.34204996f),
            // Jong donor inspected 2026-10-21.
            (JongControls.Definition, "Forward", 2, WinchRole.Left, 0.044704f, 1.20000005f),
            (JongControls.Definition, "Midships", 2, WinchRole.Left, 0.044704f, 1.20000005f),
            (JongControls.Definition, "Aft", 2, WinchRole.Left, 0.044704f, 1.20000005f),
            // Baghala donor inspected 2026-10-21; rear depth uses native Z scale.
            (LargeDhowControls.Definition, "Forward", 2, WinchRole.Left, 0.054718f, 1.19999981f),
            (LargeDhowControls.Definition, "Midships", 2, WinchRole.Left, 0.054718f, 1.19999981f),
            (LargeDhowControls.Definition, "Stern", 2, WinchRole.Left, 0.054718f, 1.19999981f),
            (SanbuqControls.Definition, "Stern", 12, WinchRole.Left, 0.054718f, 1.19391859f),
            (SanbuqControls.Definition, "Midships", 12, WinchRole.Left, 0.054718f, 1.19391859f),
            (CogControls.Definition, "Stern", 8, WinchRole.Left, 0.053125f, 1.05000019f),
            (DhowControls.Definition, "Stern", 6, WinchRole.Left, 0.054718f, 1.16000009f),
            (GlorianaControls.Definition, "Mainmast", 2, WinchRole.Left, 0.130912f, 0.7f),
            (GlorianaControls.Definition, "MainmastForward", 2, WinchRole.Left, 0.130912f, 0.7f),
            (ChronianControls.Definition, "Mainmast", 4, WinchRole.Left, 0.130912f, 0.6f),
            (ChronianControls.Definition, "MainmastForward", 4, WinchRole.Left, 0.130912f, 0.6f),
            (ChronianControls.Definition, "Mizzenmast", 6, WinchRole.Left, 0.130912f, 0.6f),
            (CaelanorControls.Definition, "Mainmast", 5, WinchRole.Left, 0.130912f, 0.6f),
            (ShroudControls.Definition, "Foremast", 6, WinchRole.Reef, 0.088011f, 1f),
            (ShroudControls.Definition, "Midships", 6, WinchRole.Reef, 0.088011f, 1f),
            (ShroudControls.Definition, "Aft", 6, WinchRole.Reef, 0.088011f, 1f),
            (ShroudControls.Definition, "Stern", 6, WinchRole.Reef, 0.088011f, 1f),
        };
        foreach (var (boat, name, mast, role, rearDepth, scale) in donors)
        {
            var pair = boat.SheetFallbacks.Single(f => f.Name == name).Fallback;
            Check(
                pair.TemplateMast == mast && pair.TemplateRole == role && pair.TemplateIndex == 0,
                $"Unexpected donor for {boat.BoatName}/{name}."
            );
            Check(
                boat.Boat.MastParents.ContainsKey(pair.TemplateMast)
                    && Math.Abs(pair.SourceNormal.magnitude - 1f) < 0.00001f
                    && Math.Abs(pair.Offset - rearDepth * scale) < 0.000001f,
                $"Donor mounting measurements differ from the installed model: {boat.BoatName}/{name}."
            );
            foreach (var seat in new[] { pair.Port, pair.Starboard })
            {
                // The donor's rear face must meet the authored surface after normal alignment.
                var rear =
                    seat.Position(offset: pair.Offset) - seat.Normal.normalized * rearDepth * scale;
                Check(
                    (rear - seat.Contact).magnitude < 0.000001f,
                    "Donor floats above or penetrates its contact."
                );
            }
        }

        SheetFallbackPair Pair(
            int mast = 5,
            WinchRole role = WinchRole.Left,
            int index = 0,
            Vector3? normal = null,
            float offset = 0.05f
        ) =>
            new SheetFallbackPair(
                templateMast: mast,
                templateRole: role,
                templateIndex: index,
                sourceNormal: normal ?? Vector3.forward,
                offset: offset,
                port: new SheetFallbackSeat(contact: Vector3.zero, normal: Vector3.up),
                starboard: new SheetFallbackSeat(contact: Vector3.right, normal: Vector3.up)
            );

        foreach (
            var invalid in new Func<SheetFallbackPair>[]
            {
                () => Pair(mast: -1),
                () => Pair(mast: 128),
                () => Pair(role: (WinchRole)99),
                () => Pair(index: -1),
                () => Pair(normal: Vector3.zero),
                () => Pair(normal: new Vector3(float.NaN, 0f, 1f)),
                () => Pair(normal: new Vector3(0f, float.PositiveInfinity, 1f)),
                () => Pair(offset: -1f),
                () => Pair(offset: float.NaN),
                () => Pair(offset: float.PositiveInfinity),
            }
        )
            Reject(action: () => invalid());
        Check(
            Pair(role: WinchRole.Right, index: 1, offset: 0f).InvalidSide == null,
            "A valid alternate donor slot or zero-depth model was rejected."
        );
        Reject(action: () =>
            new BoatControlDefinition(
                boat: KakamControls.Definition.Boat,
                sheetFallbacks: new[]
                {
                    new SheetFallbackDefinition(name: "Unknown", fallback: Pair(mast: 127)),
                }
            )
        );
        Console.WriteLine(
            "PASS: all twenty-five sheet donor selections, native-scale rear depths, surface contacts and invalid donor configuration."
        );
    }

    private static void Reject(Action action)
    {
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            return;
        }
        throw new Exception("Invalid sheet donor configuration was accepted.");
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
