using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Controls.Configuration;

namespace MoreSailwindSails.Controls
{
    // Allocates stay halyards from dependency-related native stays and the aft mast fallback.
    internal sealed class HalyardWinchPlacementResolver
    {
        private readonly NativeWinchSeats native;
        private readonly WinchReservations ledger;

        internal HalyardWinchPlacementResolver(NativeWinchSeats native, WinchReservations ledger)
        {
            this.native = native;
            this.ledger = ledger;
        }

        internal bool ValidateCurrent(Mast requested, NativeWinchCandidate current)
        {
            if (current == null || !native.ActiveSupport(mast: requested))
                return false;
            if (current.Fallback)
            {
                var definition = Definition(requested: requested);
                var template = Template(requested: requested, definition: definition);
                return template
                    && current.Id == FallbackId(requested: requested)
                    && current.Templates.Length == 1
                    && current.Templates[0] == template
                    && current.Seats.Length == 1
                    && native.Available(seat: current.Seats[0])
                    && current
                        .Seats[0]
                        .MatchesPose(
                            position: definition.Position,
                            rotation: definition.Rotation(
                                templateRotation: native.Rotation(c: template)
                            )
                        );
            }
            return native.IsStaySource(source: current.SourceMast, requested: requested)
                && current.SourceMast.reefWinch != null
                && current.SourceIndex >= 0
                && current.SourceIndex < current.SourceMast.reefWinch.Length
                && current.SourceMast.reefWinch[current.SourceIndex] == current.Templates[0]
                && native.Current(candidate: current);
        }

        internal WinchResolution Resolve(object owner, Mast requested, WinchCandidate current)
        {
            var candidates = new List<NativeWinchCandidate>();
            // Donor stays may be unfitted; Candidate checks the actual mounting support.
            if (native.ActiveSupport(mast: requested))
                foreach (var source in native.StaySources(requested: requested))
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
                native: candidates.ToArray(),
                fallback: Fallback(requested: requested)
            );
            if (!native.ActiveSupport(mast: requested))
                result.MissingSupports++;
            return result;
        }

        private HalyardFallbackSeat Definition(Mast requested) =>
            requested
                ? native
                    .Profile?.HalyardFallbacks.FirstOrDefault(g => g.Mast == requested.orderIndex)
                    ?.Fallback
                : null;

        private static string FallbackId(Mast requested) =>
            "halyard-fallback/" + requested.orderIndex;

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

        private NativeWinchCandidate Fallback(Mast requested)
        {
            var definition = Definition(requested: requested);
            var template = Template(requested: requested, definition: definition);
            if (!template)
                return null;
            string id = FallbackId(requested: requested);
            var seat = new WinchSeat(
                identity: id,
                aliases: null,
                position: definition.Position,
                rotation: definition.Rotation(templateRotation: native.Rotation(c: template))
            );
            return new NativeWinchCandidate
            {
                Id = id,
                Context =
                    $"requestedMast={requested.orderIndex}, templateRig={requested.orderIndex}, index={definition.TemplateIndex}",
                Fallback = true,
                Templates = new[] { template },
                Seats = new[] { seat },
                Supported = native.ActiveSupport(mast: requested),
                Vacant = native.Available(seat: seat),
            };
        }
    }
}
