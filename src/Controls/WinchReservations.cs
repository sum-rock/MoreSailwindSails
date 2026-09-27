using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.Controls
{
    // Pure allocation policy. Positions and radii are in the boat's local frame.
    internal sealed class WinchReservations
    {
        // Count each rejected candidate once, with native obstructions taking
        // precedence over reservations. Collection is optional for silent retries.
        internal sealed class Rejections
        {
            internal int Native,
                Reserved;
        }

        internal sealed class Reservation
        {
            internal object Donor,
                Owner;
            internal int Slot;
            internal Vector3 Position;
            internal float Radius;
            internal WinchSeat Seat;
        }

        private readonly List<Reservation> entries = new List<Reservation>();
        internal int Count => entries.Count;

        internal Reservation Acquire(
            object donor,
            object owner,
            Vector3[] candidates,
            float radius,
            Func<Vector3, float, bool> obstructed,
            Rejections rejections = null
        )
        {
            if (donor == null || owner == null)
                throw new ArgumentNullException();
            if (rejections != null)
                rejections.Native = rejections.Reserved = 0;
            var existing = entries.FirstOrDefault(e => ReferenceEquals(e.Owner, owner));
            if (existing != null && ReferenceEquals(existing.Donor, donor))
                return existing;
            Release(owner);
            for (int slot = 0; slot < candidates.Length; slot++)
            {
                var position = candidates[slot];
                if (obstructed(position, radius))
                {
                    if (rejections != null)
                        rejections.Native++;
                    continue;
                }
                if (
                    entries.Any(e =>
                        (ReferenceEquals(e.Donor, donor) && e.Slot == slot)
                        || Overlap(position, radius, e.Position, e.Radius)
                    )
                )
                {
                    if (rejections != null)
                        rejections.Reserved++;
                    continue;
                }
                var entry = new Reservation
                {
                    Donor = donor,
                    Owner = owner,
                    Slot = slot,
                    Position = position,
                    Radius = radius,
                };
                entries.Add(entry);
                return entry;
            }
            return null;
        }

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
                    entries.Any(e =>
                        !ReferenceEquals(e.Owner, owner)
                        && (
                            e.Seat != null
                                ? seats[i].Conflicts(e.Seat)
                                : Overlap(seats[i].Position, seats[i].Radius, e.Position, e.Radius)
                        )
                    )
                )
                    return null;
            }
            Release(owner);
            foreach (var seat in seats)
                entries.Add(
                    new Reservation
                    {
                        Owner = owner,
                        Donor = seat.Identity,
                        Position = seat.Position,
                        Radius = seat.Radius,
                        Seat = seat,
                    }
                );
            return new Claim(owner, seats.ToArray());
        }

        internal void Release(object owner) =>
            entries.RemoveAll(e => ReferenceEquals(e.Owner, owner));

        internal static bool Overlap(Vector3 a, float ar, Vector3 b, float br) =>
            (a - b).sqrMagnitude < (ar + br) * (ar + br);
    }
}
