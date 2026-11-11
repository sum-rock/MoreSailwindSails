using System;
using System.Linq;
using MoreSailwindSails.BoatConfiguration.Boats;
using MoreSailwindSails.Sails.FishermansStaysail;
using MoreSailwindSails.Stays.FishermansStay.Configuration;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail;

internal static class CollisionChecks
{
    internal static void Run()
    {
        CheckLuffMastCompatibility();
        CheckLuffIntervals();
        var marks = new (string Name, Func<float, float, FishermansStaysailMeshData> Create)[]
        {
            ("Mk.A", BehaviorCases.CreateA),
            ("Mk.B", BehaviorCases.CreateB),
            ("Mk.C", BehaviorCases.CreateC),
        };
        foreach (var mark in marks)
        foreach (float width in new[] { 0.25f, 3f, 13.8f, 100f })
        foreach (float slope in new[] { 0f, 8.483f, 20f, 55f, 80f })
        foreach (
            float clearance in new[]
            {
                0f,
                width * 0.1f,
                width * 0.95f / FishermansStaysailGeometry.Columns - 0.0001f,
                width,
                width * 1.1f,
            }
        )
        {
            var corners = mark.Create(width, slope).Corners;
            int enabled = 0;
            for (int column = 0; column < FishermansStaysailGeometry.Columns; column++)
            {
                bool active = FishermansStaysailInstallationGeometry.CollisionStrip(
                    corners,
                    column,
                    clearance,
                    out var center,
                    out var size
                );
                if (
                    !float.IsFinite(center.sqrMagnitude)
                    || !float.IsFinite(size.sqrMagnitude)
                    || size.x <= 0
                    || size.y <= 0
                    || size.z <= 0
                )
                    throw new Exception(
                        mark.Name + " collision strip must stay finite and positive."
                    );
                if (!active)
                    continue;
                enabled++;
                float tolerance = width * 1e-5f;
                if (
                    center.z - size.z * 0.5f < -width + clearance - tolerance
                    || center.z + size.z * 0.5f > tolerance
                )
                    throw new Exception(
                        mark.Name + " collision strip exceeds mast/span clearance."
                    );
                // Check the entire rectangle against the cut, including both ends.
                for (int sample = 0; sample <= 4; sample++)
                {
                    float z = center.z + size.z * (sample / 4f - 0.5f);
                    float u = (z + width) / width;
                    float head = Vector3.Lerp(corners[0], corners[1], u).x;
                    float foot = Vector3.Lerp(corners[2], corners[3], u).x;
                    if (
                        center.x + size.x * 0.5f > head - 0.05f + tolerance
                        || center.x - size.x * 0.5f < foot + 0.05f - tolerance
                    )
                        throw new Exception(
                            $"{mark.Name} collision strip leaves its cut: width {width}, slope {slope}, column {column}."
                        );
                }
            }
            if (clearance >= width && enabled != 0)
                throw new Exception("Fully clipped collision strips must be disabled.");
            if (clearance < width && enabled == 0)
                throw new Exception(mark.Name + " lost all usable collision strips.");
        }
        var clipped = BehaviorCases.CreateC(3).Corners;
        float lastEnd =
            3f * (FishermansStaysailGeometry.Columns - 0.05f) / FishermansStaysailGeometry.Columns;
        if (
            FishermansStaysailInstallationGeometry.CollisionStrip(
                clipped,
                FishermansStaysailGeometry.Columns - 1,
                lastEnd - 0.0001f,
                out _,
                out _
            )
        )
            throw new Exception("A clipped sliver must not be expanded beyond the mast clearance.");
        Console.WriteLine(
            "PASS (executed): all three cuts retain collision strips inside both edges and mast clearance; shallow/clipped strips are disabled."
        );
    }

    private static void CheckLuffIntervals()
    {
        // A staysail luff spans heights 8..12. Include separated, touching,
        // partially overlapping and contained gaff/spritsail luffs in both orders.
        var cases = new[]
        {
            (Bottom: 0f, Top: 7.9f, Overlap: false),
            (Bottom: 0f, Top: 8f, Overlap: true),
            (Bottom: 4f, Top: 9f, Overlap: true),
            (Bottom: 9f, Top: 11f, Overlap: true),
            (Bottom: 0f, Top: 15f, Overlap: true),
            (Bottom: 12f, Top: 15f, Overlap: true),
            (Bottom: 12.1f, Top: 15f, Overlap: false),
        };
        foreach (float rake in new[] { -20f, 0f, 25f })
        {
            var rotation =
                MoreSailwindSails.Sails.LooseFootedSpritsail.LooseFootedSpritsailSailingGeometry.PivotRotation(
                    center: Vector3.zero,
                    axis: Vector3.right,
                    degrees: rake
                );
            var offset = new Vector3(x: 120f, y: 30f, z: -70f);
            Vector3 Point(float height) =>
                offset + rotation.MultiplyVector(vector: Vector3.up * height);
            var axis = rotation.MultiplyVector(vector: Vector3.up);
            foreach (var item in cases)
            foreach (bool reverse in new[] { false, true })
            {
                var firstHead = reverse ? Point(item.Top) : Point(12);
                var firstFoot = reverse ? Point(item.Bottom) : Point(8);
                var secondHead = reverse ? Point(12) : Point(item.Top);
                var secondFoot = reverse ? Point(8) : Point(item.Bottom);
                if (
                    FishermansStaysailInstallationGeometry.LuffsOverlap(
                        firstHead: firstHead,
                        firstFoot: firstFoot,
                        secondHead: secondHead,
                        secondFoot: secondFoot,
                        axis: axis
                    ) != item.Overlap
                )
                    throw new Exception(
                        "Luff overlap changed with rake, position or installation order."
                    );
            }
        }
        Console.WriteLine(
            "PASS: separated, touching, crossing and contained luffs in both orders on upright/raked masts."
        );
    }

    private static void CheckLuffMastCompatibility()
    {
        int variants = 0;
        foreach (var profile in FishermansStayCatalog.All)
        foreach (var stay in profile.Stays.SelectMany(group => group.Variants))
        {
            variants++;
            foreach (int forwardSection in profile.Boat.Sections(section: stay.Fore))
                if (
                    !FishermansStaysailInstallationGeometry.SharesLuffMast(
                        boat: profile.Boat,
                        forwardSection: stay.Fore,
                        squareSection: forwardSection
                    )
                )
                    throw new Exception(
                        "A square sail on the forward mast must coexist with the staysail: "
                            + stay.Label
                    );
            foreach (int aftSection in profile.Boat.Sections(section: stay.Aft))
                if (
                    FishermansStaysailInstallationGeometry.SharesLuffMast(
                        boat: profile.Boat,
                        forwardSection: stay.Fore,
                        squareSection: aftSection
                    )
                )
                    throw new Exception(
                        "The aft mast must retain native sail collision checks: " + stay.Label
                    );
            if (
                FishermansStaysailInstallationGeometry.SharesLuffMast(
                    boat: profile.Boat,
                    forwardSection: stay.Fore,
                    squareSection: stay.MountIndex
                )
            )
                throw new Exception("A stay slot is not the luff's physical mast.");
        }

        // Brig's fore topmast 57 belongs to base 2; main topmast 58 belongs to 4.
        foreach (var pair in new[] { (2, 57), (57, 2), (57, 57) })
            if (
                !FishermansStaysailInstallationGeometry.SharesLuffMast(
                    boat: Brig.Definition,
                    forwardSection: pair.Item1,
                    squareSection: pair.Item2
                )
            )
                throw new Exception(
                    "Base/topmast compatibility must work in both ancestry directions."
                );
        foreach (var pair in new[] { (2, 58), (57, 4), (2, -1), (-1, 2), (999, 999) })
            if (
                FishermansStaysailInstallationGeometry.SharesLuffMast(
                    boat: Brig.Definition,
                    forwardSection: pair.Item1,
                    squareSection: pair.Item2
                )
            )
                throw new Exception(
                    "Different or unknown mast sections must keep collision checks."
                );
        if (
            FishermansStaysailInstallationGeometry.SharesLuffMast(
                boat: null,
                forwardSection: 2,
                squareSection: 2
            )
        )
            throw new Exception("An unknown boat must keep collision checks.");
        Console.WriteLine(
            $"PASS (executed): shared-luff square-sail compatibility across {variants} stays, base/topmast ancestry, and aft/unknown-mast exclusions."
        );
    }
}
