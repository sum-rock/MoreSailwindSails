using System;

namespace MoreSailwindSails.Utils.Profiling
{
    // Owns capture transitions and frame windows; reports consume state before it is reset.
    internal sealed class ProfileSession
    {
        internal readonly ProfileCollector Collector;
        internal readonly ProfileIntervals Intervals = new ProfileIntervals();
        private readonly Action<ProfileSession> report;
        private double windowStart;
        private int lastFrame = -1;
        private int initialGc;
        internal int Mode { get; private set; }
        internal int SessionId { get; private set; }
        internal int WindowId { get; private set; }
        internal ProfileBypass Bypass { get; private set; }
        internal int Frames { get; private set; }
        internal int SlowFrames { get; private set; }
        internal double FrameSeconds { get; private set; }
        internal double MaxFrameSeconds { get; private set; }
        internal int GcCollections { get; private set; }

        internal ProfileSession(ProfileCollector collector, Action<ProfileSession> report)
        {
            Collector = collector;
            this.report = report;
        }

        internal void Configure(int selection, ProfileBypass bypass)
        {
            Stop();
            Collector.Selection = selection;
            Bypass = bypass;
        }

        internal bool IsBypassed(ProfileBypass target) =>
            Mode == 2 && target != ProfileBypass.None && Bypass == target;

        internal void Tick(bool enabled, bool toggle, int frame, double seconds, double now, int gc)
        {
            if (!enabled)
            {
                Stop();
                return;
            }
            if (Mode != 0 && lastFrame != frame)
            {
                lastFrame = frame;
                Intervals.Sample(now: now, gc: gc);
                Frames++;
                FrameSeconds += seconds;
                MaxFrameSeconds = Math.Max(MaxFrameSeconds, seconds);
                if (seconds > 1.0 / 30)
                    SlowFrames++;
                GcCollections = gc - initialGc;
            }
            if (toggle)
            {
                Flush();
                Mode = (Mode + 1) % (Bypass == ProfileBypass.None ? 2 : 3);
                if (Mode == 1)
                {
                    SessionId++;
                    WindowId = 0;
                    Intervals.Start(now: now);
                }
                Reset(now: now, gc: gc);
                Collector.Active = Mode != 0;
            }
            else if (Mode != 0 && now - windowStart >= 10)
            {
                Flush();
                Reset(now: now, gc: gc);
            }
        }

        internal void Stop()
        {
            Flush();
            Mode = 0;
            Collector.Active = false;
            Reset(now: 0, gc: 0);
        }

        private void Flush()
        {
            if (Mode != 0 && Frames > 0)
                report(this);
        }

        private void Reset(double now, int gc)
        {
            Collector.Clear();
            // The next interval crosses logging or a capture boundary; seed it, do not measure it.
            Intervals.Clear();
            Frames = SlowFrames = GcCollections = 0;
            FrameSeconds = MaxFrameSeconds = 0;
            initialGc = gc;
            windowStart = now;
            WindowId++;
            // Retain lastFrame so two ticks in one frame cannot count it twice.
        }
    }
}
