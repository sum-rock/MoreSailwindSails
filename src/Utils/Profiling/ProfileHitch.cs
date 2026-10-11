namespace MoreSailwindSails.Utils.Profiling
{
    // Describes an observed sampling interval, not the duration of a GC pause.
    internal readonly struct ProfileHitch
    {
        internal readonly double EndSeconds;
        internal readonly double Seconds;
        internal readonly int Collections;

        internal ProfileHitch(double endSeconds, double seconds, int collections)
        {
            EndSeconds = endSeconds;
            Seconds = seconds;
            Collections = collections;
        }
    }
}
