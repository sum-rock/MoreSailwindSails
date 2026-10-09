using System;
using System.Diagnostics;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Optional aggregate CPU/frame diagnostics and reversible mount-only A/B comparison.
    // Timed paths allocate no samples or strings; reports are limited to ten-second windows.
    internal static class SpritsailMountProfile
    {
        internal const int Setup = 0;
        internal const int Surface = 1;
        internal const int Fit = 2;
        internal const int Upload = 3;
        internal const int Submit = 4;
        private static readonly long[] stages = new long[5];
        private static int mode;
        private static bool measuring;
        private static long windowStart;
        private static long totalTicks;
        private static long maxTicks;
        private static int calls;
        private static int refits;
        private static int hits;
        private static int misses;
        private static int frames;
        private static int slowFrames;
        private static int lastFrame = -1;
        private static int initialGc;
        private static double frameSeconds;
        private static double maxFrameSeconds;

        internal static bool Bypass => mode == 2;

        internal static void Tick(bool enabled, bool toggle)
        {
            if (!enabled)
            {
                Stop();
                return;
            }
            if (toggle)
            {
                Report();
                mode = (mode + 1) % 3;
                Reset();
                Plugin.Log.LogInfo(
                    data: "[MountProfile] "
                        + (
                            mode == 0 ? "Stopped; normal mount rendering restored."
                            : mode == 1
                                ? "NORMAL: recording mount CPU stages and frame times. Press the shortcut for BYPASS."
                            : "BYPASS: mount drawing and fitting skipped; sail physics unchanged. Press the shortcut to stop."
                        )
                );
            }
            if (mode == 0)
                return;
            if (lastFrame != Time.frameCount)
            {
                lastFrame = Time.frameCount;
                double seconds = Time.unscaledDeltaTime;
                frames++;
                frameSeconds += seconds;
                maxFrameSeconds = Math.Max(maxFrameSeconds, seconds);
                if (seconds > 1.0 / 30)
                    slowFrames++;
            }
            if ((Stopwatch.GetTimestamp() - windowStart) / (double)Stopwatch.Frequency >= 10)
            {
                Report();
                Reset();
            }
        }

        internal static long Begin()
        {
            if (mode != 1)
                return 0;
            measuring = true;
            return Stopwatch.GetTimestamp();
        }

        internal static void Mark(int stage, ref long checkpoint)
        {
            if (checkpoint == 0)
                return;
            long now = Stopwatch.GetTimestamp();
            stages[stage] += now - checkpoint;
            checkpoint = now;
        }

        internal static void Refit()
        {
            if (measuring)
                refits++;
        }

        internal static void SurfaceQuery(bool hit)
        {
            // Exclude the snotter's surface queries, which run outside Mount.Draw.
            if (!measuring)
                return;
            if (hit)
                hits++;
            else
                misses++;
        }

        internal static void End(long started)
        {
            if (started == 0)
                return;
            long elapsed = Stopwatch.GetTimestamp() - started;
            totalTicks += elapsed;
            maxTicks = Math.Max(maxTicks, elapsed);
            calls++;
            measuring = false;
        }

        internal static void Stop()
        {
            if (mode == 0)
                return;
            Report();
            mode = 0;
            measuring = false;
            Reset();
            Plugin.Log.LogInfo(data: "[MountProfile] Stopped; normal mount rendering restored.");
        }

        private static void Reset()
        {
            Array.Clear(array: stages, index: 0, length: stages.Length);
            totalTicks = maxTicks = 0;
            calls = refits = hits = misses = frames = slowFrames = 0;
            frameSeconds = maxFrameSeconds = 0;
            lastFrame = -1;
            initialGc = GC.CollectionCount(generation: 0);
            windowStart = Stopwatch.GetTimestamp();
        }

        private static void Report()
        {
            if (mode == 0 || frames == 0)
                return;
            double milliseconds = 1000.0 / Stopwatch.Frequency;
            double perFrame = milliseconds / frames;
            Plugin.Log.LogInfo(
                data: FormattableString.Invariant(
                    $"[MountProfile] mode={(mode == 1 ? "NORMAL" : "BYPASS")} frames={frames} frameMeanMs={frameSeconds * 1000 / frames:F2} frameMaxMs={maxFrameSeconds * 1000:F2} over33ms={slowFrames} callsPerFrame={calls / (double)frames:F2} refits={refits}/{calls} cacheHitMiss={hits}/{misses} mountCpuMsPerFrame={totalTicks * perFrame:F3} maxCallMs={maxTicks * milliseconds:F3} setupMsPerFrame={stages[Setup] * perFrame:F3} surfaceMsPerFrame={stages[Surface] * perFrame:F3} fitMsPerFrame={stages[Fit] * perFrame:F3} uploadMsPerFrame={stages[Upload] * perFrame:F3} submitMsPerFrame={stages[Submit] * perFrame:F3} gc0Global={GC.CollectionCount(generation: 0) - initialGc}"
                )
            );
        }
    }
}
