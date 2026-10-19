// Tracks one sheet pair or halyard placement, retry timing and diagnostic state.

using MoreSailwindSails.Controls.Placement;

namespace MoreSailwindSails.Controls.Runtime
{
    internal sealed class WinchPlacementState
    {
        internal WinchPlacementCandidate Current;
        internal float RetryAfter;
        internal bool Released = true;
        internal bool Logged,
            Warned,
            Failed;
    }
}
