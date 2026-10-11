using System;

namespace MoreSailwindSails.Utils.Profiling
{
    // Summarizes tick intervals in one GC group without retaining individual samples.
    internal struct ProfileIntervalStats
    {
        internal int Count;
        internal double Seconds;
        internal double Maximum;
        internal int Over50;
        internal int Over100;

        internal void Add(double seconds)
        {
            Count++;
            Seconds += seconds;
            Maximum = Math.Max(Maximum, seconds);
            if (seconds > 0.05)
                Over50++;
            if (seconds > 0.1)
                Over100++;
        }
    }
}
