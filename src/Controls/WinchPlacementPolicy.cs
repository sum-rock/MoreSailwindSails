using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreSailwindSails.Controls
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

    internal sealed class WinchResolution
    {
        internal WinchCandidate Candidate;
        internal int NativeUnavailable,
            Reserved,
            MissingSupports,
            FallbackBlocked;
        internal string MissingFallbackSide;
    }

    internal static class WinchPlacementPolicy
    {
        // Deduplicate whole placements only after considering their live support.
        // Seat aliases remain intact, so an occupied equivalent still blocks use.
        internal static void AddSupportedRepresentative<T>(List<T> candidates, T candidate)
            where T : WinchCandidate
        {
            if (candidate == null)
                return;
            var coincident = candidates.FirstOrDefault(c =>
                c.Seats.Length == candidate.Seats.Length
                && c.Seats.Select(
                        (seat, i) =>
                            (seat.Position - candidate.Seats[i].Position).sqrMagnitude <= 0.000001f
                    )
                    .All(same => same)
            );
            if (coincident != null)
            {
                if (coincident.Supported || !candidate.Supported)
                    return;
                candidates.Remove(coincident);
            }
            candidates.Add(candidate);
        }

        internal static WinchResolution Resolve(
            WinchReservations ledger,
            object owner,
            WinchCandidate current,
            WinchCandidate[] native,
            WinchCandidate fallback = null,
            string missingFallbackSide = null
        )
        {
            var result = new WinchResolution();
            var candidates =
                fallback == null ? native : native.Concat(new[] { fallback }).ToArray();
            var stable = candidates.FirstOrDefault(c => c.SamePlacement(current));
            if (stable != null && stable.Supported && stable.Vacant)
            {
                if (ledger.TryAcquireSeats(owner: owner, seats: stable.Seats))
                {
                    result.Candidate = stable;
                    return result;
                }
            }
            ledger.Release(owner);
            foreach (var candidate in candidates)
            {
                if (!candidate.Supported)
                    result.MissingSupports++;
                else if (!candidate.Vacant)
                {
                    if (candidate.Fallback)
                        result.FallbackBlocked++;
                    else
                        result.NativeUnavailable++;
                }
                else
                {
                    if (ledger.TryAcquireSeats(owner: owner, seats: candidate.Seats))
                    {
                        result.Candidate = candidate;
                        return result;
                    }
                    if (candidate.Fallback)
                        result.FallbackBlocked++;
                    else
                        result.Reserved++;
                }
            }
            result.MissingFallbackSide = missingFallbackSide;
            return result;
        }
    }
}
