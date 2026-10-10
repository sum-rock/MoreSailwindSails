namespace MoreSailwindSails.Visuals
{
    // A consumer acknowledges its own successful revision; failures and hidden draws stay pending.
    internal sealed class SailVisualCache
    {
        private long applied = -1;

        internal bool Needs(long revision) => applied != revision;

        internal void Commit(long revision) => applied = revision;

        internal void Invalidate() => applied = -1;
    }
}
