using System;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Sails.FishermansStaysail;
using MoreSailwindSails.Sails.FishermansStaysail.MkA;
using MoreSailwindSails.Sails.FishermansStaysail.MkB;
using MoreSailwindSails.Sails.FishermansStaysail.MkC;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

// Verifies both Dhow mast-pair configurations share a stay identity and fitted geometry.
internal static class DhowChecks
{
    internal static void Run()
    {
        var profile = Dhow.Definition;
        var configurations = profile.Stays.Single().Variants;
        Check(
            configurations.Count == 2
                && configurations.All(v =>
                    v.MountIndex == 128 && v.Label == "mainmast / mizzenmast"
                ),
            "Dhow must register one stay for both mast pairs."
        );
        Check(
            configurations.Select(v => v.Fore).SequenceEqual(new[] { 6, 7 }),
            "Dhow support configurations changed."
        );
        var masts = StaySparMeasurement.Load()[profile.BoatName];
        var anchor = masts[6].Point(configurations[0].ForePoint);
        foreach (var stay in configurations)
        {
            var spar = masts[stay.Fore];
            var fore = spar.Point(stay.ForePoint);
            var aft = masts[stay.Aft].Point(stay.AftPoint);
            Check((fore - anchor).magnitude < 0.0001f, "Equivalent Dhow stays no longer coincide.");
            var axis = (spar.Top - spar.Bottom).normalized;
            float room = Vector3.Dot(fore - spar.Bottom, axis) - 0.15f;
            float span = Vector3.ProjectOnPlane(aft - fore, axis).magnitude;
            float slope = FishermansStaysailInstallationGeometry.HeadSlope(aft - fore, axis);
            foreach (
                var cut in new Func<float, float, FishermansStaysailMeshData>[]
                {
                    FishermansStaysailMkAGeometry.Create,
                    FishermansStaysailMkBGeometry.Create,
                    FishermansStaysailMkCGeometry.Create,
                }
            )
            {
                var unit = cut(1f, slope);
                float ratio = (unit.Corners[0] - unit.Corners[2]).magnitude;
                float width = Math.Min(span - 0.2f, (room - 0.05f) / ratio);
                var mesh = cut(width, slope);
                Check(
                    width > 0
                        && FishermansStaysailInstallationGeometry.FitError(
                            width,
                            (mesh.Corners[0] - mesh.Corners[2]).magnitude,
                            0f,
                            room,
                            span
                        ) == null,
                    "Dhow alternative lost a physically fitting cut."
                );
                var direction = Vector3.ProjectOnPlane(aft - fore, axis).normalized;
                var head = mesh.Corners[1] - mesh.Corners[0];
                Check(
                    Vector3
                        .Cross(
                            (axis * head.x + direction * head.z).normalized,
                            (aft - fore).normalized
                        )
                        .magnitude < 0.00001f,
                    "Dhow cut no longer follows the stay."
                );
            }
        }
        Console.WriteLine(
            "PASS: one Dhow stay identity for two mast pairs, coincident endpoints and all three sail cuts."
        );
    }

    private static void Check(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
    }
}
