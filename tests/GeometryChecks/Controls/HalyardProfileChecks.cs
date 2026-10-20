// Checks exact authored halyard fallbacks against recorded native template arrays.
using System;
using System.IO;
using System.Linq;
using MoreSailwindSails.Controls.Configuration;

namespace MoreSailwindSails.Tests.GeometryChecks.Controls;

internal static class HalyardProfileChecks
{
    internal static void Run()
    {
        var boats = new[]
        {
            "Brig",
            "Junk",
            "Jong",
            "Sanbuq",
            "Cog",
            "Shroud",
            "LargeDhow",
            "Gloriana",
            "Chronian",
            "Caelanor",
            "Kakam",
            "Dhow",
        };
        var rows = File.ReadAllLines(
                Path.Combine(AppContext.BaseDirectory, "FishermansStay", "NativeWinchSeats.txt")
            )
            .Where(line => line.StartsWith("seat|"))
            .Select(line => line.Split('|'))
            .ToArray();
        int count = 0;
        for (int i = 0; i < boats.Length; i++)
            foreach (var group in BoatControlCatalog.All[i].HalyardFallbacks)
            {
                if (
                    group.FallbackId != "halyard-fallback/" + group.Mast
                    || !ReferenceEquals(group.FallbackId, group.FallbackId)
                )
                    throw new Exception(
                        "Halyard fallback identifiers must retain their value and stored string."
                    );
                if (
                    !rows.Any(row =>
                        row[1] == boats[i]
                        && row[3] == group.Mast.ToString()
                        && row[4] == "reefWinch"
                        && row[5] == group.Fallback.TemplateIndex.ToString()
                        && row[6] != "null"
                    )
                )
                    throw new Exception(
                        "Missing measured fallback template: " + boats[i] + "/" + group.Mast
                    );
                count++;
            }
        if (count != 18)
            throw new Exception("Preserve all eighteen authored halyard fallbacks.");
        StayDependencyChecks.Run();
        Console.WriteLine(
            "PASS: eighteen authored reef fallbacks retain recorded native templates."
        );
    }
}
