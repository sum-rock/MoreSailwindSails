using System.Collections.Generic;
using System.Linq;

namespace MoreSailwindSails.Controls
{
    // Allocates halyards from the active requested mast's authored source group.
    internal sealed class HalyardWinchPlacementResolver
    {
        private readonly NativeWinchSeats native;
        private readonly WinchReservations ledger;

        internal HalyardWinchPlacementResolver(NativeWinchSeats native, WinchReservations ledger)
        {
            this.native = native;
            this.ledger = ledger;
        }

        internal bool ValidateCurrent(Mast requested, NativeWinchCandidate current) =>
            current != null
            && native.ActiveSupport(mast: requested)
            && native.HalyardSources(requested: requested).Contains(current.SourceMast)
            && current.SourceMast.reefWinch != null
            && current.SourceIndex >= 0
            && current.SourceIndex < current.SourceMast.reefWinch.Length
            && current.SourceMast.reefWinch[current.SourceIndex] == current.Templates[0]
            && native.Current(candidate: current);

        internal WinchResolution Resolve(object owner, Mast requested, WinchCandidate current)
        {
            var candidates = new List<NativeWinchCandidate>();
            // Source rigs may be unfitted; Candidate checks the actual mounting
            // support. Never broaden this to a category or nearby-mast search.
            if (native.ActiveSupport(mast: requested))
                foreach (var source in native.HalyardSources(requested: requested))
                {
                    if (source.reefWinch == null)
                        continue;
                    for (int i = 0; i < source.reefWinch.Length; i++)
                    {
                        var control = source.reefWinch[i];
                        if (!NativeWinchSeats.Usable(c: control))
                            continue;
                        var candidate = native.Candidate(
                            id: "halyard/" + control.GetInstanceID(),
                            context: $"requestedMast={requested.orderIndex}, sourceRig={source.orderIndex}, index={i}",
                            templates: control
                        );
                        if (candidate != null)
                        {
                            candidate.SourceMast = source;
                            candidate.SourceIndex = i;
                        }
                        WinchPlacementPolicy.AddSupportedRepresentative(
                            candidates: candidates,
                            candidate: candidate
                        );
                    }
                }
            var result = WinchPlacementPolicy.Resolve(
                ledger: ledger,
                owner: owner,
                current: current,
                native: candidates.ToArray()
            );
            if (!native.ActiveSupport(mast: requested))
                result.MissingSupports++;
            return result;
        }
    }
}
