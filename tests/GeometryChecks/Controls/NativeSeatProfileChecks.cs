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
                    category.Sources.Select(s => s.Mast).Distinct().Count()
                        == category.Sources.Length,
                    "Duplicate source rig."
                );
                Check(
                    category.PhysicalMasts.All(m => category.Sources.Any(s => s.Mast == m)),
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
