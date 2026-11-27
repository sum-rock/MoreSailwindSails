using System;

namespace SailwindBenchmark
{
    // Retains numeric state transitions in fixed storage; export supplies human-readable names.
    internal sealed class BenchmarkEvents
    {
        internal readonly BenchmarkEvent[] Entries;
        internal int Count { get; private set; }
        internal int Dropped { get; private set; }
        private readonly double[] previous = new double[(int)BenchmarkEventKind.Count];
        private readonly bool[] known = new bool[(int)BenchmarkEventKind.Count];

        internal BenchmarkEvents(int capacity = 4096)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(paramName: nameof(capacity));
            Entries = new BenchmarkEvent[capacity];
        }

        internal void Clear()
        {
            Count = Dropped = 0;
            Array.Clear(array: known, index: 0, length: known.Length);
        }

        internal void Observe(
            double seconds,
            bool measuring,
            BenchmarkEventKind kind,
            double value,
            bool occurrence = false
        )
        {
            int index = (int)kind;
            if (!occurrence && known[index] && previous[index] == value)
                return;
            known[index] = true;
            previous[index] = value;
            if (Count == Entries.Length)
            {
                Dropped++;
                return;
            }
            Entries[Count++] = new BenchmarkEvent
            {
                Seconds = seconds,
                Measuring = measuring,
                Kind = kind,
                Value = value,
            };
        }
    }

    internal enum BenchmarkEventKind
    {
        Focused,
        ApplicationPaused,
        Playing,
        CurrentlyLoading,
        LoadingBoatLocalItems,
        LoadingScenes,
        Recovering,
        Sleeping,
        CursorMenu,
        Shipyard,
        BoatInstance,
        ActiveScene,
        SceneLoaded,
        SceneUnloaded,
        Width,
        Height,
        Fullscreen,
        Quality,
        VSyncCount,
        TargetFrameRate,
        TimeScale,
        FixedDeltaTime,
        ConfigurationChanged,
        Count,
    }

    internal struct BenchmarkEvent
    {
        internal double Seconds,
            Value;
        internal bool Measuring;
        internal BenchmarkEventKind Kind;
    }
}
