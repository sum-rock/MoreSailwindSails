using System;
using System.Globalization;
using System.IO;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Controls;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

internal static class ShroudPinChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    internal static void Run()
    {
        var natives = File.ReadLines(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "FishermansStay",
                    "ShroudNativeWinchMeasurements.txt"
                )
            )
            .Where(line => !line.StartsWith("#", StringComparison.Ordinal))
            .Select(line => line.Split('|'))
            .Select(f =>
            {
                var xyz = f[1]
                    .Split(',')
                    .Select(v => float.Parse(v, CultureInfo.InvariantCulture))
                    .ToArray();
                return new
                {
                    Name = f[0].Split('/').Last(),
                    Position = new Vector3(xyz[0], xyz[1], xyz[2]),
                    Radius = float.Parse(f[2], CultureInfo.InvariantCulture),
                };
            })
            .ToArray();
        var banks = Shroud.Definition.WinchMounts.Where(m => m.PinNames != null).ToArray();
        Check(
            banks.Length == 4 && banks.All(b => b.Role == WinchRole.Reef),
            "Only four Shroud halyard mappings use pin banks."
        );
        Check(
            BoatRigCatalog
                .All.Where(b => b != Shroud.Definition)
                .All(b => b.WinchMounts.All(m => m.PinNames == null)),
            "Native pin reuse leaked to another boat."
        );
        Check(
            banks[0].PinNames.SequenceEqual(banks[1].PinNames)
                && banks[2].PinNames.SequenceEqual(banks[3].PinNames),
            "Mast variants must share their physical rack."
        );
        foreach (var bank in banks)
        {
            Check(
                bank.PinNames.Length == 12 && bank.PinNames.Distinct().Count() == 12,
                "Each rack needs twelve distinct native side pins."
            );
            var pins = bank.PinNames.Select(name => natives.Single(n => n.Name == name)).ToArray();
            Check(
                pins.Count(p => p.Position.x < -0.25f) == 6
                    && pins.Count(p => p.Position.x > 0.25f) == 6,
                "A beam-top coil was mistaken for a side pin."
            );
            Check(
                pins.All(p => Math.Abs(p.Radius - 0.185f) < 0.00001f),
                "Unexpected native pin-coil interaction size."
            );
            for (int i = 0; i < pins.Length; i++)
            for (int j = i + 1; j < pins.Length; j++)
                Check(
                    (pins[i].Position - pins[j].Position).magnitude > bank.PinRadius * 2f + 0.02f,
                    "Pin clearance excludes adjacent native pin seats."
                );

            var positions = pins.Select(p => p.Position).ToArray();
            var inUse = pins.ToDictionary(p => p.Name, p => false);
            bool Obstructed(Vector3 position, float radius) =>
                natives.Any(n =>
                    (!inUse.ContainsKey(n.Name) || inUse[n.Name])
                    && WinchReservations.Overlap(
                        position,
                        radius,
                        n.Position,
                        inUse.ContainsKey(n.Name) ? bank.PinRadius : n.Radius
                    )
                );
            var allocator = new WinchReservations();
            var donor = new object();
            var flying = new object();
            var staysail = new object();
            var first = allocator.Acquire(donor, staysail, positions, bank.PinRadius, Obstructed);
            var second = allocator.Acquire(donor, flying, positions, bank.PinRadius, Obstructed);
            Check(
                first != null && second != null && first.Slot != second.Slot,
                "Mixed sail families must occupy separate unused pins."
            );

            // A native sail claims the first borrowed pin; the custom controller
            // releases it and re-acquires another slot, leaving its neighbour stable.
            inUse[pins[first.Slot].Name] = true;
            Check(
                Obstructed(first.Position, bank.PinRadius),
                "A visible native coil must reclaim its pin."
            );
            allocator.Release(staysail);
            var moved = allocator.Acquire(donor, staysail, positions, bank.PinRadius, Obstructed);
            Check(
                moved != null && moved.Slot != first.Slot && moved.Slot != second.Slot,
                "Halyard did not yield to the native sail."
            );
            allocator.Release(staysail);
            allocator.Release(flying);
            foreach (var name in bank.PinNames)
                inUse[name] = true;
            var report = new WinchReservations.Rejections();
            Check(
                allocator.Acquire(donor, staysail, positions, bank.PinRadius, Obstructed, report)
                    == null
                    && report.Native == 12
                    && report.Reserved == 0
                    && allocator.Count == 0,
                "A full native rack must exhaust without overlapping controls."
            );
            inUse[pins[0].Name] = false;
            var retry = allocator.Acquire(donor, staysail, positions, bank.PinRadius, Obstructed);
            Check(retry != null && retry.Slot == 0, "A newly unused pin must permit retry.");
        }
        Console.WriteLine(
            "PASS: Shroud halyards use twelve measured side pins per rack; adjacent seating, hidden-slot reuse, mixed-family reservations, native reclaim and full-bank exhaustion/retry."
        );
    }
}
