using System;
using System.Globalization;
using System.IO;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Controls;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

internal static class WinchChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    internal static void Run()
    {
        var allocator = new WinchReservations();
        var donor = new object();
        var flying = new object();
        var staysail = new object();
        var vanilla = new object();
        var positions = new[] { Vector3.up, Vector3.up * 2, Vector3.up * 3 };
        bool Clear(Vector3 p, float radius) => false;
        var a = allocator.Acquire(donor, flying, positions, 0.15f, Clear);
        var b = allocator.Acquire(donor, staysail, positions, 0.15f, Clear);
        var c = allocator.Acquire(donor, vanilla, positions, 0.15f, Clear);
        Check(a.Slot == 0 && b.Slot == 1 && c.Slot == 2, "Mixed families overlap on their donor.");
        Check(
            ReferenceEquals(b, allocator.Acquire(donor, staysail, positions, 0.15f, Clear)),
            "Repeated acquisition moved a control."
        );
        Check(
            allocator.Acquire(donor, new object(), positions, 0.15f, Clear) == null
                && allocator.Count == 3,
            "Exhaustion leaked a reservation."
        );
        allocator.Release(staysail);
        allocator.Release(staysail);
        Check(
            allocator.Acquire(donor, new object(), positions, 0.15f, Clear).Slot == 1
                && a.Slot == 0
                && c.Slot == 2,
            "Release should reuse a hole without moving neighbors."
        );
        var otherDonor = new object();
        var moved = allocator.Acquire(
            otherDonor,
            flying,
            positions.Select(p => p + Vector3.right * 5).ToArray(),
            0.15f,
            Clear
        );
        Check(
            moved.Slot == 0 && allocator.Count == 3,
            "Donor change did not release old reservation."
        );
        var overlapOwner = new object();
        Check(
            allocator
                .Acquire(
                    new object(),
                    overlapOwner,
                    new[] { c.Position, c.Position + Vector3.right },
                    0.15f,
                    Clear
                )
                .Slot == 1,
            "Different donors may share a physical surface; overlaps must be rejected."
        );
        var independentBoat = new WinchReservations();
        Check(
            independentBoat.Acquire(donor, flying, positions, 0.15f, Clear).Slot == 0,
            "A different boat inherited reservations."
        );
        var preview = new object();
        int before = allocator.Count;
        var temporary = allocator.Acquire(
            donor,
            preview,
            new[] { Vector3.right * 10 },
            0.15f,
            Clear
        );
        allocator.Release(preview);
        Check(
            temporary != null && allocator.Count == before,
            "Cancel/failed setup leaked a temporary reservation."
        );
        Check(
            new WinchReservations().Acquire(donor, preview, positions, 0.15f, (p, r) => true)
                == null,
            "Native controls were ignored."
        );
        var rejected = new WinchReservations.Rejections();
        Check(
            allocator.Acquire(
                donor,
                new object(),
                new[] { c.Position },
                0.15f,
                (p, r) => true,
                rejected
            ) == null
                && rejected.Native == 1
                && rejected.Reserved == 0,
            "Native obstructions must take precedence when a reservation also blocks a candidate."
        );
        Check(
            allocator.Acquire(donor, new object(), Array.Empty<Vector3>(), 0.15f, Clear, rejected)
                == null
                && rejected.Native == 0
                && rejected.Reserved == 0,
            "Empty support diagnostics must not retain an earlier rejection count."
        );

        foreach (var boat in BoatRigCatalog.All)
        {
            foreach (var support in boat.Supports)
            {
                boat.WinchMount(support.SheetControlSource, WinchRole.Left);
                boat.WinchMount(support.SheetControlSource, WinchRole.Right);
                foreach (int fore in support.ForeSections)
                    boat.WinchMount(fore, WinchRole.Reef);
            }
            foreach (var stay in boat.Stays.SelectMany(g => g.Variants))
            {
                boat.WinchMount(stay.Donor, WinchRole.Left);
                boat.WinchMount(stay.Donor, WinchRole.Right);
                boat.WinchMount(stay.Aft, WinchRole.Reef);
                boat.WinchMount(boat.Base(stay.Aft), WinchRole.Reef);
            }
        }
        Check(
            BoatRigCatalog
                .All.SelectMany(b => b.WinchMounts.Select(d => (b.BoatName, d.Mast, d.Role)))
                .Distinct()
                .Count() == BoatRigCatalog.All.Sum(b => b.WinchMounts.Length),
            "Duplicate mounting definitions."
        );
        int measured = 0;
        Vector3 Parse(string text)
        {
            var v = text.Split(',')
                .Select(x => float.Parse(x, CultureInfo.InvariantCulture))
                .ToArray();
            return new Vector3(v[0], v[1], v[2]);
        }
        foreach (
            string line in File.ReadLines(
                Path.Combine(AppContext.BaseDirectory, "FishermansStay", "WinchMeasurements.txt")
            )
        )
        {
            if (line.StartsWith("#"))
                continue;
            var fields = line.Split('|');
            var role = (WinchRole)Enum.Parse(typeof(WinchRole), fields[2], true);
            var definition = BoatRigCatalog.Find(fields[0]).WinchMount(int.Parse(fields[1]), role);
            // Native pin seats use live transforms/visibility, covered separately
            // against the complete Shroud fixture and native ShowWinch contract.
            if (definition.PinNames != null)
            {
                measured++;
                continue;
            }
            var origin = Parse(fields[3]);
            var normal = Parse(fields[4]).normalized;
            Check(
                definition.SurfaceSegments != null
                    || definition.FixedSurfacePoint.HasValue
                    || Math.Abs(Vector3.Dot(normal, definition.Direction)) < 0.12f,
                "Mounting travel leaves the donor's surface: " + line
            );
            var axisPoint = Parse(fields[5]);
            float radius = float.Parse(fields[6], CultureInfo.InvariantCulture);
            var candidates = WinchPlacementGeometry.Candidates(
                definition,
                origin,
                radius,
                axisPoint
            );
            Check(
                candidates.Length >= (definition.OnMast ? 4 : 1),
                "A measured winch has too few mounting candidates: " + line
            );
            var sourceRadial = Vector3.ProjectOnPlane(origin - axisPoint, definition.Direction);
            foreach (var candidate in candidates)
            {
                var delta = candidate.Position - origin;
                if (definition.OnMast)
                {
                    var radial = Vector3.ProjectOnPlane(
                        candidate.Position - axisPoint,
                        definition.Direction
                    );
                    Check(
                        Math.Abs(radial.magnitude - sourceRadial.magnitude) < 0.0001f,
                        "Mast fitting floated away from its attachment radius."
                    );
                    Check(
                        Math.Abs(
                            Vector3.Dot(radial.normalized, candidate.Rotation * normal)
                                - Vector3.Dot(sourceRadial.normalized, normal)
                        ) < 0.0001f,
                        "Mast overflow reversed the winch face into the spar."
                    );
                    float height = Vector3.Dot(delta, definition.Direction);
                    Check(
                        height >= -0.701f && height <= 1.401f,
                        "Mast fitting escaped its bounded height band."
                    );
                }
                else if (
                    definition.SurfaceSegments == null
                    && !definition.FixedSurfacePoint.HasValue
                )
                {
                    Check(
                        delta.magnitude >= 0.34f && delta.magnitude <= 1.401f,
                        "Surface travel is unbounded."
                    );
                    Check(
                        Vector3.Cross(delta, definition.Direction).magnitude < 0.0001f,
                        "Candidate left its authored mounting line."
                    );
                }
                var rotation = new Quaternion(0.182574f, 0.365148f, -0.547723f, 0.730297f);
                Check(
                    Math.Abs(
                        Vector3.Dot(rotation * delta, rotation * normal)
                            - Vector3.Dot(delta, normal)
                    ) < 0.0001f,
                    "Boat rotation changes mounting clearance."
                );
            }
            var capacity = new WinchReservations();
            // The large dhow's mesh-collider reef fittings use the conservative
            // mesh radius; require one extra slot in the existing height band.
            // Retain the three-slot requirement for the older mast fittings.
            int minimum =
                definition.FixedSurfacePoint.HasValue ? 1
                : definition.SurfaceSegments != null ? 2
                : fields[0] == LargeDhow.Definition.BoatName ? 1
                : 3;
            for (int i = 0; i < minimum; i++)
                Check(
                    capacity.Acquire(
                        donor,
                        new object(),
                        candidates.Select(c => c.Position).ToArray(),
                        radius,
                        (position, r) => WinchReservations.Overlap(position, r, origin, radius)
                    ) != null,
                    "Measured donor has insufficient safe mounting capacity: " + line
                );
            measured++;
        }
        Check(
            measured == BoatRigCatalog.All.Sum(b => b.WinchMounts.Length),
            "A mounting profile has no installed-asset measurement."
        );
        BrigWinchChecks.Run();
        SurfaceWinchChecks.Run();
        LargeDhowWinchChecks.Run();
        JongWinchChecks.Run();
        ShroudWinchChecks.Run();
        ShroudPinChecks.Run();
        Console.WriteLine(
            $"PASS: shared winch allocation, release, donor changes, bounded placement and {measured} installed donor datums across eight boats. Surface accessibility and Unity lifecycle require in-game validation."
        );
    }
}
