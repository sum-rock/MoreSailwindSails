using System;
using System.IO;
using System.Linq;
using MoreSailwindSails.BoatRigs;

namespace MoreSailwindSails.Tests.GeometryChecks.Controls;

internal static class NativeSeatProfileChecks
{
    internal static void Run()
    {
        foreach (var boat in BoatRigCatalog.All)
        {
            foreach (
                int fore in boat
                    .Stays.SelectMany(g => g.Variants)
                    .Select(v => v.Fore)
                    .Concat(boat.Supports.SelectMany(s => s.ForeSections))
            )
                Check(
                    boat.SheetCategory(fore) != null,
                    "Missing forward category: " + boat.BoatName + "/" + fore
                );
            foreach (var category in boat.SheetCategories)
            {
                Check(
                    category.PhysicalMasts.Distinct().Count() == category.PhysicalMasts.Length,
                    "Duplicate category member."
                );
                Check(
                    category.Sources.Distinct().Count() == category.Sources.Length,
                    "Duplicate source rig."
                );
                Check(
                    category.PhysicalMasts.All(m => category.Sources.Any(s => s == m)),
                    "Physical variants missing from source pool."
                );
                Check(
                    category.Fallback == null || category.Fallback.InvalidSide == null,
                    "Invalid authored fallback pair."
                );
            }
        }
        Check(
            Brig.Definition.SheetCategory(3) == Brig.Definition.SheetCategory(2),
            "Brig variants split."
        );
        Check(
            Jong.Definition.SheetCategory(2) != Jong.Definition.SheetCategory(3),
            "Jong mainmasts merged."
        );
        Check(
            Leopard.Definition.SheetCategory(6) == Leopard.Definition.SheetCategory(4),
            "Topmast category lost."
        );
        Check(
            Shroud.Definition.SheetCategory(6).Fallback != null,
            "Shroud measured fallback lost."
        );
        var rows = File.ReadAllLines(
                Path.Combine(AppContext.BaseDirectory, "FishermansStay", "NativeWinchSeats.txt")
            )
            .Where(l => l.StartsWith("seat|"))
            .Select(l => l.Split('|'))
            .ToArray();
        foreach (
            string boat in new[]
            {
                "Brig",
                "Junk",
                "Jong",
                "Sanbuq",
                "Cog",
                "Leopard",
                "Shroud",
                "LargeDhow",
            }
        )
            Check(rows.Any(r => r[1] == boat), "Missing boat inventory: " + boat);
        // Verify profile constants against independently extracted shipyard groups.
        var rigs = File.ReadAllLines(
                Path.Combine(AppContext.BaseDirectory, "FishermansStay", "NativeWinchSeats.txt")
            )
            .Where(l => l.StartsWith("rig|"))
            .Select(l => l.Split('|'))
            .ToArray();
        var names = new[]
        {
            "Brig",
            "Junk",
            "Jong",
            "Sanbuq",
            "Cog",
            "Leopard",
            "Shroud",
            "LargeDhow",
        };
        for (int b = 0; b < BoatRigCatalog.All.Length; b++)
        {
            var profile = BoatRigCatalog.All[b];
            foreach (var category in profile.SheetCategories)
            {
                var groups = category
                    .PhysicalMasts.Select(id =>
                        rigs.Single(r => r[1] == names[b] && r[3] == id.ToString())[5]
                    )
                    .Distinct()
                    .ToArray();
                Check(
                    groups.Length == 1,
                    names[b] + "/" + category.Name + " merges separate shipyard groups."
                );
                foreach (var source in category.Sources)
                    Check(
                        rigs.Any(r => r[1] == names[b] && r[3] == source.ToString()),
                        "Missing audited source: " + names[b] + "/" + source
                    );
            }
            foreach (
                var donor in profile
                    .Stays.SelectMany(g => g.Variants)
                    .Select(v => v.Donor)
                    .Distinct()
            )
                Check(
                    !rows.Any(r =>
                        r[1] == names[b] && r[3] == donor.ToString() && r[4] == "midAngleWinch"
                    ),
                    "Stay donor requires an unhandled centre sheet."
                );
        }
        Check(
            rows.Count(r => r[1] == "Shroud" && r[3] == "7" && r[4] == "reefWinch") == 7
                && rows.Count(r => r[1] == "Shroud" && r[3] == "8" && r[4] == "reefWinch") == 5,
            "Re-audit active Shroud mast pin capacity."
        );
        string Seat(string boat, int mast, string role, int index) =>
            rows.Single(r =>
                r[1] == boat
                && r[2] == "native"
                && r[3] == mast.ToString()
                && r[4] == role
                && r[5] == index.ToString()
            )[6];
        Check(
            Seat("Brig", 3, "leftAngleWinch", 0) == Seat("Brig", 2, "leftAngleWinch", 0),
            "Brig shared native identity changed."
        );
        foreach (
            var group in rows.Where(r => r[4] is "leftAngleWinch" or "rightAngleWinch")
                .GroupBy(r => (r[1], r[2], r[3]))
        )
            Check(
                group.Count(r => r[4] == "leftAngleWinch")
                    == group.Count(r => r[4] == "rightAngleWinch"),
                "Re-audit unequal native sheet arrays: " + group.Key
            );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
