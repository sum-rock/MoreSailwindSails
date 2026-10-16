using System;
using System.IO;
using System.Linq;
using MoreSailwindSails.BoatConfiguration;
using MoreSailwindSails.BoatConfiguration.Boats;
using MoreSailwindSails.Controls.Configuration;
using MoreSailwindSails.Controls.Configuration.Boats;
using MoreSailwindSails.Sails.FishermansFlyingSail.Configuration;
using MoreSailwindSails.Stays.FishermansStay.Configuration;

namespace MoreSailwindSails.Tests.GeometryChecks.Controls;

// Checks authored fallback templates and installed donor control inventories.
internal static class NativeSeatProfileChecks
{
    internal static void Run()
    {
        foreach (var boat in BoatControlCatalog.All)
        {
            foreach (var fallback in boat.SheetFallbacks)
                Check(
                    fallback.Fallback != null && fallback.Fallback.InvalidSide == null,
                    "Invalid authored fallback pair."
                );
            foreach (var fallback in boat.HalyardFallbacks)
                Check(
                    boat.Boat.Base(section: fallback.Mast) == fallback.Mast
                        && fallback.Fallback.Valid,
                    "Halyard fallback must belong to a physical base mast."
                );
        }
        Check(
            BoatControlCatalog.All.Sum(boat => boat.SheetFallbacks.Count) == 2,
            "Preserve the two authored sheet fallback pairs."
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
                "Shroud",
                "LargeDhow",
                "Gloriana",
                "Chronian",
                "Caelanor",
                "Kakam",
                "Dhow",
            }
        )
            Check(rows.Any(r => r[1] == boat), "Missing boat inventory: " + boat);
        var names = new[]
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
        for (int b = 0; b < BoatControlCatalog.All.Count; b++)
        {
            var profile = BoatControlCatalog.All[b];
            foreach (
                var donor in FishermansStayCatalog
                    .Find(boatName: profile.BoatName)
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
        Check(
            rows.Single(r => r[1] == "Cog" && r[3] == "58" && r[4] == "reefWinch")[7]
                == "SE_parts_cog/winches/winch_reef_midstay2",
            "Re-audit Cog's additional mizzen halyard seat."
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
