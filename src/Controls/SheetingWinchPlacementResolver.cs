using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatRigs;
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

        internal bool ValidateCurrent(Mast forward, NativeWinchCandidate current)
        {
            if (current == null || !native.ActiveSupport(forward))
                return false;
            var category = native.Profile?.SheetCategory(forward.orderIndex);
            if (category == null)
                return false;
            if (current.Fallback)
            {
                if (
                    category.Fallback == null
                    || category.Fallback.InvalidSide != null
                    || current.Id != category.FallbackName
                )
                    return false;
                return ValidFallbackSide(
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
                && category.Sources.Contains(mast.orderIndex)
                && native.Mast(mast.orderIndex) == mast
                && mast.leftAngleWinch != null
                && mast.rightAngleWinch != null
                && index < mast.leftAngleWinch.Length
                && index < mast.rightAngleWinch.Length
                && mast.leftAngleWinch[index] == current.Templates[0]
                && mast.rightAngleWinch[index] == current.Templates[1]
                && native.Current(current);
        }

        private bool ValidFallbackSide(
            SheetFallbackSeat definition,
            NativeWinchCandidate current,
            int index
        )
        {
            var controls = NativeWinchSeats.Sources(
                mast: native.Mast(definition.TemplateMast),
                role: definition.TemplateRole
            );
            var template = current.Templates[index];
            return controls != null
                && definition.TemplateIndex < controls.Length
                && controls[definition.TemplateIndex] == template
                && native.TemplateUsable(template)
                && definition.Supports.All(id => native.ActiveSupport(native.Mast(id)))
                && current
                    .Seats[index]
                    .MatchesPose(
                        position: definition.Contact
                            + definition.Normal.normalized * definition.Offset,
                        rotation: Quaternion.FromToRotation(
                            definition.SourceNormal,
                            definition.Normal
                        ) * native.Rotation(template)
                    );
        }

        internal WinchResolution Resolve(
            object owner,
            GameObject ownerObject,
            Mast forward,
            WinchCandidate current
        )
        {
            var category = forward ? native.Profile?.SheetCategory(forward.orderIndex) : null;
            if (category == null)
            {
                ledger.Release(owner);
                native.Diagnose(
                    "category/" + (forward ? forward.orderIndex : -1),
                    $"Missing sheet category: boat={native.Boat.name}, owner={ownerObject.name}, mast={(forward ? forward.orderIndex : -1)}."
                );
                return new WinchResolution { MissingSupports = 1 };
            }
            var pairs = new List<NativeWinchCandidate>();
            foreach (var source in category.Sources)
            {
                var mast = native.Mast(source);
                if (!mast)
                    continue;
                var left = mast.leftAngleWinch ?? Array.Empty<GPButtonRopeWinch>();
                var right = mast.rightAngleWinch ?? Array.Empty<GPButtonRopeWinch>();
                var indices = NativeSheetPairing.Indices(
                    left,
                    right,
                    NativeWinchSeats.Usable,
                    index =>
                        native.Diagnose(
                            $"pair/{source}/{index}",
                            $"Unmatched native sheet pair: boat={native.Boat.name}, category={category.Name}, source={source}, indices={index}."
                        )
                );
                foreach (var index in indices)
                {
                    var p = left[index[0]];
                    var s = right[index[1]];
                    var candidate = native.Candidate(
                        $"native/{p.GetInstanceID()}/{s.GetInstanceID()}",
                        $"category={category.Name}, sourceRig={source}, indices={index[0]}/{index[1]}",
                        p,
                        s
                    );
                    if (candidate == null)
                        continue;
                    candidate.SourceMast = mast;
                    candidate.SourceIndex = index[0];
                    candidate.Supported &= native.ActiveSupport(forward);
                    WinchPlacementPolicy.AddSupportedRepresentative(
                        candidates: pairs,
                        candidate: candidate
                    );
                }
            }
            var fallback = Fallback(category, forward, out var invalid);
            var result = WinchPlacementPolicy.Resolve(
                ledger,
                owner,
                current,
                pairs.ToArray(),
                fallback,
                invalid
            );
            if (result.MissingFallbackSide != null)
                native.Diagnose(
                    "fallback/" + category.Name + "/" + result.MissingFallbackSide,
                    $"Missing or invalid sheet fallback: boat={native.Boat.name}#{native.Boat.GetInstanceID()}, category={category.Name}, owner={ownerObject.name}#{ownerObject.GetInstanceID()}, fallback={category.FallbackName}, side={result.MissingFallbackSide}.",
                    true
                );
            return result;
        }

        private NativeWinchCandidate Fallback(
            SheetWinchCategory category,
            Mast forward,
            out string invalid
        )
        {
            invalid =
                category.Fallback == null
                    ? "port/starboard (not authored)"
                    : category.Fallback.InvalidSide;
            if (invalid != null)
                return null;
            var definitions = new[] { category.Fallback.Port, category.Fallback.Starboard };
            var templates = new GPButtonRopeWinch[2];
            var seats = new WinchSeat[2];
            bool supported = native.ActiveSupport(forward);
            for (int i = 0; i < 2; i++)
            {
                var d = definitions[i];
                var controls = NativeWinchSeats.Sources(
                    native.Mast(d.TemplateMast),
                    d.TemplateRole
                );
                if (
                    controls == null
                    || d.TemplateIndex >= controls.Length
                    || !NativeWinchSeats.Usable(controls[d.TemplateIndex])
                )
                {
                    invalid = (i == 0 ? "port" : "starboard") + " template";
                    return null;
                }
                templates[i] = controls[d.TemplateIndex];
                seats[i] = new WinchSeat(
                    category.FallbackName + "/" + i,
                    null,
                    d.Contact + d.Normal.normalized * d.Offset,
                    Quaternion.FromToRotation(d.SourceNormal, d.Normal)
                        * native.Rotation(templates[i])
                );
                supported &= d.Supports.All(id => native.ActiveSupport(native.Mast(id)));
            }
            return new NativeWinchCandidate
            {
                Id = category.FallbackName,
                Context = "category=" + category.Name,
                Fallback = true,
                Templates = templates,
                Seats = seats,
                Supported = supported,
                // Authored fallbacks are chosen to avoid native fittings; the ledger prevents reuse.
                Vacant = true,
            };
        }
    }
}
