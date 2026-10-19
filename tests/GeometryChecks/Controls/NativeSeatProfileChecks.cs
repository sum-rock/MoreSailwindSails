using System;
using System.IO;
using System.Linq;
using MoreSailwindSails.BoatConfiguration;
using MoreSailwindSails.BoatConfiguration.Boats;
using MoreSailwindSails.Controls;
using MoreSailwindSails.Controls.Configuration;
using MoreSailwindSails.Controls.Configuration.Boats;
using MoreSailwindSails.Sails.FishermansFlyingSail.Configuration;
using MoreSailwindSails.Stays.FishermansStay.Configuration;
using UnityEngine;

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
            BoatControlCatalog.All.Sum(boat => boat.SheetFallbacks.Count) == 9,
            "Preserve the nine authored sheet fallback pairs."
        );
        foreach (var pair in BoatControlCatalog.All.SelectMany(boat => boat.SheetFallbacks))
        foreach (var seat in new[] { pair.Fallback.Port, pair.Fallback.Starboard })
        {
            var displacement = seat.Position - seat.Contact;
            Check(
                Math.Abs(displacement.magnitude - 0.044704f) < 0.000001f
                    && Vector3.Dot(displacement.normalized, seat.Normal.normalized) > 0.99999f,
                "Every sheet fallback must use the shared mounting depth along its surface normal."
            );
        }
        Check(
            (
                ShroudControls.Definition.SheetFallbacks.Single().Fallback.Port.Position
                - new Vector3(-3.465970f, 4.874469f, -3.901223f)
            ).magnitude < 0.000001f,
            "Shroud must keep its captured contact with the smaller shared model's mounting depth."
        );
        Check(
            (
                SheetFallbackTemplate.Normal - new Vector3(0.00020555f, 0.99987845f, 0.01559755f)
            ).magnitude < 0.000001f
                && SheetFallbackTemplate.Scale == Vector3.one,
            "Shared sheet frame or unit size differs from the installed Kakam reference."
        );
        Check(
            !new SheetFallbackSeat(contact: Vector3.zero, normal: Vector3.zero).Valid
                && !new SheetFallbackSeat(
                    contact: Vector3.zero,
                    normal: new Vector3(0, float.PositiveInfinity, 0)
                ).Valid,
            "Invalid mounting normals must make a fallback unavailable."
        );
        var caelanor = CaelanorControls.Definition;
        var caelanorPair = caelanor.SheetFallbacks.Single().Fallback;
        Vector3 Reflect(Vector3 value) => new Vector3(-value.x, value.y, value.z);
        foreach (
            var pair in BrigControls
                .Definition.SheetFallbacks.Select(definition => definition.Fallback)
                .Append(caelanorPair)
                .Concat(
                    GlorianaControls.Definition.SheetFallbacks.Select(definition =>
                        definition.Fallback
                    )
                )
                .Concat(
                    ChronianControls.Definition.SheetFallbacks.Select(definition =>
                        definition.Fallback
                    )
                )
        )
            Check(
                (pair.Port.Contact - Reflect(pair.Starboard.Contact)).magnitude < 0.000001f
                    && (pair.Port.Normal - Reflect(pair.Starboard.Normal)).magnitude < 0.000001f
                    && (pair.Port.Position - Reflect(pair.Starboard.Position)).magnitude < 0.000001f
                    && pair.Port.Contact.x < 0
                    && pair.Starboard.Contact.x > 0,
                "Brig, Caelanor, Gloriana and Chronian sheet contacts, mounting offsets and normals must mirror at the deck centreline."
            );
        Check(
            SheetFallbackOrder
                .FromMast(fallbacks: ChronianControls.Definition.SheetFallbacks, mastZ: -2.7997803f)
                .Count() == 2
                && SheetFallbackOrder
                    .FromMast(
                        fallbacks: ChronianControls.Definition.SheetFallbacks,
                        mastZ: -12.799802f
                    )
                    .Single()
                    .Name == "Mizzenmast",
            "Both Chronian sheet pairs must be aft of its mainmast; only the mizzen pair is aft of its mizzen."
        );
        var reef = caelanor.HalyardFallbacks.Single();
        Check(
            reef.Mast == 5
                && reef.Fallback.TemplateIndex == 0
                && Math.Abs(
                    (reef.Fallback.Position - reef.Fallback.Contact).magnitude - 0.07854738f
                ) < 0.000001f
                && SheetFallbackOrder
                    .FromMast(fallbacks: caelanor.SheetFallbacks, mastZ: -2.306270f)
                    .Count() == 1,
            "Caelanor must retain its mainmast reef fallback and its aft-eligible sheet pair."
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
