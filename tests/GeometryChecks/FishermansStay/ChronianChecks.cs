using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Sails.FishermansStaysail;
using MoreSailwindSails.Stays.FishermansStay;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

// Checks Chronian's two authored stays against every support combination.
internal static class ChronianChecks
{
    internal static void Run()
    {
        var boat = Chronian.Definition;
        Check(
            ReferenceEquals(BoatRigCatalog.Find(boatName: "BOAT CHRONIAN (187)(Clone)"), boat)
                && boat.Stays.Count == 2
                && boat.Stays.All(group => group.Variants.Count == 1),
            "Chronian must offer exactly two Fisherman's Stays."
        );
        var forward = boat.Stays[0].Variants.Single();
        Check(
            forward.Fore == 3
                && forward.Aft == 5
                && forward.AlignGuideHeightToAftAnchor
                && forward.MountIndex == 128,
            "Chronian stays must connect adjacent named Top sections."
        );
        var aft = boat.Stays[1].Variants.Single();
        Check(
            aft.MountIndex == 129
                && aft.Fore == 4
                && aft.Aft == 7
                && aft.Donor == forward.Donor
                && aft.AlignGuideHeightToAftAnchor
                && aft.Forbidden.SequenceEqual(new[] { 14 }),
            "Chronian aft stay must retain the existing prototype, physical supports and scoped exclusion."
        );
        var masts = StaySparMeasurement.Load()[boat.BoatName];
        foreach (var stay in new[] { forward, aft })
        {
            var foreSpar = masts[stay.Fore];
            var aftSpar = masts[stay.Aft];
            var forePoint = foreSpar.Point(local: stay.ForePoint);
            var aftPoint = aftSpar.Point(local: stay.AftPoint);
            float slope = FishermansStaysailInstallationGeometry.HeadSlope(
                stay: aftPoint - forePoint,
                mastAxis: foreSpar.Top - foreSpar.Bottom
            );
            Check(
                Math.Abs(slope - (stay == forward ? 8.482916f : 24.5f)) < 0.001f,
                "Chronian authored stay slope changed."
            );
            if (stay == forward)
                Check(
                    Math.Abs((forePoint - foreSpar.Top).magnitude - 0.02f) < 0.002f
                        && Math.Abs((aftPoint - aftSpar.Top).magnitude - 0.02f) < 0.002f,
                    "Chronian fore/main anchors must sit 2 cm below rendered spar tips."
                );
            else
            {
                // Installed aelasyl STAY_mizenroyalstay line extended to the physical mast axes.
                var origin = new Vector3(0.00000154f, 24.45619965f, -11.46850168f);
                var axis = new Vector3(0.00000014f, 0.41469351f, -0.90996129f).normalized;
                Check(
                    Vector3.Cross(forePoint - origin, axis).magnitude < 0.0001f
                        && Vector3.Cross(aftPoint - origin, axis).magnitude < 0.0001f,
                    "Chronian main/mizzen anchors left the measured native attachment line."
                );
            }
        }
        foreach (var (top, middle, lower) in new[] { (15, 3, 2), (16, 5, 4), (17, 7, 6) })
            Check(
                boat.Sections(section: top).SequenceEqual(new[] { top, middle, lower })
                    && boat.SheetCategory(mast: top) == boat.SheetCategory(mast: lower)
                    && boat.HalyardSources(mast: top).SequenceEqual(new[] { top }),
                "Chronian mast ancestry or independent T'gallant reef sources changed."
            );
        // Installed capsule bounds differ from the rendered end rings; registration
        // must still accept the inset anchors, at both retained fore/main endpoints.
        foreach (
            var (point, center, height) in new[]
            {
                (forward.ForePoint, -4.5f, 12.64099979f),
                (forward.AftPoint, -5.19999981f, 14.18000031f),
            }
        )
            Check(
                FishermansStayGeometry.OnSpar(
                    point: point,
                    center: new Vector3(0f, 0f, center),
                    direction: 2,
                    height: height,
                    radius: 0.2f
                ),
                "Chronian masthead registration would reject its native capsule."
            );
        int[] sections = { 2, 3, 4, 5, 6, 7, 15, 16, 17, 14 };
        for (int mask = 0; mask < 1 << sections.Length; mask++)
        {
            var active = new HashSet<int>(
                sections.Where((section, index) => (mask & (1 << index)) != 0)
            );
            bool Available(FishermansStayVariantDefinition stay) =>
                stay.Required.All(active.Contains) && !stay.Forbidden.Any(active.Contains);
            // Includes incomplete/invalid native combinations during shipyard preview.
            Check(
                Available(forward) == new[] { 2, 3, 4, 5 }.All(active.Contains)
                    && Available(aft)
                        == (new[] { 4, 5, 6, 7 }.All(active.Contains) && !active.Contains(14)),
                "Chronian stay availability lost a supporting section or gained an extra dependency."
            );
        }
        Console.WriteLine(
            "PASS: Chronian has two authored stays across 1024 support/exclusion combinations, optional T'gallants and independent mast controls."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
