using System;
using System.Linq;
using MoreSailwindSails.Sails.FishermansFlyingSail;
using MoreSailwindSails.Sails.FishermansFlyingSail.Configuration;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansFlyingSail;

internal static class MastInstallationChecks
{
    internal static void Run()
    {
        CheckMastAxis();
        CheckCollisionEnvelope();
        var brig = FlyingSailCatalog.Find("BOAT medi medium (50)");
        var forwardPairs = brig.MastPairs(2).ToArray();
        Require(
            forwardPairs.Select(g => g.Key).OrderBy(i => i).SequenceEqual(new[] { 4, 5 }),
            "Selecting the physical foremast must find both supported mainmast options from its physical mast selection."
        );
        Require(
            forwardPairs
                .Single(g => g.Key == 4)
                .SelectMany(v => v.AftSections)
                .Distinct()
                .OrderBy(i => i)
                .SequenceEqual(new[] { 4, 58 }),
            "The physical mast pair must combine lower and optional topmast pulley candidates."
        );
        Require(
            brig.MastPairs(4).Select(g => g.Key).OrderBy(i => i).SequenceEqual(new[] { 6, 7 }),
            "The Brig mainmast must support a fisherman ahead of either mizzen option."
        );
        Require(
            !brig.MastPairs(6).Any() && !brig.MastPairs(999).Any(),
            "The last mast and unknown mast IDs must not invent an aft support."
        );
        Require(
            FishermansFlyingSailMastInstallationGeometry.FitError(8, 18, 20, 22, 22, 10) == null,
            "A supported mast installation was rejected."
        );
        Require(
            FishermansFlyingSailMastInstallationGeometry.FitError(10, 18, 20, 22, 22, 10) != null,
            "The sail must leave clearance at the aft mast."
        );
        Require(
            FishermansFlyingSailMastInstallationGeometry.FitError(8, 23, 20, 22, 22, 10) != null,
            "A sail above its supporting pulley was accepted."
        );
        Require(
            FishermansFlyingSailMastInstallationGeometry.FitError(8, 18, 23, 22, 22, 10) != null,
            "Rising aft head above its own guide was accepted."
        );
        Require(
            FishermansFlyingSailMastInstallationGeometry.FitError(8, 18, 23, 20, 24, 10) == null,
            "The aft head should use its own guide rather than the shorter fore guide."
        );
        Require(
            FishermansFlyingSailMastInstallationGeometry.FitError(
                9.7f + 0.4572f,
                18,
                20,
                22,
                22,
                10
            ) != null,
            "The mast gap must count toward available span."
        );
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, -1f, 0f })
            Require(
                FishermansFlyingSailMastInstallationGeometry.FitError(invalid, 18, 20, 22, 22, 10)
                    != null,
                "Invalid fitting geometry was accepted."
            );

        foreach (float width in new[] { 0.25f, 6f, 13.8f, 40f })
        foreach (float scale in new[] { 0.3f, 0.6f, 1f, 1.5f })
        {
            var cut = FishermansFlyingSailGeometry
                .Create(width)
                .Corners.Select(c => c * scale)
                .ToArray();
            var deck = cut[0] - Vector3.right * (width * scale * 2.6f);
            var previous = cut.Select(c =>
                    FishermansFlyingSailMastInstallationGeometry.HoistCorner(c, cut[0], deck, 0)
                )
                .ToArray();
            Near(previous[0], deck, "The head must begin at the deck gathering point.");
            for (int step = 0; step <= 100; step++)
            {
                float unroll = step / 100f;
                var posed = cut.Select(c =>
                        FishermansFlyingSailMastInstallationGeometry.HoistCorner(
                            c,
                            cut[0],
                            deck,
                            unroll
                        )
                    )
                    .ToArray();
                for (int i = 0; i < 4; i++)
                {
                    Require(
                        float.IsFinite(posed[i].x)
                            && float.IsFinite(posed[i].y)
                            && float.IsFinite(posed[i].z),
                        "Nonfinite hoist corner."
                    );
                    Require(
                        posed[i].x >= previous[i].x - 1e-5f,
                        "Hoisting moved cloth back toward the deck."
                    );
                    if (unroll >= 0.98f)
                        Near(
                            posed[i],
                            cut[i],
                            "Fully raised cloth did not regain the existing sail cut."
                        );
                    for (int j = i + 1; j < 4; j++)
                        Require(
                            Math.Abs(
                                (posed[i] - posed[j]).magnitude / (cut[i] - cut[j]).magnitude
                                    - FishermansFlyingSailMastInstallationGeometry.HoistScale(
                                        unroll
                                    )
                            ) < 1e-4f,
                            "Hoisting stretched one edge differently from the others."
                        );
                }
                // The tension solver must accept the new partially raised pose.
                var leech = new Vector3[FishermansFlyingSailGeometry.Rows + 1];
                bool fitted = FishermansFlyingSailTension.Fit(
                    posed[3],
                    posed[1],
                    posed[2],
                    Vector3.zero,
                    (cut[3] - cut[1]).magnitude
                        * FishermansFlyingSailMastInstallationGeometry.HoistScale(unroll),
                    (posed[3] - posed[2]).magnitude,
                    0,
                    leech
                );
                Require(fitted, "The hoist pose violates coupled foot/leech limits.");
                previous = posed;
            }
            foreach (float heel in new[] { -45f, 45f })
            {
                var half = FishermansFlyingSailMastInstallationGeometry.HoistCorner(
                    cut[0],
                    cut[0],
                    deck,
                    0.5f
                );
                var rotated = FishermansFlyingSailFrameGeometry.RotateAroundMast(
                    half,
                    Vector3.zero,
                    Vector3.forward,
                    heel
                );
                var expected = Vector3.Lerp(
                    FishermansFlyingSailFrameGeometry.RotateAroundMast(
                        deck,
                        Vector3.zero,
                        Vector3.forward,
                        heel
                    ),
                    FishermansFlyingSailFrameGeometry.RotateAroundMast(
                        cut[0],
                        Vector3.zero,
                        Vector3.forward,
                        heel
                    ),
                    0.5f
                );
                Near(rotated, expected, "Hoisting must follow the boat when it heels.");
            }
        }
        // At full strike the upper route collapses to the existing aft pulley.
        var pulley = new Vector3(0, 22, 10);
        var control = new Vector3(2, 2, 8);
        Near(
            FishermansFlyingSailRopeGeometry.DirectPoint(pulley, control, 0, 0),
            pulley,
            "Parked upper rope left its pulley."
        );
        Near(
            FishermansFlyingSailRopeGeometry.DirectPoint(pulley, control, 0, 1),
            control,
            "Parked upper rope lost its deck control."
        );
        Console.WriteLine(
            "PASS: mast-pair clearance, pulley height, deck-up hoisting, finite scaled poses, coupled tension, heel and parked ropes."
        );
    }

    private static void CheckMastAxis()
    {
        var bottom = new Vector3(4, 0, 12);
        var top = new Vector3(6, 30, 15);
        Near(
            FishermansFlyingSailMastInstallationGeometry.AtHeight(bottom, top, 15),
            new Vector3(5, 15, 13.5f),
            "Raked mast interpolation left the physical axis."
        );
        var shift = new Vector3(-50, 3, 90);
        Near(
            FishermansFlyingSailMastInstallationGeometry.AtHeight(bottom + shift, top + shift, 18),
            new Vector3(5, 15, 13.5f) + shift,
            "Mast interpolation depends on boat position."
        );
        Near(
            FishermansFlyingSailMastInstallationGeometry.AtHeight(bottom, top, -3),
            new Vector3(3.8f, -3, 11.7f),
            "Deck projection must extend the same mast axis below the mount."
        );
        foreach (
            var invalid in new[]
            {
                Vector3.right,
                new Vector3(0, -1, 0),
                new Vector3(float.NaN, 3, 0),
            }
        )
        {
            bool rejected = false;
            try
            {
                FishermansFlyingSailMastInstallationGeometry.AtHeight(Vector3.zero, invalid, 1);
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            Require(rejected, "Invalid mast axis was accepted.");
        }
    }

    private static void CheckCollisionEnvelope()
    {
        foreach (float scale in new[] { 0.3f, 0.5f, 0.65f, 1f, 1.5f })
        {
            const float width = 13.8f;
            float clearance = 0; // The entire panel is beyond the mast surface.
            int enabled = 0;
            for (int i = 0; i < FishermansFlyingSailGeometry.Columns; i++)
            {
                bool active = FishermansFlyingSailMastInstallationGeometry.CollisionStrip(
                    width,
                    i,
                    clearance,
                    out var center,
                    out var size
                );
                Require(
                    size.x > 0 && size.y > 0 && size.z > 0,
                    "Collision boxes must retain positive size."
                );
                if (!active)
                    continue;
                enabled++;
                float near =
                    (center.z - size.z * 0.5f + width) * scale
                    + FishermansFlyingSailFrameGeometry.TieLength;
                Require(
                    near >= FishermansFlyingSailFrameGeometry.TieLength - 0.00001f,
                    "Collision envelope includes intentional mast attachment contact."
                );
                float stripStart = center.z - size.z * 0.5f + width;
                float head = stripStart * FishermansFlyingSailGeometry.EdgeSlope;
                float foot = -2 * width - head;
                Require(
                    center.x + size.x * 0.5f <= head,
                    "Collision strip protrudes above the rising head."
                );
                Require(
                    center.x - size.x * 0.5f >= foot,
                    "Collision strip protrudes below the falling foot."
                );
                Require(center.z + size.z * 0.5f < 0, "Collision strip protrudes aft of the sail.");
                Require(
                    size.y * scale <= 0.0751f,
                    "Neutral collision check reserves full billow thickness."
                );
            }
            Require(
                enabled == FishermansFlyingSailGeometry.Columns,
                "Mast clearance removed too much of the sail's collision envelope."
            );
        }

        // With the eighteen-inch mast gap, even the first strip must detect obstacles.
        const float testScale = 0.65f;
        bool first = FishermansFlyingSailMastInstallationGeometry.CollisionStrip(
            13.8f,
            0,
            0,
            out _,
            out _
        );
        Require(first, "The offset panel must retain its first collision strip.");
        bool next = FishermansFlyingSailMastInstallationGeometry.CollisionStrip(
            13.8f,
            2,
            0,
            out var c,
            out var sz
        );
        Require(next && Contains(c, sz, c), "Genuine panel obstruction would be ignored.");
        Require(
            !Contains(c, sz, c + new Vector3(0, 0.45f / testScale, 0)),
            "Clear lateral shroud still intersects neutral panel."
        );
        Console.WriteLine(
            "PASS: thin trapezoid collision envelope, fixed mast gap, lateral shroud clearance and retained panel obstruction."
        );
    }

    private static bool Contains(Vector3 center, Vector3 size, Vector3 point) =>
        Math.Abs(point.x - center.x) <= size.x * 0.5f
        && Math.Abs(point.y - center.y) <= size.y * 0.5f
        && Math.Abs(point.z - center.z) <= size.z * 0.5f;

    private static void Near(Vector3 a, Vector3 b, string message) =>
        Require((a - b).magnitude < 0.001f, message);

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
