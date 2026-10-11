using System;

namespace MoreSailwindSails.Utils.Profiling
{
    // A stack-local timing/context token; disposal closes early-return and exception paths.
    internal readonly struct ProfileScope : IDisposable
    {
        private readonly ProfileCollector collector;
        private readonly int generation;
        private readonly int index;
        private readonly long started;
        private readonly long allocationStart;
        private readonly ProfileFamily previous;

        internal ProfileScope(
            ProfileCollector collector,
            int generation,
            int index,
            long started,
            long allocationStart,
            ProfileFamily previous
        )
        {
            this.collector = collector;
            this.generation = generation;
            this.index = index;
            this.started = started;
            this.allocationStart = allocationStart;
            this.previous = previous;
        }

        public void Dispose() =>
            collector?.End(
                generation: generation,
                index: index,
                started: started,
                allocationStart: allocationStart,
                previous: previous
            );
    }
}
