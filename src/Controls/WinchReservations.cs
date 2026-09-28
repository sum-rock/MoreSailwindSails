using System.Collections.Generic;
using System.Linq;

namespace MoreSailwindSails.Controls
{
    // Boat-owned allocation by seat identity and aliases, without geometric clearance.
    internal sealed class WinchReservations
    {
        private sealed class Reservation
        {
            internal object Owner;
            internal WinchSeat Seat;
        }

        private readonly List<Reservation> entries = new List<Reservation>();
        internal int Count => entries.Count;

        // Check the whole transaction before touching the ledger. A failed
        // acquisition cannot leave half a pair or consume another owner's seat.
        internal bool TryAcquireSeats(object owner, WinchSeat[] seats)
        {
            if (
                owner == null
                || seats == null
                || seats.Length == 0
                || seats.Any(s => s == null || !s.Valid)
            )
                return false;
            for (int i = 0; i < seats.Length; i++)
            {
                for (int j = 0; j < i; j++)
                    if (seats[i].Conflicts(seats[j]))
                        return false;
                if (
                    entries.Any(e => !ReferenceEquals(e.Owner, owner) && seats[i].Conflicts(e.Seat))
                )
                    return false;
            }
            if (
                entries.Count(e => ReferenceEquals(e.Owner, owner)) == seats.Length
                && seats.All(s =>
                    entries.Any(e =>
                        ReferenceEquals(e.Owner, owner)
                        && e.Seat.SamePose(s)
                        && e.Seat.Aliases.SequenceEqual(s.Aliases)
                    )
                )
            )
                return true;
            Release(owner);
            foreach (var seat in seats)
                entries.Add(new Reservation { Owner = owner, Seat = seat });
            return true;
        }

        internal void Release(object owner) =>
            entries.RemoveAll(e => ReferenceEquals(e.Owner, owner));
    }
}
