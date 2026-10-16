// Allocates complete sheet pairs from aft-mast dependencies, then authored positions moving aft.
using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Controls.Configuration;
using UnityEngine;

namespace MoreSailwindSails.Controls
{
    internal sealed class SheetingWinchPlacementResolver
    {
        private readonly NativeWinchSeats native;
        private readonly WinchReservations ledger;

        internal SheetingWinchPlacementResolver(NativeWinchSeats native, WinchReservations ledger)
        {
            this.native = native;
            this.ledger = ledger;
        }

        private IEnumerable<SheetFallbackDefinition> Fallbacks(Mast requested) =>
            SheetFallbackOrder.FromMast(
                fallbacks: native.Profile?.SheetFallbacks ?? Array.Empty<SheetFallbackDefinition>(),
                mastZ: native.Boat.transform.InverseTransformPoint(requested.transform.position).z
            );

        internal bool ValidateCurrent(Mast requested, NativeWinchCandidate current)
        {
            if (current == null || !native.ActiveSupport(mast: requested))
                return false;
            if (current.Fallback)
            {
                var category = Fallbacks(requested: requested)
                    .FirstOrDefault(item => item.FallbackName == current.Id);
                return category != null
                    && ValidFallbackSide(
                        definition: category.Fallback.Port,
                        current: current,
                        index: 0
                    )
                    && ValidFallbackSide(
                        definition: category.Fallback.Starboard,
                        current: current,
                        index: 1
                    );
            }
            var mast = current.SourceMast;
            int index = current.SourceIndex;
            return mast
                && native.StaySources(requested: requested).Contains(mast)
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
            SheetFallbackSeat definition,
            NativeWinchCandidate current,
            int index
        )
        {
            var template = Template(definition: definition);
            return template
                && current.Templates[index] == template
                && definition.Supports.All(id => native.ActiveSupport(mast: native.Mast(id: id)))
                && native.Available(seat: current.Seats[index])
                && current
                    .Seats[index]
                    .MatchesPose(
                        position: definition.Contact
                            + definition.Normal.normalized * definition.Offset,
                        rotation: Quaternion.FromToRotation(
                            definition.SourceNormal,
                            definition.Normal
                        ) * native.Rotation(c: template)
                    );
        }

        internal WinchResolution Resolve(object owner, Mast requested, WinchCandidate current)
        {
            var pairs = new List<NativeWinchCandidate>();
            if (native.ActiveSupport(mast: requested))
            {
                foreach (var mast in native.StaySources(requested: requested))
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
                // Append authored pairs in encounter order. The shared policy skips occupied,
                // unsupported or claimed pairs and acquires both sides together.
                foreach (var category in Fallbacks(requested: requested))
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
            if (!native.ActiveSupport(mast: requested))
                result.MissingSupports++;
            return result;
        }

        private GPButtonRopeWinch Template(SheetFallbackSeat definition)
        {
            var controls = NativeWinchSeats.Sources(
                mast: native.Mast(id: definition.TemplateMast),
                role: definition.TemplateRole
            );
            return
                controls != null
                && definition.TemplateIndex < controls.Length
                && native.TemplateUsable(control: controls[definition.TemplateIndex])
                ? controls[definition.TemplateIndex]
                : null;
        }

        private NativeWinchCandidate Fallback(SheetFallbackDefinition category)
        {
            var definitions = new[] { category.Fallback.Port, category.Fallback.Starboard };
            var templates = definitions.Select(Template).ToArray();
            if (templates.Any(template => !template))
                return null;
            var seats = definitions
                .Select(
                    (definition, index) =>
                        new WinchSeat(
                            identity: category.FallbackName + "/" + index,
                            aliases: null,
                            position: definition.Contact
                                + definition.Normal.normalized * definition.Offset,
                            rotation: Quaternion.FromToRotation(
                                definition.SourceNormal,
                                definition.Normal
                            ) * native.Rotation(c: templates[index])
                        )
                )
                .ToArray();
            return new NativeWinchCandidate
            {
                Id = category.FallbackName,
                Context = "fallback=" + category.FallbackName,
                Fallback = true,
                Templates = templates,
                Seats = seats,
                Supported = definitions.All(definition =>
                    definition.Supports.All(id => native.ActiveSupport(mast: native.Mast(id: id)))
                ),
                Vacant = seats.All(native.Available),
            };
        }
    }
}
