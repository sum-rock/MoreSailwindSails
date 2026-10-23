// Describes a complete placement and its live support and occupancy state.
using System.Linq;

namespace MoreSailwindSails.Controls.Placement
{
    internal class WinchCandidate
    {
        internal string Id;
        internal WinchSeat[] Seats;
        internal bool Supported,
            Vacant,
            Fallback;

        internal bool SamePlacement(WinchCandidate other) =>
            other != null
            && Id == other.Id
            && Seats.Length == other.Seats.Length
            && Seats.Select((s, i) => s.SamePose(other.Seats[i])).All(s => s);
    }
}
