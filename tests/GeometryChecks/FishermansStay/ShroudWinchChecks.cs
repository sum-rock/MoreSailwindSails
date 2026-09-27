using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Controls;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

internal static class ShroudWinchChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    private static Vector3 Parse(string text)
    {
        var v = text.Split(',').Select(x => float.Parse(x, CultureInfo.InvariantCulture)).ToArray();
        return new Vector3(v[0], v[1], v[2]);
    }

    private static string[][] Rows(string name) =>
        File.ReadLines(Path.Combine(AppContext.BaseDirectory, "FishermansStay", name))
            .Where(line => !line.StartsWith("#", StringComparison.Ordinal))
            .Select(line => line.Split('|'))
            .ToArray();

    internal static void Run()
    {
        var profile = Shroud.Definition;
        var natives = Rows("ShroudNativeWinchMeasurements.txt")
            .Select(f => new
            {
                Path = f[0],
                Position = Parse(f[1]),
                Radius = float.Parse(f[2], CultureInfo.InvariantCulture),
            })
            .ToArray();
        Check(
            natives.Length == 96 && natives.Select(n => n.Path).Distinct().Count() == 96,
            "Shroud fixture must include 95 sphere fittings and the mesh-collider anchor."
        );
        bool Obstructed(Vector3 p, float r) =>
            natives.Any(n => WinchReservations.Overlap(p, r, n.Position, n.Radius));
        var trims = Rows("ShroudTrimMeasurements.txt");
        Check(trims.Length == 4, "Measure both sides of painted and unpainted Shroud trim.");
        Check(
            BoatRigCatalog
                .All.SelectMany(b => b.WinchMounts)
                .Count(m => m.FixedSurfacePoint.HasValue) == 2
                && profile
                    .WinchMounts.Where(m => m.FixedSurfacePoint.HasValue)
                    .All(m => m.Mast == 24),
            "Fixed mounts must only replace the Shroud's forward sheets."
        );
        var allocator = new WinchReservations();
        var placed = new List<Vector3>();
        foreach (var role in new[] { WinchRole.Left, WinchRole.Right })
        {
            string side = role == WinchRole.Left ? "left" : "right";
            var datum = Rows("WinchMeasurements.txt")
                .Single(f => f[0] == profile.BoatName && f[1] == "24" && f[2] == side);
            var origin = Parse(datum[3]);
            float radius = float.Parse(datum[6], CultureInfo.InvariantCulture);
            var mount = profile.WinchMount(24, role);
            Check(
                mount.SourceMast == -1 && mount.SourceIndex == -1,
                "Fixed mounts changed donors."
            );
            var contact = mount.FixedSurfacePoint.Value;
            foreach (var trim in trims.Where(t => t[1] == side))
            {
                var middle = (Parse(trim[2]) + Parse(trim[3])) * 0.5f;
                Check(
                    (contact.Position - middle).magnitude < 0.0001f,
                    "Fixed sheet is not centred across the visible trim: " + trim[0]
                );
            }
            var candidates = WinchPlacementGeometry.Candidates(mount, origin, radius, origin);
            Check(
                candidates.Length == 1,
                "A fixed sheet must have exactly one authored candidate."
            );
            var point = candidates[0].Position;
            Check(
                Math.Abs(point.y - 4.906450f) < 0.00001f
                    && Math.Abs(point.z - (-3.886444f - 3.916001f) * 0.5f) < 0.00001f
                    && Vector3.Dot(candidates[0].Rotation * Parse(datum[4]), Vector3.up) > 0.99999f,
                "Fixed sheet has the wrong mounting height, capture midpoint or surface alignment."
            );
            Check(
                (point - origin).magnitude > 1.401f,
                "Fixed-point regression needs a distant donor."
            );
            var movedDonor = origin + new Vector3(50f, 20f, -30f);
            Check(
                WinchPlacementGeometry
                    .Candidates(mount, movedDonor, radius, movedDonor)
                    .Single()
                    .Position == point,
                "Donor movement changed or filtered an explicitly authored point."
            );

            // Reproduce the old failed rail search using its independent face fixture.
            var oldSegments = Rows("WinchSurfaceMeasurements.txt")
                .Where(f =>
                    f[0] == profile.BoatName && f[1] == "24" && f[2] == side && Parse(f[4]).z < -6f
                )
                .Select(f => new WinchSurfaceSegment(
                    (Parse(f[4]) + Parse(f[5])) * 0.5f,
                    (Parse(f[6]) + Parse(f[7])) * 0.5f,
                    Parse(f[8])
                ))
                .ToArray();
            Check(oldSegments.Length == 2, "Missing old Shroud forward rail measurements.");
            var oldMount = new WinchMountDefinition(
                24,
                role,
                Parse(datum[4]),
                mount.BaseOffset,
                oldSegments
            );
            var old = WinchPlacementGeometry.Candidates(oldMount, origin, radius, origin);
            Check(
                old.Length > 0 && old.All(p => Obstructed(p.Position, radius)),
                "Old Shroud sheet exhaustion was not reproduced."
            );

            var positions = new[] { point };
            var report = new WinchReservations.Rejections();
            var donor = new object();
            var owner = new object();
            var waitingOwner = new object();
            Check(
                allocator.Acquire(donor, owner, positions, radius, Obstructed) != null,
                "Fixed sheet intersects a native fitting or opposite sheet."
            );
            Check(
                allocator.Acquire(donor, waitingOwner, positions, radius, Obstructed, report)
                    == null
                    && report.Native == 0
                    && report.Reserved == 1,
                "A second custom control must not occupy the same fixed point."
            );
            allocator.Release(owner);
            Check(
                allocator.Acquire(donor, waitingOwner, positions, radius, Obstructed) != null,
                "Fixed sheet failed to retry after reservation release."
            );
            var blocked = new WinchReservations();
            Check(
                blocked.Acquire(donor, owner, positions, radius, (p, r) => true, report) == null
                    && blocked.Count == 0
                    && report.Native == 1
                    && report.Reserved == 0,
                "Fixed points must retain native obstruction rejection and diagnostics."
            );
            Check(
                blocked.Acquire(donor, owner, positions, radius, Obstructed) != null,
                "Fixed sheet failed to retry after native obstruction cleared."
            );
            placed.Add(point);
        }
        Check(
            allocator.Count == 2
                && placed[0].x == -placed[1].x
                && placed[0].y == placed[1].y
                && placed[0].z == placed[1].z,
            "Shroud forward sheets must coexist as an exactly mirrored pair."
        );
        Console.WriteLine(
            "PASS: Shroud forward sheet exhaustion reproduced; fixed mounts centre on both trim finishes, mirror exactly, clear 96 native fittings, retain reservations/retries and ignore donor distance."
        );
    }
}
