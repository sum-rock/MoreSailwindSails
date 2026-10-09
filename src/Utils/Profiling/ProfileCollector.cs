using System;

namespace MoreSailwindSails.Utils.Profiling
{
    // Allocation-free inclusive CPU aggregation, independent of Unity and log formatting.
    internal sealed class ProfileCollector
    {
        private const int StageCount = (int)ProfileStage.Count;
        private const int TargetCount = (int)ProfileTarget.Count;
        internal const int SampleCount = (int)ProfileFamily.Count * TargetCount * StageCount;
        internal readonly long[] Ticks = new long[SampleCount];
        internal readonly long[] Maximum = new long[SampleCount];
        internal readonly int[] Calls = new int[SampleCount];
        internal readonly int[] Counters = new int[
            (int)ProfileFamily.Count * (int)ProfileCounter.Count
        ];
        private readonly bool[] measuring = new bool[SampleCount];
        private readonly Func<long> timestamp;
        private int generation;
        private ProfileFamily family;
        internal bool Active { get; set; }
        internal int Selection { get; set; }

        internal ProfileCollector(Func<long> timestamp) => this.timestamp = timestamp;

        internal static int Index(
            ProfileFamily family,
            ProfileTarget target,
            ProfileStage stage = ProfileStage.Total
        ) => ((int)family * TargetCount + (int)target) * StageCount + (int)stage;

        internal bool Selected(ProfileTarget target) =>
            Active && (Selection & (1 << (int)target)) != 0;

        internal ProfileScope Begin(
            ProfileTarget target,
            ProfileFamily owner = ProfileFamily.Unspecified,
            ProfileStage stage = ProfileStage.Total
        )
        {
            if (!Active)
                return default;
            var previous = family;
            if (owner != ProfileFamily.Unspecified)
                family = owner;
            int index = Index(family: family, target: target, stage: stage);
            // Nested routes/tubes in the same category are already included in their parent.
            bool measure = Selected(target: target) && !measuring[index];
            if (measure)
                measuring[index] = true;
            return new ProfileScope(
                collector: this,
                generation: generation,
                index: measure ? index : -1,
                started: measure ? timestamp() : 0,
                previous: previous
            );
        }

        internal void End(int generation, int index, long started, ProfileFamily previous)
        {
            if (generation != this.generation)
                return;
            if (index >= 0)
            {
                long elapsed = Math.Max(0, timestamp() - started);
                Ticks[index] += elapsed;
                Maximum[index] = Math.Max(Maximum[index], elapsed);
                Calls[index]++;
                measuring[index] = false;
            }
            family = previous;
        }

        internal void Count(ProfileTarget target, ProfileCounter counter)
        {
            if (Selected(target: target))
                Counters[(int)family * (int)ProfileCounter.Count + (int)counter]++;
        }

        internal void SurfaceQuery(bool hit)
        {
            if (Active && measuring[Index(family: family, target: ProfileTarget.SailMount)])
                Count(
                    target: ProfileTarget.SailMount,
                    counter: hit ? ProfileCounter.SurfaceHits : ProfileCounter.SurfaceMisses
                );
        }

        internal void Clear()
        {
            generation++;
            family = ProfileFamily.Unspecified;
            Array.Clear(Ticks, 0, Ticks.Length);
            Array.Clear(Maximum, 0, Maximum.Length);
            Array.Clear(Calls, 0, Calls.Length);
            Array.Clear(Counters, 0, Counters.Length);
            Array.Clear(measuring, 0, measuring.Length);
        }
    }
}
