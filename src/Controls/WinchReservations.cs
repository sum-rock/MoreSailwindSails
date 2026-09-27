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

        internal void Release(object owner) =>
            entries.RemoveAll(e => ReferenceEquals(e.Owner, owner));

        internal static bool Overlap(Vector3 a, float ar, Vector3 b, float br) =>
            (a - b).sqrMagnitude < (ar + br) * (ar + br);
    }
}
