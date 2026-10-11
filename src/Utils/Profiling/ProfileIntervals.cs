namespace MoreSailwindSails.Utils.Profiling
{
    // Correlates consecutive tick clocks and GC counts, excluding reporting/transition gaps.
    internal sealed class ProfileIntervals
    {
        internal readonly ProfileHitch[] Longest = new ProfileHitch[8];
        internal ProfileIntervalStats WithGc;
        internal ProfileIntervalStats WithoutGc;
        internal int LongestCount { get; private set; }
        internal int Excluded { get; private set; }
        private bool baseline;
        private double previousTime;
        private int previousGc;
        private double captureStart;

        internal void Start(double now)
        {
            captureStart = now;
            Clear();
        }

        internal void Clear()
        {
            baseline = false;
            WithGc = WithoutGc = default;
            LongestCount = Excluded = 0;
        }

        internal void Sample(double now, int gc)
        {
            double seconds = now - previousTime;
            int collections = gc - previousGc;
            bool finite = !double.IsNaN(now) && !double.IsInfinity(now);
            if (!baseline || !finite || seconds < 0 || collections < 0)
                Excluded++;
            else
            {
                if (collections > 0)
                    WithGc.Add(seconds: seconds);
                else
                    WithoutGc.Add(seconds: seconds);
                if (seconds > 0.05)
                {
                    int index = LongestCount;
                    while (index > 0 && Longest[index - 1].Seconds < seconds)
                    {
                        if (index < Longest.Length)
                            Longest[index] = Longest[index - 1];
                        index--;
                    }
                    if (index < Longest.Length)
                    {
                        Longest[index] = new ProfileHitch(
                            endSeconds: now - captureStart,
                            seconds: seconds,
                            collections: collections
                        );
                        if (LongestCount < Longest.Length)
                            LongestCount++;
                    }
                }
            }
            baseline = finite;
            previousTime = now;
            previousGc = gc;
        }
    }
}
