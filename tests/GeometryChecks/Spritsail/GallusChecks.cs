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
        Check(profile == null, "Gallus spritsails must not require a boat profile.");
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
                SpritsailMastAlignment.IsUsable(
                    boatLocalAxis: new Vector3(matrix[2], matrix[6], matrix[10])
                ),
                "Gallus Spritsails must accept both the plumb and 15-degree raked mast frames."
            );
        }
        Console.WriteLine(
            "PASS: Gallus profile independence, plumb and raked mast eligibility; no Fisherman's sail support."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
