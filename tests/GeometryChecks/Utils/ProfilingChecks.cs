using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Utils.Profiling;

namespace MoreSailwindSails.Tests.GeometryChecks.Utils;

// Exercises the capture engine without Unity, real clocks, or game-state mutations.
internal static class ProfilingChecks
{
    internal static void Run()
    {
        Selection();
        Timing();
        Sessions();
        Console.WriteLine(
            "PASS: profiling selections, inclusive timing, attribution, exception cleanup, mode/window transitions, and allocation-free warmed collection."
        );
    }

    private static void Selection()
    {
        var warnings = new List<string>();
        int selected = ProfileSelection.Parse(
            text: " rig, SHAPE,rig, unknown,UNKNOWN, ,0,Count",
            warn: warnings.Add
        );
        Check(
            selected == ((1 << (int)ProfileTarget.Rig) | (1 << (int)ProfileTarget.Shape)),
            "Only named, selected categories are enabled."
        );
        Check(
            warnings.Count == 3,
            "Each distinct unknown option warns once; numeric/sentinel values are invalid."
        );
        Check(
            ProfileSelection.Parse(text: "All, Ropes", warn: warnings.Add) == ProfileSelection.All,
            "All selects every category."
        );
        Check(
            ProfileSelection.Parse(text: " , ", warn: warnings.Add) == 0,
            "Empty selection permits frame-only capture."
        );
        Check(
            ProfileSelection.ParseBypass(text: " sNoTtEr ", warn: warnings.Add)
                == ProfileBypass.Snotter,
            "Bypass names ignore case/whitespace."
        );
        Check(
            ProfileSelection.ParseBypass(text: "Rig", warn: warnings.Add) == ProfileBypass.None,
            "Unknown/unsafe bypass fails closed."
        );
    }

    private static void Timing()
    {
        long now = 0;
        int clockReads = 0;
        var collector = new ProfileCollector(timestamp: () =>
        {
            clockReads++;
            return now;
        });
        using (collector.Begin(target: ProfileTarget.Rig, owner: ProfileFamily.BoomedSpritsail)) { }
        collector.Count(target: ProfileTarget.VisualCache, counter: ProfileCounter.Sheet);
        Check(
            clockReads == 0
                && collector.Calls.All(c => c == 0)
                && collector.Counters.All(c => c == 0),
            "Disabled hooks collect nothing and never read the clock."
        );
        collector.Active = true;
        collector.Selection =
            (1 << (int)ProfileTarget.Shape) | (1 << (int)ProfileTarget.VisualCache);
        using (collector.Begin(target: ProfileTarget.Rig, owner: ProfileFamily.BoomedSpritsail))
        {
            Check(
                clockReads == 0,
                "Unselected parent supplies attribution without reading the clock."
            );
            using (collector.Begin(target: ProfileTarget.Shape))
            {
                now = 10;
                using (collector.Begin(target: ProfileTarget.Shape))
                {
                    now = 20;
                }
                collector.Count(target: ProfileTarget.VisualCache, counter: ProfileCounter.Sheet);
            }
        }
        int shape = ProfileCollector.Index(
            family: ProfileFamily.BoomedSpritsail,
            target: ProfileTarget.Shape
        );
        Check(
            collector.Calls[shape] == 1 && collector.Ticks[shape] == 20 && clockReads == 2,
            "Same-category nested work is included exactly once."
        );
        Check(
            collector.Counters[
                (int)ProfileFamily.BoomedSpritsail * (int)ProfileCounter.Count
                    + (int)ProfileCounter.Sheet
            ] == 1,
            "Counters inherit family attribution."
        );
        collector.Clear();
        collector.Selection = ProfileSelection.All;
        now = 0;
        using (collector.Begin(target: ProfileTarget.Rig, owner: ProfileFamily.FlyingSail))
        {
            now = 3;
            using (collector.Begin(target: ProfileTarget.Ropes))
            {
                now = 8;
            }
            using (collector.Begin(target: ProfileTarget.Shape, owner: ProfileFamily.Staysail))
            {
                now = 10;
            }
            now = 12;
        }
        Check(
            collector.Ticks[ProfileCollector.Index(ProfileFamily.FlyingSail, ProfileTarget.Rig)]
                == 12,
            "Parent timings are inclusive."
        );
        Check(
            collector.Ticks[ProfileCollector.Index(ProfileFamily.FlyingSail, ProfileTarget.Ropes)]
                == 5,
            "Nested target retains the parent family."
        );
        Check(
            collector.Ticks[ProfileCollector.Index(ProfileFamily.Staysail, ProfileTarget.Shape)]
                == 2,
            "Explicit nested family remains separate."
        );
        try
        {
            using (
                collector.Begin(
                    target: ProfileTarget.SailMount,
                    owner: ProfileFamily.LooseFootedSpritsail
                )
            )
            {
                using (
                    collector.Begin(target: ProfileTarget.SailMount, stage: ProfileStage.Surface)
                )
                {
                    collector.SurfaceQuery(hit: true);
                    now += 4;
                    throw new InvalidOperationException("expected");
                }
            }
        }
        catch (InvalidOperationException) { }
        collector.SurfaceQuery(hit: false);
        Check(
            collector.Counters[
                (int)ProfileFamily.LooseFootedSpritsail * (int)ProfileCounter.Count
                    + (int)ProfileCounter.SurfaceHits
            ] == 1,
            "Mount queries are attributed to their owning family."
        );
        Check(
            collector
                .Counters.Where(
                    (_, i) => i % (int)ProfileCounter.Count == (int)ProfileCounter.SurfaceMisses
                )
                .All(n => n == 0),
            "Queries outside mount timing are excluded."
        );
        using (
            collector.Begin(
                target: ProfileTarget.SailMount,
                owner: ProfileFamily.LooseFootedSpritsail
            )
        )
        {
            now++;
        }
        Check(
            collector.Calls[
                ProfileCollector.Index(ProfileFamily.LooseFootedSpritsail, ProfileTarget.SailMount)
            ] == 2,
            "An exception cannot leave a timing category open."
        );
        var stale = collector.Begin(target: ProfileTarget.Rig, owner: ProfileFamily.FlyingSail);
        collector.Clear();
        stale.Dispose();
        Check(
            collector.Calls.All(n => n == 0),
            "Scopes from an ended window cannot pollute a new capture."
        );
        void Sample()
        {
            using (collector.Begin(target: ProfileTarget.Rig, owner: ProfileFamily.BoomedSpritsail))
            using (collector.Begin(target: ProfileTarget.Ropes))
                collector.Count(
                    target: ProfileTarget.VisualCache,
                    counter: ProfileCounter.SnotterReuse
                );
        }
        for (int i = 0; i < 100; i++)
            Sample();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
            Sample();
        Check(
            GC.GetAllocatedBytesForCurrentThread() == before,
            "Warmed nested scopes and counters must allocate no managed memory."
        );
    }

    private static void Sessions()
    {
        var collector = new ProfileCollector(timestamp: () => 0);
        var reports = new List<(int Mode, int Frames, int Slow, double Seconds, int Gc)>();
        var session = new ProfileSession(
            collector: collector,
            report: s =>
                reports.Add((s.Mode, s.Frames, s.SlowFrames, s.FrameSeconds, s.GcCollections))
        );
        session.Configure(selection: ProfileSelection.All, bypass: ProfileBypass.SailMount);
        Check(
            session.Mode == 0 && !collector.Active && !session.IsBypassed(ProfileBypass.SailMount),
            "Configuration starts OFF."
        );
        void Tick(
            int frame,
            double now,
            bool toggle = false,
            bool enabled = true,
            double seconds = .02,
            int gc = 3
        ) =>
            session.Tick(
                enabled: enabled,
                toggle: toggle,
                frame: frame,
                seconds: seconds,
                now: now,
                gc: gc
            );
        Tick(frame: 0, now: 0, toggle: true);
        Check(
            session.Mode == 1 && session.SessionId == 1,
            "First press starts a new NORMAL session."
        );
        Tick(frame: 1, now: 1);
        Tick(frame: 1, now: 1);
        Tick(frame: 2, now: 10, seconds: .04, gc: 4);
        Check(
            reports.Count == 1 && reports[0] == (1, 2, 1, .06, 1),
            "Ten-second windows count frames once and report frame/GC data."
        );
        Tick(frame: 3, now: 11, toggle: true);
        Check(
            reports.Count == 2 && reports[1].Frames == 1 && session.Mode == 2,
            "Mode changes flush partial windows before changing mode."
        );
        foreach (
            var bypass in new[]
            {
                ProfileBypass.SailMount,
                ProfileBypass.Snotter,
                ProfileBypass.SpritsailLiveRopes,
            }
        )
        {
            session.Configure(selection: ProfileSelection.All, bypass: bypass);
            Tick(frame: 4, now: 12, toggle: true);
            Tick(frame: 5, now: 13, toggle: true);
            Check(
                session.IsBypassed(bypass) && !session.IsBypassed(ProfileBypass.None),
                "Only the configured visual target is bypassed."
            );
            Tick(frame: 6, now: 14, toggle: true);
            Check(
                session.Mode == 0 && !collector.Active && !session.IsBypassed(bypass),
                "Third press restores normal behavior."
            );
        }
        session.Configure(selection: 0, bypass: ProfileBypass.None);
        Tick(frame: 7, now: 15, toggle: true);
        Tick(frame: 8, now: 16, toggle: true);
        Check(session.Mode == 0, "None uses a two-state cycle.");
        session.Configure(selection: ProfileSelection.All, bypass: ProfileBypass.Snotter);
        Tick(frame: 9, now: 17, toggle: true);
        Tick(frame: 10, now: 18, toggle: true);
        Tick(frame: 11, now: 19, enabled: false);
        Check(
            session.Mode == 0 && !session.IsBypassed(ProfileBypass.Snotter),
            "Disabling restores visual behavior."
        );
        Tick(frame: 12, now: 20, toggle: true);
        Tick(frame: 13, now: 21, toggle: true);
        session.Configure(selection: 0, bypass: ProfileBypass.None);
        Check(
            session.Mode == 0 && !collector.Active && collector.Selection == 0,
            "Changing configuration stops capture and clears old samples."
        );
        int count = reports.Count;
        session.Stop();
        session.Stop();
        Check(reports.Count == count, "Stopping repeatedly cannot emit duplicate reports.");
    }

    private static void Check(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }
}
