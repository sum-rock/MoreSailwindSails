using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Utils.Profiling;

namespace MoreSailwindSails.Tests.GeometryChecks.Utils;

// Checks GC correlation boundaries and optional allocation accounting without a game process.
internal static class ProfilingGcChecks
{
    internal static void Run()
    {
        Intervals();
        Transitions();
        Allocations();
        Console.WriteLine(
            "PASS: GC interval correlation, report exclusions, bounded outliers, optional allocation counters and allocation-free sampling."
        );
    }

    private static void Intervals()
    {
        var intervals = new ProfileIntervals();
        intervals.Start(now: 100);
        intervals.Sample(now: 101, gc: 10);
        intervals.Sample(now: 101.125, gc: 12);
        intervals.Sample(now: 101.1875, gc: 12);
        intervals.Sample(now: 101.21875, gc: 12);
        Check(
            intervals.Excluded == 1
                && intervals.WithGc.Count == 1
                && intervals.WithoutGc.Count == 2,
            "Starting interval is excluded; multiple collections still represent one interval."
        );
        Check(
            intervals.WithGc.Seconds == 0.125
                && intervals.WithGc.Maximum == 0.125
                && intervals.WithGc.Over50 == 1
                && intervals.WithGc.Over100 == 1
                && intervals.WithoutGc.Seconds == 0.09375
                && intervals.WithoutGc.Maximum == 0.0625
                && intervals.WithoutGc.Over50 == 1
                && intervals.WithoutGc.Over100 == 0,
            "Groups retain correct durations and hitch counts."
        );
        Check(
            intervals.LongestCount == 2
                && intervals.Longest[0].Collections == 2
                && intervals.Longest[0].EndSeconds == 1.125
                && intervals.Longest[1].Collections == 0,
            "Outliers preserve capture-relative end times and collection deltas."
        );
        var stats = new ProfileIntervalStats();
        stats.Add(seconds: 0.05);
        stats.Add(seconds: 0.1);
        Check(
            stats.Over50 == 1 && stats.Over100 == 0,
            "Thresholds are strictly greater than 50/100 ms."
        );
        intervals.Clear();
        intervals.Sample(now: 102, gc: 12);
        double now = 102;
        for (int i = 1; i <= 20; i++)
        {
            now += i * 0.125;
            intervals.Sample(now: now, gc: 12 + i);
        }
        Check(
            intervals.LongestCount == 8
                && intervals.Longest[0].Seconds == 2.5
                && intervals.Longest[7].Seconds == 1.625
                && intervals.Longest[0].EndSeconds == now - 100,
            "Only the eight longest hitches survive, with the capture origin retained across windows."
        );
        intervals.Sample(now: now + 0.0625, gc: 32);
        Check(
            intervals.Longest[7].Seconds == 1.625,
            "Shorter later hitches cannot replace the longest eight."
        );
        intervals.Sample(now: now - 1, gc: 32);
        intervals.Sample(now: now, gc: 0);
        intervals.Sample(now: double.NaN, gc: 0);
        intervals.Sample(now: now + 1, gc: 0);
        Check(intervals.Excluded == 5, "Clock/counter discontinuities are excluded and reseeded.");
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 1; i <= 1000; i++)
            intervals.Sample(now: now + 1 + i * 0.125, gc: i);
        Check(
            GC.GetAllocatedBytesForCurrentThread() == before,
            "Warmed interval sampling and outlier insertion allocate nothing."
        );
    }

    private static void Transitions()
    {
        var collector = new ProfileCollector(timestamp: () => 0);
        var reports = new List<(int WithGc, int WithoutGc, int Excluded)>();
        var logs = new List<string>();
        var session = new ProfileSession(
            collector: collector,
            report: s =>
            {
                reports.Add(
                    (s.Intervals.WithGc.Count, s.Intervals.WithoutGc.Count, s.Intervals.Excluded)
                );
                ProfileReport.Write(session: s, log: logs.Add);
            }
        );
        session.Configure(selection: ProfileSelection.All, bypass: ProfileBypass.Snotter);
        void Tick(int frame, double now, int gc, bool toggle = false) =>
            session.Tick(
                enabled: true,
                toggle: toggle,
                frame: frame,
                seconds: 0.02,
                now: now,
                gc: gc
            );
        Tick(frame: 1, now: 100, gc: 0, toggle: true);
        Tick(frame: 2, now: 101, gc: 1);
        Tick(frame: 2, now: 101, gc: 1);
        Tick(frame: 3, now: 101.125, gc: 2);
        Tick(frame: 4, now: 110, gc: 2);
        Check(
            reports.Single() == (1, 1, 1),
            "Duplicate frames are ignored and elapsed tick time is independent of Unity delta time."
        );
        Check(
            logs.Any(s => s.Contains("maxMs=125.00"))
                && logs.Any(s => s.Contains("allocations=unavailable"))
                && logs.Any(s => s.Contains("gc0Delta=1")),
            "Reports label GC intervals and unavailable allocation attribution."
        );
        Tick(frame: 5, now: 112, gc: 5); // Simulated reporting delay and logging allocations.
        Check(
            session.Intervals.Excluded == 1 && session.Intervals.WithGc.Count == 0,
            "The interval crossing report emission is excluded, including its collections."
        );
        Tick(frame: 6, now: 112.125, gc: 6, toggle: true);
        Check(
            reports.Last() == (1, 0, 1) && session.Mode == 2,
            "Mode transitions flush only the old mode's intervals."
        );
        Tick(frame: 7, now: 114, gc: 10);
        Tick(frame: 8, now: 114.125, gc: 10);
        Check(
            session.Intervals.Excluded == 1
                && session.Intervals.WithoutGc.Count == 1
                && session.Intervals.Longest[0].EndSeconds == 14.125,
            "A mode change excludes transition logging but retains capture-relative timestamps."
        );
        session.Configure(selection: 0, bypass: ProfileBypass.None);
        Check(
            session.Mode == 0 && session.Intervals.LongestCount == 0,
            "Configuration changes clear correlation state."
        );
        Tick(frame: 9, now: 200, gc: 20, toggle: true);
        Tick(frame: 10, now: 201, gc: 21);
        Tick(frame: 11, now: 201.125, gc: 22);
        Check(
            session.Intervals.Longest[0].EndSeconds == 1.125,
            "A new capture resets the timestamp origin."
        );
    }

    private static void Allocations()
    {
        Check(
            ProfileAllocationCounter.TryCreate(runtime: typeof(string)) == null,
            "A missing runtime API is unavailable, not zero allocation."
        );
        var counter = ProfileAllocationCounter.TryCreate(runtime: typeof(GC));
        Check(
            counter != null && counter() >= 0,
            "The .NET test runtime supplies the optional counter."
        );
        long bytes = 0;
        int reads = 0;
        var collector = new ProfileCollector(
            timestamp: () => 0,
            allocatedBytes: () =>
            {
                reads++;
                return bytes;
            }
        );
        using (collector.Begin(target: ProfileTarget.Rig)) { }
        collector.Active = true;
        using (collector.Begin(target: ProfileTarget.Rig)) { }
        Check(reads == 0, "Disabled and unselected scopes never read the allocation counter.");
        collector.Selection = ProfileSelection.All;
        using (collector.Begin(target: ProfileTarget.Rig, owner: ProfileFamily.BoomedSpritsail))
        {
            bytes = 10;
            using (collector.Begin(target: ProfileTarget.Shape))
            using (collector.Begin(target: ProfileTarget.Shape))
                bytes = 30;
            bytes = 40;
        }
        int rig = ProfileCollector.Index(
            family: ProfileFamily.BoomedSpritsail,
            target: ProfileTarget.Rig
        );
        int shape = ProfileCollector.Index(
            family: ProfileFamily.BoomedSpritsail,
            target: ProfileTarget.Shape
        );
        Check(
            reads == 4
                && collector.AllocatedBytes[rig] == 40
                && collector.MaximumAllocatedBytes[rig] == 40
                && collector.AllocatedBytes[shape] == 20,
            "Allocation totals are inclusive; identical nested categories coalesce."
        );
        using (collector.Begin(target: ProfileTarget.Rig, owner: ProfileFamily.BoomedSpritsail))
            bytes += 5;
        Check(
            collector.AllocatedBytes[rig] == 45 && collector.MaximumAllocatedBytes[rig] == 40,
            "Allocation sums and maximum calls remain distinct."
        );
        var stale = collector.Begin(target: ProfileTarget.Rig);
        collector.Clear();
        stale.Dispose();
        Check(
            collector.AllocatedBytes.All(n => n == 0)
                && collector.MaximumAllocatedBytes.All(n => n == 0),
            "Old scopes cannot leak allocation totals across windows."
        );
        var live = new ProfileCollector(timestamp: () => 0, allocatedBytes: counter)
        {
            Active = true,
            Selection = ProfileSelection.All,
        };
        void Sample()
        {
            using (live.Begin(target: ProfileTarget.Rig))
            using (live.Begin(target: ProfileTarget.Shape)) { }
        }
        for (int i = 0; i < 100; i++)
            Sample();
        long before = counter();
        for (int i = 0; i < 1000; i++)
            Sample();
        Check(
            counter() == before && live.AllocatedBytes.All(n => n == 0),
            "Optional attribution does not add warmed per-scope allocations."
        );
    }

    private static void Check(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message: message);
    }
}
