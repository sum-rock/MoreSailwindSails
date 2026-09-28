namespace MoreSailwindSails.Controls
{
    // Bounds hierarchy discovery while allowing immediate lifecycle invalidation.
    internal sealed class WinchDiscoverySchedule
    {
        private bool dirty = true;
        private float nextDiscovery;

        internal void Invalidate() => dirty = true;

        internal bool Due(float now) => dirty || now >= nextDiscovery;

        internal void Discovered(float now)
        {
            dirty = false;
            nextDiscovery = now + 1f;
        }
    }
}
