using System;
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
        internal WinchReservations.Claim Claim;
        internal int NativeUnavailable,
            Reserved,
            MissingSupports,
            FallbackBlocked;
        internal string MissingFallbackSide;
    }

    internal static class WinchPlacementPolicy
    {
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
                result.Claim = ledger.AcquireSeats(owner, stable.Seats);
                if (result.Claim != null)
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
                    var claim = ledger.AcquireSeats(owner, candidate.Seats);
                    if (claim != null)
                    {
                        result.Candidate = candidate;
                        result.Claim = claim;
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
