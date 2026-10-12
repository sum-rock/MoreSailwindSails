// Exercises authored profiles with native-only inventories and missing required assets.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoreSailwindSails.BoatRigs;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

internal static class NativeAvailabilityChecks
{
    internal static void Run()
    {
        int candidates = 0;
        foreach (
            var line in File.ReadLines(
                Path.Combine(AppContext.BaseDirectory, "FishermansStay/NativeRigAvailability.txt")
            )
        )
        {
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                continue;
            var columns = line.Split('|');
            int[] Ids(string text) =>
                text.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
            var native = new HashSet<int>(Ids(columns[1]));
            var profile = BoatRigCatalog.Find(boatName: columns[0]);
            var actual = profile
                .Stays.SelectMany(g => g.Variants)
                .Where(v => v.HasRequiredParts(present: native.Contains))
                .Select(v => v.MountIndex)
                .Distinct()
                .OrderBy(i => i)
                .ToArray();
            if (!actual.SequenceEqual(Ids(columns[2])))
                throw new Exception("Native-only stay availability changed: " + columns[0]);
            candidates += actual.Length;
        }
        foreach (
            var variant in BoatRigCatalog.All.SelectMany(b => b.Stays).SelectMany(g => g.Variants)
        )
        {
            var present = new HashSet<int>(variant.Required) { variant.Donor };
            if (!variant.HasRequiredParts(present: present.Contains))
                throw new Exception(
                    "Missing excluded options must not reject complete required rigging."
                );
            foreach (int id in present.ToArray())
            {
                present.Remove(id);
                if (variant.HasRequiredParts(present: present.Contains))
                    throw new Exception(
                        "A missing donor or required mast must disable its variant."
                    );
                present.Add(id);
            }
        }
        Console.WriteLine(
            $"PASS: {candidates} native-only stay candidates across eight boat inventories; missing donors/supports and absent exclusions. Live construction and fit remain unverified."
        );
    }
}
