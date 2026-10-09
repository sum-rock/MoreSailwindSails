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
        private readonly ProfileFamily previous;

        internal ProfileScope(
            ProfileCollector collector,
            int generation,
            int index,
            long started,
            ProfileFamily previous
        )
        {
            this.collector = collector;
            this.generation = generation;
            this.index = index;
            this.started = started;
            this.previous = previous;
        }

        public void Dispose() =>
            collector?.End(
                generation: generation,
                index: index,
                started: started,
                previous: previous
            );
    }
}
