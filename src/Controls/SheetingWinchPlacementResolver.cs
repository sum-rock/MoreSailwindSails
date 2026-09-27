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
                var mast = native.Mast(source.Mast);
                if (!mast)
                    continue;
                var left = mast.leftAngleWinch ?? Array.Empty<GPButtonRopeWinch>();
                var right = mast.rightAngleWinch ?? Array.Empty<GPButtonRopeWinch>();
                var indices =
                    source.Pairs
                    ?? Enumerable
                        .Range(0, Math.Max(left.Length, right.Length))
                        .Select(i => new[] { i, i })
                        .ToArray();
                foreach (var index in indices)
                {
                    if (
                        index.Length != 2
                        || index[0] < 0
                        || index[1] < 0
                        || index[0] >= left.Length
                        || index[1] >= right.Length
                        || !NativeWinchSeats.Usable(left[index[0]])
                        || !NativeWinchSeats.Usable(right[index[1]])
                    )
                    {
                        native.Diagnose(
                            $"pair/{source.Mast}/{string.Join(",", index)}",
                            $"Unmatched native sheet pair: boat={native.Boat.name}, category={category.Name}, source={source.Mast}, indices={string.Join(",", index)}."
                        );
                        continue;
                    }
                    var p = left[index[0]];
                    var s = right[index[1]];
                    var candidate = native.Candidate(
                        $"native/{p.GetInstanceID()}/{s.GetInstanceID()}",
                        $"category={category.Name}, sourceRig={source.Mast}, indices={index[0]}/{index[1]}",
                        p,
                        s
                    );
                    if (candidate == null)
                        continue;
                    candidate.Supported &= native.ActiveSupport(forward);
                    // Aliases are already collected across the complete boat inventory.
                    if (
                        pairs.Any(c =>
                            c.Seats.Select(
                                    (seat, i) =>
                                        (seat.Position - candidate.Seats[i].Position).sqrMagnitude
                                        <= 0.000001f
                                )
                                .All(same => same)
                        )
                    )
                        continue;
                    pairs.Add(candidate);
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
                        * native.Rotation(templates[i]),
                    native.Radius(templates[i])
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
                Vacant = seats.All(s => native.Clear(s, false)),
            };
        }
    }
}
