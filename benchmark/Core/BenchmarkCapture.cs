using System;

namespace SailwindBenchmark
{
    // Retains whole-frame intervals without allocations or Unity dependencies during capture.
    internal sealed class BenchmarkCapture
    {
        internal readonly FrameSample[] Samples;
        internal CaptureState State { get; private set; }
        internal string Reason { get; private set; } = "";
        internal int Count { get; private set; }
        internal double Elapsed { get; private set; }
        internal bool Active => State == CaptureState.Warming || State == CaptureState.Measuring;
        private double warmupEnd;
        private double duration;
        private double previous;
        private int lastFrame;
        private int gc0,
            gc1,
            gc2;

        internal BenchmarkCapture(int capacity = 262144)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            Samples = new FrameSample[capacity];
        }

        internal void Start(double now, double warmupSeconds, double durationSeconds)
        {
            if (
                !Finite(now)
                || !Finite(warmupSeconds)
                || warmupSeconds < 0
                || !Finite(durationSeconds)
                || durationSeconds <= 0
            )
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            Count = 0;
            Elapsed = 0;
            Reason = "";
            lastFrame = -1;
            warmupEnd = now + warmupSeconds;
            duration = durationSeconds;
            previous = now;
            State = CaptureState.Warming;
        }

        internal void Tick(
            double now,
            int frame,
            int collections0,
            int collections1,
            int collections2
        )
        {
            if (!Active || frame == lastFrame)
                return;
            lastFrame = frame;
            if (!Finite(now) || now < previous)
            {
                Cancel(reason: "invalid-clock");
                return;
            }
            if (State == CaptureState.Warming)
            {
                previous = now;
                gc0 = collections0;
                gc1 = collections1;
                gc2 = collections2;
                if (now >= warmupEnd)
                    State = CaptureState.Measuring;
                return; // Seed the boundary; never include warm-up or start logging.
            }
            if (now <= previous || collections0 < gc0 || collections1 < gc1 || collections2 < gc2)
            {
                Cancel(reason: "invalid-counter");
                return;
            }
            if (Count == Samples.Length)
            {
                Cancel(reason: "buffer-exhausted");
                return;
            }
            double seconds = now - previous;
            Samples[Count++] = new FrameSample
            {
                seconds = seconds,
                gc0 = collections0 - gc0,
                gc1 = collections1 - gc1,
                gc2 = collections2 - gc2,
            };
            Elapsed += seconds;
            previous = now;
            gc0 = collections0;
            gc1 = collections1;
            gc2 = collections2;
            if (Elapsed >= duration)
                State = CaptureState.Complete;
        }

        internal void Cancel(string reason)
        {
            if (!Active)
                return;
            Reason = reason;
            State = CaptureState.Cancelled;
        }

        private static bool Finite(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value);
    }

    internal enum CaptureState
    {
        Idle,
        Warming,
        Measuring,
        Complete,
        Cancelled,
    }

    internal struct FrameSample
    {
        public double seconds;
        public int gc0,
            gc1,
            gc2;
    }
}
