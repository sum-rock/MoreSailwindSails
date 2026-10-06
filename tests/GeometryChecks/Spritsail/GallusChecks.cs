using System;
using System.Globalization;
using System.IO;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Sails.Spritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Verifies single-mast family separation and installed Gallus mast eligibility.
internal static class GallusChecks
{
    internal static void Run()
    {
        var profile = BoatRigCatalog.Find(boatName: "BOAT GALLUS (197)(Clone)");
        Check(profile == Gallus.Definition, "Gallus profile was not registered.");
        Check(
            profile.Stays.Count == 0 && profile.Supports.Count == 0,
            "Gallus must not register Fisherman's Stays or Flying Sail support pairs."
        );
        foreach (int mast in new[] { 1, 4 })
            Check(
                profile.Sections(section: mast).SequenceEqual(new[] { mast })
                    && profile.SheetCategory(mast: mast) != null
                    && !profile.MastPairs(foreIndex: mast).Any(),
                "Gallus mast alternatives must remain independent single-mast supports."
            );
        var frames = File.ReadLines(
                Path.Combine(AppContext.BaseDirectory, "FishermansStay", "StayMeasurements.txt")
            )
            .Where(line => line.StartsWith("BOAT GALLUS (197)|"))
            .Select(line => line.Split('|'))
            .ToArray();
        Check(frames.Length == 2, "Gallus requires both installed mast measurements.");
        foreach (var frame in frames)
        {
            var matrix = frame[3]
                .Split(',')
                .Select(value => float.Parse(value, CultureInfo.InvariantCulture))
                .ToArray();
            Check(
                SpritsailMastAlignment.IsUpright(
                    boatLocalAxis: new Vector3(matrix[2], matrix[6], matrix[10])
                ) == (frame[1] == "1"),
                "Gallus Spritsails must accept the plumb mast and reject the 15-degree raked mast."
            );
        }
        Console.WriteLine(
            "PASS: Gallus single-mast profile, plumb Spritsail eligibility, raked rejection and no Fisherman's sail support."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
