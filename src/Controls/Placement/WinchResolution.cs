// Reports a selected placement and the reasons other candidates were unavailable.

namespace MoreSailwindSails.Controls.Placement
{
    internal sealed class WinchResolution
    {
        internal WinchCandidate Candidate;
        internal int NativeUnavailable,
            Reserved,
            MissingSupports,
            FallbackBlocked;
        internal string MissingFallbackSide;
    }
}
