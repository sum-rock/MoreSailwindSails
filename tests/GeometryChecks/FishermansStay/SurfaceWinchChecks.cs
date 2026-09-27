using System;
using System.Globalization;
using System.IO;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Controls;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

internal static class SurfaceWinchChecks
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
            .Where(line => !line.StartsWith("#"))
            .Select(line => line.Split('|'))
            .ToArray();

    // Independently check the measured cap faces, including their slight warp.
    private static bool OnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        var u = b - a;
        var v = c - a;
        var w = p - a;
        var normal = Vector3.Cross(u, v).normalized;
        if (Math.Abs(Vector3.Dot(w, normal)) > 0.02f)
            return false;
        float uu = Vector3.Dot(u, u),
            uv = Vector3.Dot(u, v),
            vv = Vector3.Dot(v, v);
        float wu = Vector3.Dot(w, u),
            wv = Vector3.Dot(w, v);
        float determinant = uu * vv - uv * uv;
        float x = (vv * wu - uv * wv) / determinant;
        float y = (uu * wv - uv * wu) / determinant;
        return x >= -0.001f && y >= -0.001f && x + y <= 1.001f;
    }

    internal static void Run()
    {
        var faces = Rows("WinchSurfaceMeasurements.txt")
            .Select(f => new
            {
                Boat = f[0],
                Ids = f[1].Split(',').Select(int.Parse).ToArray(),
                Role = (WinchRole)Enum.Parse(typeof(WinchRole), f[2], true),
                Offset = float.Parse(f[3], CultureInfo.InvariantCulture),
                A = Parse(f[4]),
                B = Parse(f[5]),
                C = Parse(f[6]),
                D = Parse(f[7]),
                Normal = Parse(f[8]).normalized,
            })
            .ToArray();
        var donors = Rows("WinchMeasurements.txt")
            .Select(f => new
            {
                Boat = f[0],
                Id = int.Parse(f[1]),
                Role = (WinchRole)Enum.Parse(typeof(WinchRole), f[2], true),
                Origin = Parse(f[3]),
                Normal = Parse(f[4]).normalized,
                Radius = float.Parse(f[6], CultureInfo.InvariantCulture),
            })
            .ToArray();
        int measured = 0;
        foreach (var boat in BoatRigCatalog.All)
        foreach (var mount in boat.WinchMounts)
        {
            Check(
                mount.OnMast
                    || mount.SurfaceSegments?.Length > 0
                    || mount.FixedSurfacePoint.HasValue,
                "A deck/rail winch still relies on an unsupported tangent: " + boat.BoatName
            );
            if (mount.OnMast || boat.BoatName == Brig.Definition.BoatName)
                continue;
            var donor = donors.Single(d =>
                d.Boat == boat.BoatName && d.Id == mount.Mast && d.Role == mount.Role
            );
            var measuredFaces = faces
                .Where(f =>
                    f.Boat == boat.BoatName && f.Ids.Contains(mount.Mast) && f.Role == mount.Role
                )
                .ToArray();
            Check(measuredFaces.Length > 0, "Missing independently measured support bounds.");
            var candidates = WinchPlacementGeometry.Candidates(
                mount,
                donor.Origin,
                donor.Radius,
                donor.Origin
            );
            foreach (var candidate in candidates)
            {
                var normal = candidate.Rotation * donor.Normal;
                Check(
                    measuredFaces.Any(f =>
                    {
                        var contact = candidate.Position - normal * f.Offset;
                        var start = (f.A + f.B) * 0.5f;
                        var end = (f.C + f.D) * 0.5f;
                        return Vector3.Dot(normal, f.Normal) > 0.99999f
                            && (contact - start).magnitude >= donor.Radius - 0.001f
                            && (contact - end).magnitude >= donor.Radius - 0.001f
                            && (
                                OnTriangle(contact, f.A, f.B, f.C)
                                || OnTriangle(contact, f.B, f.D, f.C)
                            );
                    }),
                    $"Unsupported surface placement: {boat.BoatName}/{mount.Mast}/{mount.Role}."
                );
                Check(
                    mount.FixedSurfacePoint.HasValue
                        || (candidate.Position - donor.Origin).magnitude <= 1.4011f,
                    "Surface candidate exceeds bounded donor travel."
                );
            }
            var allocator = new WinchReservations();
            var positions = candidates.Select(c => c.Position).ToArray();
            bool Obstructed(Vector3 p, float radius) =>
                donors.Any(d =>
                    d.Boat == boat.BoatName
                    && WinchReservations.Overlap(p, radius, d.Origin, d.Radius)
                );
            // Include neighboring donor variants even when they cannot all be active.
            // Each source must retain at least one supported, unobstructed clone slot.
            var first = allocator.Acquire(donor, new object(), positions, donor.Radius, Obstructed);
            Check(
                first != null,
                $"No supported slot beside native fittings: {boat.BoatName}/{mount.Mast}/{mount.Role}."
            );
            int count = 1;
            while (
                allocator.Acquire(new object(), new object(), positions, donor.Radius, Obstructed)
                != null
            )
                Check(++count <= positions.Length, "Surface reservations did not exhaust.");
            Check(allocator.Count == count, "Surface exhaustion leaked a reservation.");
            measured++;
        }
        Check(measured == 110, "Expected 110 surface mappings beyond the 24 Brig sheet mappings.");

        // Leopard's neighboring reef coils leave a safe strip end between the
        // native controls and the raised post, outside the donor-centered spacing grid.
        var bounded = new WinchMountDefinition(
            1,
            WinchRole.Reef,
            Vector3.up,
            0.13f,
            new WinchSurfaceSegment(
                new Vector3(-0.85f, 0f, 0f),
                new Vector3(0.85f, 0f, 0f),
                Vector3.up
            )
        );
        var origin = new Vector3(-0.66f, 0.13f, 0f);
        var slots = WinchPlacementGeometry.Candidates(bounded, origin, 0.21f, origin);
        var endReservation = new WinchReservations().Acquire(
            new object(),
            new object(),
            slots.Select(p => p.Position).ToArray(),
            0.21f,
            (p, r) =>
                new[] { -0.66f, -0.33f, 0f }.Any(x =>
                    WinchReservations.Overlap(p, r, new Vector3(x, 0.13f, 0f), 0.21f)
                )
        );
        Check(
            endReservation != null && endReservation.Position.x > 0.42f,
            "Bounded surface ends must remain available when the spacing grid is obstructed."
        );
        var unmeasured = new WinchMountDefinition(1, WinchRole.Left, Vector3.right, false, -1);
        Check(
            WinchPlacementGeometry.Candidates(unmeasured, Vector3.zero, 0.2f, Vector3.zero).Length
                == 0,
            "Unmeasured surface must not fall back to floating tangent positions."
        );
        Console.WriteLine(
            "PASS: 110 additional measured deck/rail mappings, native-neighbor clearance, support normals, bounded ends and finite reservations; all eight boats have authored surface support."
        );
    }
}
