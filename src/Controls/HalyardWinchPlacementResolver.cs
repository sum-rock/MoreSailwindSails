using System.Collections.Generic;
using System.Linq;

namespace MoreSailwindSails.Controls
{
    internal sealed class HalyardWinchPlacementResolver
    {
        private readonly NativeWinchSeats native;
        private readonly WinchReservations ledger;

        internal HalyardWinchPlacementResolver(NativeWinchSeats native, WinchReservations ledger)
        {
            this.native = native;
            this.ledger = ledger;
        }

        internal WinchResolution Resolve(object owner, Mast requested, WinchCandidate current)
        {
            var candidates = new List<NativeWinchCandidate>();
            // Never substitute an inactive variant or another mast in the category.
            if (native.ActiveSupport(requested) && requested.reefWinch != null)
                for (int i = 0; i < requested.reefWinch.Length; i++)
                {
                    var control = requested.reefWinch[i];
                    if (!NativeWinchSeats.Usable(control))
                        continue;
                    var candidate = native.Candidate(
                        "halyard/" + control.GetInstanceID(),
                        $"requestedMast={requested.orderIndex}, index={i}",
                        control
                    );
                    WinchPlacementPolicy.AddSupportedRepresentative(
                        candidates: candidates,
                        candidate: candidate
                    );
                }
            var result = WinchPlacementPolicy.Resolve(ledger, owner, current, candidates.ToArray());
            if (!native.ActiveSupport(requested))
                result.MissingSupports++;
            return result;
        }
    }
}
