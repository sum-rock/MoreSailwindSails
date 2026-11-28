// Checks Sh'ba's six saved fittings, installed mast geometry and topmast prerequisites.
using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Stays.FishermansStay;
using MoreSailwindSails.Stays.FishermansStay.Configuration.Boats;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

internal static class ShBaChecks
{
    internal static void Run()
    {
        var boat = ShBaStays.Definition;
        Check(
            boat.Stays.Select(group => group.Label)
                .SequenceEqual(
                    new[]
                    {
                        "Mainmast / mizzenmast",
                        "Foremast / mizzenmast",
                        "Foremast / mainmast",
                    }
                )
                && boat.Stays.Select(group => group.Variants.Count)
                    .SequenceEqual(new[] { 1, 1, 4 }),
            "Sh'ba saved group order or option counts changed."
        );
        var stays = boat.Stays.SelectMany(group => group.Variants).ToArray();
        var endpoints = new[] { (2, 5), (1, 4), (0, 3), (25, 28), (1, 3), (26, 28) };
        int[][] required =
        {
            new[] { 2, 5 },
            new[] { 1, 4 },
            new[] { 0, 3 },
            new[] { 0, 25, 3, 28 },
            new[] { 1, 3 },
            new[] { 1, 26, 3, 28 },
        };
        int[] sections = { 0, 1, 2, 3, 4, 5, 25, 26, 27, 28 };
        var masts = StaySparMeasurement.Load()[boat.BoatName];
        // Visible shaft radii at the authored band heights, independently measured
        // from mesh cross sections (the native capsule radii are 0.15/0.12 m).
        float[] foreRadii =
        {
            0.11900130f,
            0.10724754f,
            0.10255618f,
            0.07969557f,
            0.09959532f,
            0.07951897f,
        };
        float[] aftRadii =
        {
            0.09369782f,
            0.09726744f,
            0.10130936f,
            0.07940975f,
            0.10130936f,
            0.07940975f,
        };
        for (int i = 0; i < stays.Length; i++)
        {
            var stay = stays[i];
            Check(
                stay.MountIndex == 128 + i && (stay.Fore, stay.Aft) == endpoints[i],
                "Sh'ba mount identity or physical endpoint changed."
            );
            Check(
                stay.Donor == 46
                    && !stay.ExtendedGuide
                    && stay.GuideIndex == 0
                    && stay.AlignGuideHeightToAftAnchor,
                "Sh'ba construction references changed."
            );
            var aftSpar = masts[stay.Aft];
            var aft = aftSpar.Point(local: stay.AftPoint);
            var fore = masts[stay.Fore].Point(local: stay.ForePoint);
            var down = (aftSpar.Bottom - aftSpar.Top).normalized;
            Check(
                (aft - aftSpar.Top).magnitude < 0.0001f,
                "Sh'ba stay must attach at the visible aft masthead."
            );
            Check(
                Math.Abs(Vector3.Dot((fore - aft).normalized, down) - Math.Cos(70 * Math.PI / 180))
                    < 0.00001
                    && fore.y < aft.y,
                "Sh'ba stay must descend at the established 70-degree mast-relative angle."
            );
            Check(
                Math.Abs(stay.AftCollarOffset.z + FishermansStayBakedMesh.CollarHalfHeight)
                    < 0.00001f
                    && Math.Abs(stay.ForeCollarRadius - foreRadii[i]) < 0.00001f
                    && Math.Abs(stay.AftCollarRadius - aftRadii[i]) < 0.00001f,
                "Sh'ba bands must fit the visible shafts and remain below the aft tips."
            );
            Check(
                stay.Required.OrderBy(index => index)
                    .SequenceEqual(required[i].OrderBy(index => index))
                    && stay.Forbidden.Count == 0,
                "Sh'ba required support chain or topmast exclusions changed."
            );
            for (int mask = 0; mask < 1 << sections.Length; mask++)
            {
                var active = new HashSet<int>(
                    sections.Where((section, bit) => (mask & (1 << bit)) != 0)
                );
                bool available =
                    stay.Required.All(active.Contains) && !stay.Forbidden.Any(active.Contains);
                Check(
                    available == required[i].All(active.Contains),
                    "Sh'ba fitting must require its supporting sections and ignore unrelated topmasts."
                );
            }
            Check(
                stay.HasRequiredParts(present: index => index == 46 || sections.Contains(index))
                    && !stay.HasRequiredParts(present: sections.Contains),
                "Missing Sh'ba donor assets must leave an unavailable fitting."
            );
        }
        // Appending three groups must not reinterpret existing boat selections.
        var oldSnapshot = new[] { 2, 1, 0, 3 };
        for (int i = 0; i < oldSnapshot.Length + 3; i++)
            Check(
                FishermansStaySaveState.RestoreOption(savedOptions: oldSnapshot, partIndex: i)
                    == (i < oldSnapshot.Length ? oldSnapshot[i] : 0),
                "Old Sh'ba snapshots must retain native choices and default appended stays to None."
            );
        Console.WriteLine(
            "PASS: six Sh'ba stays, 1024 support combinations each, masthead geometry, collars and saved options."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
