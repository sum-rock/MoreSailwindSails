using System;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Stays.FishermansStay;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

internal static class StayChecks
{
    internal static void Run()
    {
        var measurements = StaySparMeasurement.Load();
        // Installed Jong donor 58 has different visual/walking mesh bounds.
        // Both must map onto the same new stay endpoints without reflection.
        foreach (var interval in new[] { new[] { -18.125f, 1.149f }, new[] { -18f, 0.872f } })
        {
            FishermansStayGeometry.FitAxis(
                interval[0],
                interval[1],
                12f,
                out float scale,
                out float offset
            );
            Check(
                scale > 0
                    && Math.Abs(interval[0] * scale + offset + 12) < 0.00001f
                    && Math.Abs(interval[1] * scale + offset) < 0.00001f,
                "Visual and walking rope bounds missed their authored endpoints."
            );
        }
        var oldSnapshot = Enumerable.Repeat(1, 20).ToArray();
        Check(
            FishermansStaySaveState.RestoreOption(oldSnapshot, 20) == 0
                && FishermansStaySaveState.RestoreOption(oldSnapshot, 21) == 0,
            "Cancellation of a pre-stay snapshot retained a preview selection."
        );
        var newSnapshot = oldSnapshot.Concat(new[] { 3, 0 }).ToArray();
        Check(
            FishermansStaySaveState.RestoreOption(newSnapshot, 20) == 3
                && FishermansStaySaveState.RestoreOption(newSnapshot, 21) == 0
                && FishermansStaySaveState.RestoreOption(null, 20) == 0,
            "Saved stay choices or empty snapshots were not restored correctly."
        );
        int variants = 0,
            fallback = 0;
        int[] counts = { 24, 9, 9, 26, 3, 8, 14, 3, 2, 1, 2, 2 };
        for (int boatIndex = 0; boatIndex < BoatRigCatalog.All.Count; boatIndex++)
        {
            var boat = BoatRigCatalog.All[boatIndex];
            var masts = measurements[boat.BoatName];
            var stays = boat.Stays.SelectMany(g => g.Variants).ToArray();
            Check(
                stays.Length == counts[boatIndex],
                "Stay variant coverage changed: " + boat.BoatName
            );
            Check(
                stays.Select(s => s.MountIndex).SequenceEqual(Enumerable.Range(128, stays.Length)),
                "Stable stay mount slots changed."
            );
            foreach (var stay in stays)
            {
                var fore = masts[stay.Fore].Point(stay.ForePoint);
                var aftSpar = masts[stay.Aft];
                var aft = aftSpar.Point(stay.AftPoint);
                Check(
                    OnSegment(fore, masts[stay.Fore]) && OnSegment(aft, aftSpar),
                    "Attachment left a physical spar."
                );
                var down = (aftSpar.Bottom - aftSpar.Top).normalized;
                if (stay.AlignGuideHeightToAftAnchor)
                {
                    var guide = FishermansStayGeometry.AlignGuideHeight(aftSpar.Guide, aft, -down);
                    Check(
                        Math.Abs(Vector3.Dot(guide - aft, down)) < 0.00001f
                            && Vector3.ProjectOnPlane(guide - aftSpar.Guide, down).magnitude
                                < 0.00001f,
                        "Aligned guide changed radial offset or missed attachment height."
                    );
                    var pose = new Vector3(25f, -8f, 17f);
                    Check(
                        (
                            FishermansStayGeometry.AlignGuideHeight(
                                Rotate(aftSpar.Guide) + pose,
                                Rotate(aft) + pose,
                                Rotate(-down)
                            ) - (Rotate(guide) + pose)
                        ).magnitude < 0.00001f,
                        "Aligned halyard guide drifted with boat pose."
                    );
                }
                else
                {
                    Check(
                        Math.Abs(Vector3.Dot(aft - aftSpar.Guide, down)) < 0.025f,
                        "Aft anchor left the authored halyard height."
                    );
                    float angle = Angle(fore - aft, down);
                    if (Math.Abs(angle - 70f) > 0.02f)
                    {
                        Check(angle < 70, "Fallback must steepen toward the shorter foremast.");
                        Check(
                            (fore - masts[stay.Fore].Top).magnitude < 0.002f,
                            "Fallback must meet the foremast head."
                        );
                        fallback++;
                    }
                    Check(fore.y < aft.y, "Stay must descend toward the foremast.");
                }
                // Native-guide-height variants exclude continuations above the selected aft section.
                // Optional T'gallants on authored aligned-guide stays are covered by their profile checks.
                if (!stay.AlignGuideHeightToAftAnchor)
                    foreach (var upper in masts.Values.Where(m => m.Parents.Contains(stay.Aft)))
                        Check(
                            stay.Forbidden.Contains(upper.Id),
                            "A lower stay remains eligible beneath a topmast."
                        );
                Check(
                    stay.Required.Contains(stay.Fore) && stay.Required.Contains(stay.Aft),
                    "Missing supporting section dependency."
                );
                var shifted = new Vector3(150, -12, 78);
                float span = FishermansStayGeometry.Span(aft, fore);
                Check(
                    Math.Abs(
                        span
                            - FishermansStayGeometry.Span(
                                Rotate(aft) + shifted,
                                Rotate(fore) + shifted
                            )
                    ) < 0.0001f,
                    "Stay span changed with boat pose."
                );
                variants++;
            }
        }
        Check(fallback == 22, "Shorter-foremast fallback coverage changed.");
        // The large dhow's foremast capsules extend 6.7 cm beyond the rendered
        // end rings. Both upright and raked options must use the visible spar.
        var dhowSpars = measurements[LargeDhow.Definition.BoatName];
        foreach (int id in new[] { 0, 1 })
            Check(
                !OnSegment(dhowSpars[id].Point(new Vector3(-0.006836f, 0f, 2.29f)), dhowSpars[id]),
                "Large dhow capsule tip incorrectly accepted as a visible masthead."
            );
        Check(
            LargeDhow
                .Definition.Stays.SelectMany(g => g.Variants)
                .Count(s => s.Fore == 0 || s.Fore == 1) == 8,
            "Missing large dhow foremast endpoint coverage."
        );
        // Actual Brig coordinates straddle the boundary: the same authored aft
        // anchor meets a bare foremast head, or the preferred line on its topmast.
        var brig = BoatRigCatalog.All[0].Stays[0].Variants;
        var bare = brig.Single(s => s.MountIndex == 129);
        var tall = brig.Single(s => s.MountIndex == 131);
        var data = measurements[BoatRigCatalog.All[0].BoatName];
        var start = data[bare.Aft].Point(bare.AftPoint);
        var target = data[tall.Fore].Point(tall.ForePoint);
        var direction = (data[bare.Aft].Bottom - data[bare.Aft].Top).normalized;
        Check(
            Angle(data[bare.Fore].Point(bare.ForePoint) - start, direction) < 70,
            "Bare foremast did not steepen the stay."
        );
        Check(
            Math.Abs(Angle(target - start, direction) - 70) < 0.02,
            "Topmast lost the preferred angle."
        );
        Check(
            FishermansStayGeometry.OnSpar(new Vector3(0, 0, 10), new Vector3(0, 0, 5), 2, 10, 0.1f),
            "Exact masthead attachment rejected."
        );
        Check(
            !FishermansStayGeometry.OnSpar(
                new Vector3(0, 0, 10.1f),
                new Vector3(0, 0, 5),
                2,
                10,
                0.1f
            ),
            "Floating attachment accepted."
        );
        Reject(() => FishermansStayGeometry.Span(Vector3.zero, Vector3.zero));
        Reject(() => FishermansStayGeometry.Span(new Vector3(float.NaN, 0, 0), Vector3.zero));
        var sample = brig[0];
        Reject(() =>
            new FishermansStayVariantDefinition(
                128,
                15,
                "bad",
                3,
                Vector3.zero,
                5,
                Vector3.zero,
                false,
                0,
                new[] { 3, 5 },
                new[] { 5 }
            )
        );
        Reject(() =>
            new BoatRigDefinition(
                "duplicate",
                Brig.Definition.Supports,
                new[] { new FishermansStayGroupDefinition("test", new[] { sample, sample }) },
                Brig.Definition.MastParents,
                Brig.Definition.SheetCategories
            )
        );
        foreach (
            string text in new[]
            {
                "192: (ERROR): Fisherman's Stay (foremast 1 / main topmast 2) -> (no Fisherman's Stay)",
                "Fisherman's Stay " + new string('x', 200),
                "Fisherman's Stay\nrequires: main mast and topmast",
            }
        )
        {
            Check(
                FishermansStayOrderText.NeedsWrapping(text),
                "Stay order escaped freeze protection."
            );
            var wrapped = FishermansStayOrderText.Wrap(text).ToArray();
            Check(
                wrapped.All(l => l.Length <= 45),
                "Stay text wrapping failed to terminate safely."
            );
            Check(
                string.Concat(wrapped).Replace(" ", "") == text.Replace(" ", "").Replace("\n", ""),
                "Stay order details were lost."
            );
        }
        Check(
            !FishermansStayOrderText.NeedsWrapping(
                "Fisherman's Flying Sail " + new string('x', 60)
            ),
            "Stay guard changed flying-sail scope."
        );
        Console.WriteLine(
            $"PASS: {variants} authored stays across {BoatRigCatalog.All.Count} Fisherman boat profiles, {fallback} masthead fallbacks, physical endpoints, topmast dependencies, stable IDs, pose invariance and order text."
        );
    }

    private static float Angle(Vector3 a, Vector3 b) =>
        (float)(
            Math.Acos(Math.Max(-1, Math.Min(1, Vector3.Dot(a.normalized, b.normalized))))
            * 180
            / Math.PI
        );

    private static bool OnSegment(Vector3 p, StaySparMeasurement s)
    {
        var axis = s.Top - s.Bottom;
        float t = Vector3.Dot(p - s.Bottom, axis) / axis.sqrMagnitude;
        return t >= -0.001f && t <= 1.001f && (p - (s.Bottom + axis * t)).magnitude < 0.002f;
    }

    private static Vector3 Rotate(Vector3 p) => new Vector3(p.y, p.z, p.x);

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    private static void Reject(Action action)
    {
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            return;
        }
        throw new Exception("Invalid stay definition accepted.");
    }
}
