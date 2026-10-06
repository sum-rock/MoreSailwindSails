using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Sails.FishermansStaysail;
using MoreSailwindSails.Stays.FishermansStay;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

// Checks the sole Caelanor masthead option, physical prerequisites and installed capsules.
internal static class CaelanorChecks
{
    internal static void Run()
    {
        var boat = Caelanor.Definition;
        Check(
            BoatRigCatalog.Find(boatName: "BOAT CAELANOR (192)(Clone)") == boat
                && boat.Stays.Count == 1
                && boat.Stays[0].Variants.Count == 1,
            "Caelanor must offer exactly one Fisherman's Stay."
        );
        var stay = boat.Stays[0].Variants.Single();
        Check(
            stay.MountIndex == 128
                && stay.Fore == 2
                && stay.Aft == 6
                && stay.AlignGuideHeightToAftAnchor,
            "Caelanor stay must join the fore and aft-position main topmasts."
        );
        var masts = StaySparMeasurement.Load()[boat.BoatName];
        var foreSpar = masts[stay.Fore];
        var aftSpar = masts[stay.Aft];
        var forePoint = foreSpar.Point(local: stay.ForePoint);
        var aftPoint = aftSpar.Point(local: stay.AftPoint);
        Check(
            Math.Abs((forePoint - foreSpar.Top).magnitude - 0.02f) < 0.002f
                && Math.Abs((aftPoint - aftSpar.Top).magnitude - 0.02f) < 0.002f,
            "Caelanor anchors must sit 2 cm below rendered topmast tips."
        );
        Check(
            Math.Abs(
                FishermansStaysailInstallationGeometry.HeadSlope(
                    stay: aftPoint - forePoint,
                    mastAxis: foreSpar.Top - foreSpar.Bottom
                ) - 13.462291f
            ) < 0.001f,
            "Caelanor foremast-relative stay slope changed."
        );
        foreach (var (top, middle, lower) in new[] { (17, 2, 1), (18, 6, 5), (19, 4, 3) })
            Check(
                boat.Sections(section: top).SequenceEqual(new[] { top, middle, lower })
                    && boat.HalyardSources(mast: top).SequenceEqual(new[] { top }),
                "Caelanor mast ancestry or independent T'gallant reef sources changed."
            );
        foreach (
            var (point, center, height, radius) in new[]
            {
                (stay.ForePoint, -2.86418533f, 7.99926853f, 0.11f),
                (stay.AftPoint, -2.85489726f, 8.01927376f, 0.13f),
            }
        )
            Check(
                FishermansStayGeometry.OnSpar(
                    point: point,
                    center: new Vector3(0f, 0f, center),
                    direction: 2,
                    height: height,
                    radius: radius
                ),
                "Caelanor masthead anchor left its installed mast capsule."
            );
        int[] sections = { 1, 2, 3, 4, 5, 6, 17, 18, 19 };
        for (int mask = 0; mask < 1 << sections.Length; mask++)
        {
            var active = new HashSet<int>(
                sections.Where((section, index) => (mask & (1 << index)) != 0)
            );
            bool available =
                stay.Required.All(active.Contains) && !stay.Forbidden.Any(active.Contains);
            Check(
                available == new[] { 1, 2, 5, 6 }.All(active.Contains),
                "Caelanor stay requires both full support chains and must retain optional T'gallants."
            );
        }
        Check(
            !boat.MastPairs(foreIndex: 4).Any(),
            "Caelanor Midmast gained an unintended sail support pair."
        );
        Console.WriteLine(
            "PASS: one Caelanor topmast stay across 512 support combinations, installed capsules and independent T'gallants."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
