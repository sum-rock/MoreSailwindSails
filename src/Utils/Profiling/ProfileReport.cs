using System;
using System.Diagnostics;
using System.Text;

namespace MoreSailwindSails.Utils.Profiling
{
    // Formats bounded-window logs outside timed paths; nested CPU totals are inclusive.
    internal static class ProfileReport
    {
        internal static void Write(ProfileSession session, Action<string> log)
        {
            var prefix =
                $"[PerformanceProfile] session={session.SessionId} window={session.WindowId} mode={(session.Mode == 1 ? "NORMAL" : "BYPASS")} bypass={session.Bypass}";
            log(
                FormattableString.Invariant(
                    $"{prefix} scope=all-loaded-boats frames={session.Frames} frameMeanMs={session.FrameSeconds * 1000 / session.Frames:F2} frameMaxMs={session.MaxFrameSeconds * 1000:F2} fps={(session.FrameSeconds > 0 ? session.Frames / session.FrameSeconds : 0):F2} over33ms={session.SlowFrames} gc0Global={session.GcCollections} targets={Names(selection: session.Collector.Selection)} cpu=inclusive-not-additive excludes=Unity-Cloth-and-GPU"
                )
            );
            double milliseconds = 1000.0 / Stopwatch.Frequency;
            for (var family = ProfileFamily.Unspecified; family < ProfileFamily.Count; family++)
            {
                for (var target = ProfileTarget.Rig; target < ProfileTarget.Count; target++)
                {
                    if (!session.Collector.Selected(target: target))
                        continue;
                    for (var stage = ProfileStage.Total; stage < ProfileStage.Count; stage++)
                    {
                        int index = ProfileCollector.Index(
                            family: family,
                            target: target,
                            stage: stage
                        );
                        int calls = session.Collector.Calls[index];
                        if (calls == 0)
                            continue;
                        log(
                            FormattableString.Invariant(
                                $"{prefix} family={family} target={target} stage={stage} calls={calls} callsPerFrame={calls / (double)session.Frames:F2} cpuMsPerFrame={session.Collector.Ticks[index] * milliseconds / session.Frames:F3} maxCallMs={session.Collector.Maximum[index] * milliseconds:F3}"
                            )
                        );
                    }
                }
                var counters = new StringBuilder();
                for (
                    var counter = ProfileCounter.MountUploads;
                    counter < ProfileCounter.Count;
                    counter++
                )
                {
                    int count = session.Collector.Counters[
                        (int)family * (int)ProfileCounter.Count + (int)counter
                    ];
                    if (count != 0)
                        counters.Append(' ').Append(counter).Append('=').Append(count);
                }
                if (counters.Length > 0)
                    log($"{prefix} family={family} counters:{counters}");
            }
        }

        internal static string Names(int selection)
        {
            var names = new StringBuilder();
            for (var target = ProfileTarget.Rig; target < ProfileTarget.Count; target++)
                if ((selection & (1 << (int)target)) != 0)
                {
                    if (names.Length > 0)
                        names.Append(',');
                    names.Append(target);
                }
            return names.Length == 0 ? "FrameStatisticsOnly" : names.ToString();
        }
    }
}
