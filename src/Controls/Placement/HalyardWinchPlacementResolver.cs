// Allocates halyards from consumer-selected native sources and its requested mast fallback.
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Controls.Configuration;
using MoreSailwindSails.Controls.Discovery;

namespace MoreSailwindSails.Controls.Placement
{
    // Uses the same source policy for selection and retention, with an independent fallback mast.
    internal sealed class HalyardWinchPlacementResolver
    {
        private readonly NativeWinchInventory native;
        private readonly WinchReservations ledger;

        internal HalyardWinchPlacementResolver(
            NativeWinchInventory native,
            WinchReservations ledger
        )
        {
            this.native = native;
            this.ledger = ledger;
        }

        internal bool ValidateCurrent(
            Mast forward,
            Mast requested,
            WinchPlacementCandidate current,
            NativeControlSourcePolicy sourcePolicy = NativeControlSourcePolicy.MatchingStays,
            Mast fittedMast = null
        )
        {
            if (current == null || !native.ActiveSupport(mast: requested))
                return false;
            if (current.Fallback)
            {
                var definition = Definition(requested: requested);
                var template = Template(requested: requested, definition: definition?.Fallback);
                return template
                    && current.Id == definition.FallbackId
                    && current.Templates.Length == 1
                    && current.Templates[0] == template
                    && current.Seats.Length == 1
                    && native.Available(seat: current.Seats[0])
                    && current
                        .Seats[0]
                        .MatchesPose(
                            position: definition.Fallback.Position,
                            rotation: definition.Fallback.Rotation(
                                templateRotation: native.Rotation(c: template)
                            )
                        );
            }
            return native.IsControlSource(
                    source: current.SourceMast,
                    forward: forward,
                    requested: requested,
                    policy: sourcePolicy,
                    fittedMast: fittedMast
                )
                && current.SourceMast.reefWinch != null
                && current.SourceIndex >= 0
                && current.SourceIndex < current.SourceMast.reefWinch.Length
                && current.SourceMast.reefWinch[current.SourceIndex] == current.Templates[0]
                && native.Current(candidate: current);
        }

        internal WinchResolution Resolve(
            object owner,
            Mast forward,
            Mast requested,
            WinchCandidate current,
            NativeControlSourcePolicy sourcePolicy = NativeControlSourcePolicy.MatchingStays,
            Mast fittedMast = null
        )
        {
            var candidates = new List<WinchPlacementCandidate>();
            // Donor stays may be unfitted; Candidate checks the actual mounting support.
            if (native.ActiveSupport(mast: requested))
                foreach (
                    var source in native.ControlSources(
                        forward: forward,
                        requested: requested,
                        policy: sourcePolicy,
                        fittedMast: fittedMast
                    )
                )
                {
                    if (source.reefWinch == null)
                        continue;
                    for (int i = 0; i < source.reefWinch.Length; i++)
                    {
                        var control = source.reefWinch[i];
                        if (!NativeWinchInventory.Usable(c: control))
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
                native: candidates.ToArray(),
                fallback: Fallback(requested: requested)
            );
            if (!native.ActiveSupport(mast: requested))
                result.MissingSupports++;
            return result;
        }

        private HalyardFallbackDefinition Definition(Mast requested) =>
            requested
                ? native.Profile?.HalyardFallbacks.FirstOrDefault(g =>
                    g.Mast == requested.orderIndex
                )
                : null;

        private GPButtonRopeWinch Template(Mast requested, HalyardFallbackSeat definition)
        {
            if (
                definition == null
                || !definition.Valid
                || !requested
                || requested.reefWinch == null
                || definition.TemplateIndex >= requested.reefWinch.Length
            )
                return null;
            var template = requested.reefWinch[definition.TemplateIndex];
            // Occupancy of the original seat does not prevent cloning its template.
            return native.TemplateUsable(control: template) ? template : null;
        }

        private WinchPlacementCandidate Fallback(Mast requested)
        {
            var definition = Definition(requested: requested);
            var template = Template(requested: requested, definition: definition?.Fallback);
            if (!template)
                return null;
            string id = definition.FallbackId;
            var seat = new WinchSeat(
                identity: id,
                aliases: null,
                position: definition.Fallback.Position,
                rotation: definition.Fallback.Rotation(
                    templateRotation: native.Rotation(c: template)
                )
            );
            return new WinchPlacementCandidate
            {
                Id = id,
                Context =
                    $"requestedMast={requested.orderIndex}, templateRig={requested.orderIndex}, index={definition.Fallback.TemplateIndex}",
                Fallback = true,
                Templates = new[] { template },
                Seats = new[] { seat },
                Supported = native.ActiveSupport(mast: requested),
                Vacant = native.Available(seat: seat),
            };
        }
    }
}
