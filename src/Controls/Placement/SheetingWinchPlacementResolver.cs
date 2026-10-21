// Allocates complete sheet pairs from matching physical mast pairs, then authored positions moving aft.
using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Controls.Configuration;
using MoreSailwindSails.Controls.Discovery;
using UnityEngine;

namespace MoreSailwindSails.Controls.Placement
{
    // Resolves and validates native sheet pairs and fallbacks aft of the forward physical mast.
    internal sealed class SheetingWinchPlacementResolver
    {
        private readonly NativeWinchInventory native;
        private readonly WinchReservations ledger;

        internal SheetingWinchPlacementResolver(
            NativeWinchInventory native,
            WinchReservations ledger
        )
        {
            this.native = native;
            this.ledger = ledger;
        }

        private IEnumerable<SheetFallbackDefinition> Fallbacks(Mast forward) =>
            SheetFallbackOrder.FromMast(
                fallbacks: native.Profile?.SheetFallbacks ?? Array.Empty<SheetFallbackDefinition>(),
                mastZ: native.Boat.transform.InverseTransformPoint(forward.transform.position).z
            );

        internal bool ValidateCurrent(Mast forward, Mast requested, WinchPlacementCandidate current)
        {
            if (
                current == null
                || !native.ActiveSupport(mast: forward)
                || !native.ActiveSupport(mast: requested)
            )
                return false;
            if (current.Fallback)
            {
                var category = SheetFallbackOrder.FindCurrent(
                    fallbacks: native.Profile?.SheetFallbacks
                        ?? Array.Empty<SheetFallbackDefinition>(),
                    id: current.Id,
                    mastZ: native.Boat.transform.InverseTransformPoint(forward.transform.position).z
                );
                return category != null
                    && current.Templates?.Length == 2
                    && current.Seats?.Length == 2
                    && ValidFallbackSide(
                        pair: category.Fallback,
                        definition: category.Fallback.Port,
                        current: current,
                        index: 0
                    )
                    && ValidFallbackSide(
                        pair: category.Fallback,
                        definition: category.Fallback.Starboard,
                        current: current,
                        index: 1
                    );
            }
            var mast = current.SourceMast;
            int index = current.SourceIndex;
            return mast
                && native.IsStaySource(source: mast, forward: forward, requested: requested)
                && mast.leftAngleWinch != null
                && mast.rightAngleWinch != null
                && index >= 0
                && index < mast.leftAngleWinch.Length
                && index < mast.rightAngleWinch.Length
                && mast.leftAngleWinch[index] == current.Templates[0]
                && mast.rightAngleWinch[index] == current.Templates[1]
                && native.Current(candidate: current);
        }

        private bool ValidFallbackSide(
            SheetFallbackPair pair,
            SheetFallbackSeat definition,
            WinchPlacementCandidate current,
            int index
        )
        {
            var template = Template(pair: pair);
            return template
                && current.Templates[index] == template
                && definition.Supports.All(id => native.ActiveSupport(mast: native.Mast(id: id)))
                && native.Available(seat: current.Seats[index])
                && current
                    .Seats[index]
                    .MatchesPose(
                        position: definition.Position(offset: pair.Offset),
                        rotation: definition.Rotation(
                            sourceNormal: pair.SourceNormal,
                            templateRotation: native.Rotation(c: template)
                        )
                    );
        }

        internal WinchResolution Resolve(
            object owner,
            Mast forward,
            Mast requested,
            WinchCandidate current
        )
        {
            var pairs = new List<WinchPlacementCandidate>();
            bool supportsActive =
                native.ActiveSupport(mast: forward) && native.ActiveSupport(mast: requested);
            if (supportsActive)
            {
                foreach (var mast in native.StaySources(forward: forward, requested: requested))
                {
                    var left = mast.leftAngleWinch ?? Array.Empty<GPButtonRopeWinch>();
                    var right = mast.rightAngleWinch ?? Array.Empty<GPButtonRopeWinch>();
                    foreach (
                        var indices in NativeSheetPairing.Indices(
                            port: left,
                            starboard: right,
                            usable: native.TemplateUsable,
                            invalid: index =>
                                native.Diagnose(
                                    key: $"pair/{mast.GetInstanceID()}/{index}",
                                    message: $"Unmatched native sheet pair: boat={native.Boat.name}, source={mast.orderIndex}, index={index}."
                                )
                        )
                    )
                    {
                        var candidate = native.Candidate(
                            id: $"native/{left[indices[0]].GetInstanceID()}/{right[indices[1]].GetInstanceID()}",
                            context: $"requestedMast={requested.orderIndex}, sourceRig={mast.orderIndex}, index={indices[0]}",
                            templates: new[] { left[indices[0]], right[indices[1]] }
                        );
                        if (candidate == null)
                            continue;
                        candidate.SourceMast = mast;
                        candidate.SourceIndex = indices[0];
                        WinchPlacementPolicy.AddSupportedRepresentative(
                            candidates: pairs,
                            candidate: candidate
                        );
                    }
                }
                // Append authored pairs moving aft from the forward physical mast.
                // The policy skips unavailable pairs and acquires both sides together.
                foreach (var category in Fallbacks(forward: forward))
                {
                    var fallback = Fallback(category: category);
                    if (fallback != null)
                        pairs.Add(fallback);
                }
            }
            var result = WinchPlacementPolicy.Resolve(
                ledger: ledger,
                owner: owner,
                current: current,
                native: pairs.ToArray()
            );
            if (!supportsActive)
                result.MissingSupports++;
            return result;
        }

        private GPButtonRopeWinch Template(SheetFallbackPair pair)
        {
            if (pair == null)
                return null;
            var mast = native.Mast(id: pair.TemplateMast);
            var sources = NativeWinchInventory.Sources(mast: mast, role: pair.TemplateRole);
            if (sources == null || pair.TemplateIndex >= sources.Length)
                return null;
            var template = sources[pair.TemplateIndex];
            // A donor can be occupied or on an unfitted mast; only the destination is reserved.
            return native.TemplateUsable(control: template) ? template : null;
        }

        private WinchPlacementCandidate Fallback(SheetFallbackDefinition category)
        {
            var pair = category.Fallback;
            var definitions = new[] { pair.Port, pair.Starboard };
            var template = Template(pair: pair);
            if (!template)
                return null;
            var rotation = native.Rotation(c: template);
            var seats = definitions
                .Select(
                    (definition, index) =>
                        new WinchSeat(
                            identity: category.FallbackName + "/" + index,
                            aliases: null,
                            position: definition.Position(offset: pair.Offset),
                            rotation: definition.Rotation(
                                sourceNormal: pair.SourceNormal,
                                templateRotation: rotation
                            )
                        )
                )
                .ToArray();
            return new WinchPlacementCandidate
            {
                Id = category.FallbackName,
                Context =
                    $"fallback={category.FallbackName}, templateRig={pair.TemplateMast}, role={pair.TemplateRole}, index={pair.TemplateIndex}",
                Fallback = true,
                Templates = new[] { template, template },
                Seats = seats,
                Supported = definitions.All(definition =>
                    definition.Supports.All(id => native.ActiveSupport(mast: native.Mast(id: id)))
                ),
                Vacant = seats.All(native.Available),
            };
        }
    }
}
