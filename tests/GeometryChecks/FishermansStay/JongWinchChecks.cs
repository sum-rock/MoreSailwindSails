using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Controls;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

internal static class JongWinchChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    private static Vector3 Parse(string text)
    {
        var values = text.Split(',')
            .Select(v => float.Parse(v, CultureInfo.InvariantCulture))
            .ToArray();
        return new Vector3(values[0], values[1], values[2]);
    }

    private static string[][] Rows(string file) =>
        File.ReadLines(Path.Combine(AppContext.BaseDirectory, "FishermansStay", file))
            .Where(line => !line.StartsWith("#", StringComparison.Ordinal))
            .Select(line => line.Split('|'))
            .ToArray();

    internal static void Run()
    {
        var profile = Jong.Definition;
        var natives = Rows("JongNativeWinchMeasurements.txt")
            .Select(f => new
            {
                Path = f[0],
                Origin = Parse(f[1]),
                Radius = float.Parse(f[2], CultureInfo.InvariantCulture),
            })
            .ToArray();
        Check(
            natives.Length == 151
                && natives.Select(n => n.Path).Distinct().Count() == 151
                && natives.Count(n => n.Path.StartsWith("native/", StringComparison.Ordinal)) == 60,
            "Missing native or Shipyard Expansion Jong fittings."
        );
        // Conservatively include inactive/mutually exclusive variants too. The
        // original tests missed SE's extra front sheets (2), (3) and (4).
        bool Obstructed(Vector3 position, float radius) =>
            natives.Any(n => WinchReservations.Overlap(position, radius, n.Origin, n.Radius));
        var donors = Rows("WinchMeasurements.txt")
            .Where(f => f[0] == profile.BoatName)
            .ToDictionary(f => (int.Parse(f[1]), Enum.Parse<WinchRole>(f[2], true)));
        var placed = new List<Vector3>();
        foreach (var role in new[] { WinchRole.Left, WinchRole.Right })
        {
            string side = role == WinchRole.Left ? "left" : "right";
            var oldDonor = natives.Single(n => n.Path == "native/winch_stay_front_" + side);
            var lowerDonor = natives.Single(n =>
                n.Path == "native/winch_stay_front_" + side + " (1)"
            );
            var datum = donors[(10, role)];
            var mount = profile.WinchMount(10, role);
            Check(
                mount.SourceMast == 7 && mount.SourceIndex == -1,
                "Jong fore sheets must select the native lower-stay donor."
            );
            Check(
                (Parse(datum[3]) - lowerDonor.Origin).magnitude < 0.00001f,
                "Jong donor datum does not describe native mast 7's sheet."
            );

            // Rebuild the former three-strip profile from independent face
            // measurements, not from the corrected one-strip production profile.
            var oldSegments = Rows("WinchSurfaceMeasurements.txt")
                .Where(f => f[0] == profile.BoatName && f[1] == "10" && f[2] == side)
                .Select(f => new WinchSurfaceSegment(
                    (Parse(f[4]) + Parse(f[5])) * 0.5f,
                    (Parse(f[6]) + Parse(f[7])) * 0.5f,
                    Parse(f[8])
                ))
                .ToArray();
            Check(oldSegments.Length == 3, "Missing original forward rail measurements.");
            var oldMount = new WinchMountDefinition(
                10,
                role,
                Parse(datum[4]),
                mount.BaseOffset,
                oldSegments
            );
            var oldPositions = WinchPlacementGeometry
                .Candidates(oldMount, oldDonor.Origin, oldDonor.Radius, oldDonor.Origin)
                .Select(p => p.Position)
                .ToArray();
            var report = new WinchReservations.Rejections();
            var oldReservation = new WinchReservations().Acquire(
                oldDonor,
                new object(),
                oldPositions,
                oldDonor.Radius,
                Obstructed,
                report
            );
            Check(
                oldPositions.Length > 0 && (oldReservation == null) == (role == WinchRole.Left),
                "Issue #16 must reproduce exhausted port placement with starboard still available."
            );
            if (role == WinchRole.Left)
                Check(
                    report.Native == oldPositions.Length && report.Reserved == 0,
                    "Original Jong failure must be attributed to native fittings."
                );

            var positions = WinchPlacementGeometry
                .Candidates(mount, lowerDonor.Origin, lowerDonor.Radius, lowerDonor.Origin)
                .Select(p => p.Position)
                .ToArray();
            var allocator = new WinchReservations();
            var owner = new object();
            var fitting = allocator.Acquire(
                lowerDonor,
                owner,
                positions,
                lowerDonor.Radius,
                Obstructed,
                report
            );
            Check(fitting != null, "Corrected Jong sheet still has no supported slot.");
            Check(
                (fitting.Position - lowerDonor.Origin).magnitude <= 1.4011f
                    && (fitting.Position - oldDonor.Origin).magnitude > 1.401f,
                "The clear lower-rail slot must require the corrected donor, not expanded travel."
            );
            Check(
                mount.SurfaceSegments.Length == 1 && fitting.Position.z < 2.6f,
                "Jong fore sheets must remain on the opposed lower rails."
            );
            placed.Add(fitting.Position);
            Check(
                ReferenceEquals(
                    fitting,
                    allocator.Acquire(lowerDonor, owner, positions, lowerDonor.Radius, Obstructed)
                ),
                "A bound Jong sheet moved on reacquisition."
            );
            Check(
                allocator.Acquire(
                    lowerDonor,
                    new object(),
                    positions,
                    lowerDonor.Radius,
                    Obstructed,
                    report
                ) == null
                    && allocator.Count == 1,
                "True Jong exhaustion must not overlap or leak a reservation."
            );
            Check(
                report.Native > 0
                    && report.Reserved > 0
                    && report.Native + report.Reserved == positions.Length,
                "Exhaustion must distinguish native fittings from custom reservations."
            );
            allocator.Release(owner);
            var retry = allocator.Acquire(
                lowerDonor,
                new object(),
                positions,
                lowerDonor.Radius,
                Obstructed,
                report
            );
            Check(
                retry != null && retry.Position == fitting.Position && report.Reserved == 0,
                "Released Jong space must be reusable and rejection counts must reset."
            );
        }
        var mirrored = new Vector3(-placed[0].x, placed[0].y, placed[0].z);
        Check(
            (mirrored - placed[1]).magnitude < 0.002f,
            "Jong fore sheet positions must mirror within native datum tolerances."
        );
        Check(
            profile.Stays.SelectMany(g => g.Variants).Single(v => v.MountIndex == 128).Donor == 10,
            "Changing sheet sources must not change the stay geometry donor or saved mount."
        );

        // Simulate either fitting order and reconstruction after releasing a
        // neighboring Flying Sail. Both sails share the aft/fore reef donor 2.
        foreach (bool flyingFirst in new[] { false, true })
        {
            var allocator = new WinchReservations();
            var identities = new Dictionary<(int, WinchRole), object>();
            var neighbors = new List<object>();
            var sheets = new List<WinchReservations.Reservation>();
            void Fit(int mapping, WinchRole role, bool flying)
            {
                var mount = profile.WinchMount(mapping, role);
                var datum = donors[(mapping, role)];
                var key = (mount.SourceMast >= 0 ? mount.SourceMast : mapping, role);
                if (!identities.TryGetValue(key, out var donor))
                    identities.Add(key, donor = new object());
                float radius = float.Parse(datum[6], CultureInfo.InvariantCulture);
                var positions = WinchPlacementGeometry
                    .Candidates(mount, Parse(datum[3]), radius, Parse(datum[5]))
                    .Select(p => p.Position)
                    .ToArray();
                var owner = new object();
                var fitting = allocator.Acquire(donor, owner, positions, radius, Obstructed);
                Check(fitting != null, $"Mixed Jong configuration cannot fit {mapping}/{role}.");
                if (flying)
                    neighbors.Add(owner);
                else if (role != WinchRole.Reef)
                    sheets.Add(fitting);
            }
            void FitFlying()
            {
                Fit(2, WinchRole.Reef, true);
                Fit(12, WinchRole.Left, true);
                Fit(12, WinchRole.Right, true);
            }
            if (flyingFirst)
                FitFlying();
            Fit(2, WinchRole.Reef, false);
            Fit(10, WinchRole.Left, false);
            Fit(10, WinchRole.Right, false);
            if (!flyingFirst)
                FitFlying();
            Check(allocator.Count == 6, "Mixed Jong controls lost a reservation.");
            foreach (var owner in neighbors)
                allocator.Release(owner);
            Check(allocator.Count == 3, "Removing neighboring controls leaked reservations.");
            FitFlying();
            Check(
                allocator.Count == 6 && sheets.Select(s => s.Position).SequenceEqual(placed),
                "Refitting a neighboring sail displaced the Jong fore sheets."
            );
        }
        Console.WriteLine(
            "PASS: Jong port-only exhaustion reproduced against 151 native/SE fittings; lower-stay donors give opposed supported sheets, bounded exhaustion/retry and mixed Flying Sail reservations."
        );
    }
}
