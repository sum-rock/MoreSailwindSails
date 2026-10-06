using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoreSailwindSails.BoatRigs;

namespace MoreSailwindSails.Tests.GeometryChecks.Controls;

// Checks supported boats' authored groups against the installed support audit and native seats.
internal static class HalyardProfileChecks
{
    internal static void Run()
    {
        var boats = new Dictionary<string, BoatRigDefinition>
        {
            { "Brig", Brig.Definition },
            { "Junk", Junk.Definition },
            { "Jong", Jong.Definition },
            { "Sanbuq", Sanbuq.Definition },
            { "Cog", Cog.Definition },
            { "Shroud", Shroud.Definition },
            { "LargeDhow", LargeDhow.Definition },
            { "Gloriana", Gloriana.Definition },
            { "Chronian", Chronian.Definition },
            { "Caelanor", Caelanor.Definition },
            { "Gallus", Gallus.Definition },
        };
        var associations = File.ReadAllLines(
                Path.Combine(AppContext.BaseDirectory, "Controls", "HalyardMounts.txt")
            )
            .Where(line => !line.StartsWith("#") && line.Length > 0)
            .Select(line => line.Split('|'))
            .Where(row => boats.ContainsKey(row[0]))
            .SelectMany(row =>
                row[1]
                    .Split(',')
                    .Select(id => new
                    {
                        Boat = row[0],
                        Mast = int.Parse(id),
                        Sources = row[2]
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(int.Parse)
                            .ToArray(),
                    })
            )
            .ToDictionary(row => (row.Boat, row.Mast), row => row.Sources);
        var inventory = File.ReadAllLines(
                Path.Combine(AppContext.BaseDirectory, "FishermansStay", "NativeWinchSeats.txt")
            )
            .Where(line => line.StartsWith("rig|") || line.StartsWith("seat|"))
            .Select(line => line.Split('|'))
            .ToArray();
        var rigs = inventory
            .Where(row => row[0] == "rig")
            .ToDictionary(row => (row[1], int.Parse(row[3])));
        string[] ReefSeats(string boat, int source) =>
            inventory
                .Where(row =>
                    row[0] == "seat"
                    && row[1] == boat
                    && int.Parse(row[3]) == source
                    && row[4] == "reefWinch"
                )
                .Select(row => row[6])
                .Where(id => id != "null")
                .ToArray();

        foreach (var entry in boats)
        {
            var boat = entry.Value;
            Check(
                boat.HalyardGroups.Count == associations.Keys.Count(key => key.Boat == entry.Key),
                "Unaudited or missing halyard groups: " + entry.Key
            );
            foreach (int mast in boat.MastParents.Keys)
            {
                var expected = new[] { mast }.Concat(
                    associations.TryGetValue((entry.Key, mast), out var sources)
                        ? sources
                        : Array.Empty<int>()
                );
                Check(
                    boat.HalyardSources(mast: mast).SequenceEqual(expected),
                    "Halyard source order or physical mast association changed: "
                        + entry.Key
                        + "/"
                        + mast
                );
            }
            foreach (var group in boat.HalyardGroups)
            {
                foreach (int source in group.Sources)
                    Check(
                        rigs.TryGetValue((entry.Key, source), out var rig)
                            && rig[5] != "-1"
                            && ReefSeats(boat: entry.Key, source: source).Length > 0,
                        "Missing, unregistered or empty reef source: " + entry.Key + "/" + source
                    );
                var own = ReefSeats(boat: entry.Key, source: group.Mast);
                Check(
                    group.Fallback?.Valid == true
                        || group
                            .Sources.Skip(1)
                            .SelectMany(source => ReefSeats(boat: entry.Key, source: source))
                            .Except(own)
                            .Any(),
                    "Group adds neither distinct stay reef seats nor a valid fallback: "
                        + entry.Key
                        + "/"
                        + group.Mast
                );
            }
        }

        Check(
            ReefSeats(boat: "Cog", source: 51).Length == 1
                && ReefSeats(boat: "Cog", source: 51)
                    .SequenceEqual(ReefSeats(boat: "Cog", source: 65))
                && !ReefSeats(boat: "Cog", source: 8)
                    .Intersect(ReefSeats(boat: "Cog", source: 51))
                    .Any(),
            "Cog's original mizzen must gain one shared midstay seat, not two independent seats."
        );

        // These Brig objects have the same hierarchy name, but are not the same rig.
        Check(
            rigs[("Brig", 70)][7] == rigs[("Brig", 74)][7]
                && rigs[("Brig", 70)][5] != "-1"
                && rigs[("Brig", 74)][5] == "-1"
                && !ReefSeats(boat: "Brig", source: 70)
                    .Intersect(ReefSeats(boat: "Brig", source: 74))
                    .Any()
                && Brig.Definition.HalyardSources(mast: 4).Contains(70)
                && Brig.Definition.HalyardSources(mast: 58).Contains(70)
                && !Brig.Definition.HalyardGroups.Any(group => group.Sources.Contains(74)),
            "Brig's registered mainmast source 70 was confused with unregistered foremast source 74."
        );
        Console.WriteLine(
            $"PASS: {associations.Count} audited halyard groups across eleven supported boats, exact mast/section source lists, own-seat priority, registered reef capacity and Brig 70/74 identity separation."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
