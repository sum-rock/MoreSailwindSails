using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.Controls
{
    // Pure allocation policy. Positions and radii are in the boat's local frame.
    internal sealed class WinchReservations
    {
        private sealed class Reservation
        {
            internal object Owner;
            internal WinchSeat Seat;
        }

        private readonly List<Reservation> entries = new List<Reservation>();
        internal int Count => entries.Count;

        internal sealed class Claim
        {
            internal readonly object Owner;
            internal readonly WinchSeat[] Seats;

            internal Claim(object owner, WinchSeat[] seats)
            {
                Owner = owner;
                Seats = seats;
            }
        }

        // Check the whole transaction before touching the ledger. A failed
        // acquisition cannot leave half a pair or consume another owner's seat.
        internal Claim AcquireSeats(object owner, WinchSeat[] seats)
        {
            if (
                owner == null
                || seats == null
                || seats.Length == 0
                || seats.Any(s => s == null || !s.Valid)
            )
                return null;
            for (int i = 0; i < seats.Length; i++)
            {
                for (int j = 0; j < i; j++)
                    if (seats[i].Conflicts(seats[j]))
                        return null;
                if (
                    entries.Any(e => !ReferenceEquals(e.Owner, owner) && seats[i].Conflicts(e.Seat))
                )
                    return null;
            }
            Release(owner);
            foreach (var seat in seats)
                entries.Add(new Reservation { Owner = owner, Seat = seat });
            return new Claim(owner, seats.ToArray());
        }

        internal void Release(object owner) =>
            entries.RemoveAll(e => ReferenceEquals(e.Owner, owner));

        internal static bool Overlap(Vector3 a, float ar, Vector3 b, float br) =>
            (a - b).sqrMagnitude < (ar + br) * (ar + br);
    }
}
