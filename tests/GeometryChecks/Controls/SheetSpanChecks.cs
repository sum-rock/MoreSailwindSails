// Exercises fitted-width projection, size boundaries, stable retention and pair exhaustion.
using System;
using System.Linq;
using MoreSailwindSails.Controls;
using MoreSailwindSails.Controls.Configuration;
using MoreSailwindSails.Controls.Placement;
using MoreSailwindSails.Sails.FishermansStaysail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Controls;

internal static class SheetSpanChecks
{
    internal static void Run()
    {
        // A neutral 4 m width scaled 2x and pitched 60 degrees spans 4 m along the boat.
        Vector3 Rotate(Vector3 value, Vector3 axis, float angle) =>
            FishermansStaysailFrameGeometry.RotateAroundMast(
                point: value,
                pivot: Vector3.zero,
                axis: axis,
                degrees: angle
            );
        var width = Rotate(
            value: Vector3.Scale(new Vector3(0, 0, 4), new Vector3(3, 1, 2)),
            axis: Vector3.right,
            angle: 60
        );
        float span = SailControlGeometry.SheetSpan(neutralWidthInBoat: width).Value;
        Check(Math.Abs(span - 4f) < 0.00001f, "Use projected fitted width, not edge length.");
        var boatScale = new Vector3(2, 3, 4);
        var worldWidth = Rotate(
            value: Vector3.Scale(width, boatScale),
            axis: Vector3.up,
            angle: 53
        );
        var localWidth = Vector3.Scale(
            Rotate(value: worldWidth, axis: Vector3.up, angle: -53),
            new Vector3(0.5f, 1f / 3, 0.25f)
        );
        float rotated = SailControlGeometry.SheetSpan(neutralWidthInBoat: localWidth).Value;
        Check(
            Math.Abs(rotated - span) < 0.00001f,
            "Boat rotation and scale must not change the local span."
        );
        Check(
            SailControlGeometry.SheetSpan(neutralWidthInBoat: -width) == span
                && SailControlGeometry.SheetSpan(neutralWidthInBoat: Vector3.zero) == null
                && SailControlGeometry.SheetSpan(neutralWidthInBoat: new Vector3(float.NaN, 0, 1))
                    == null,
            "Mirrored scale must preserve extent; invalid geometry must defer fallbacks."
        );

        var near = Pair(name: "near", portZ: 1, starboardZ: 1);
        var edge = Pair(name: "edge", portZ: -2, starboardZ: -2);
        var crossing = Pair(name: "crossing", portZ: -2, starboardZ: -1.9f);
        var far = Pair(name: "far", portZ: -6, starboardZ: -6);
        var pairs = new[] { far, near, crossing, edge };
        float boundary = SheetFallbackOrder.Boundary(mastZ: 3, fittedSpan: 5);
        Check(boundary == -2, "Aft is negative boat Z.");
        var eligible = SheetFallbackOrder
            .FromBoundary(fallbacks: pairs, aftBoundaryZ: boundary)
            .ToArray();
        Check(
            eligible.SequenceEqual(new[] { edge, far }),
            "Both contacts must meet the inclusive boundary."
        );
        foreach (float? invalid in new float?[] { null, -1, float.NaN, float.PositiveInfinity })
            Check(
                !SheetFallbackOrder
                    .FromBoundary(
                        fallbacks: pairs,
                        aftBoundaryZ: SheetFallbackOrder.Boundary(mastZ: 3, fittedSpan: invalid)
                    )
                    .Any(),
                "Unavailable fitted dimensions must not select a forward fallback."
            );
        Check(
            SheetFallbackOrder.Boundary(mastZ: 3, fittedSpan: 0) == 3,
            "Other sails retain the mast-only boundary."
        );

        var ledger = new WinchReservations();
        var owner = new object();
        var occupied = Candidate(pair: edge, vacant: false);
        var free = Candidate(pair: far, vacant: true);
        var selected = WinchPlacementPolicy
            .Resolve(ledger: ledger, owner: owner, current: null, native: new[] { occupied, free })
            .Candidate;
        Check(selected == free, "An occupied eligible pair must allow the next pair aft.");
        bool Retain(float fittedSpan) =>
            WinchPlacementPolicy.TryRetain(
                ledger: ledger,
                owner: owner,
                current: selected,
                valid: SheetFallbackOrder.FindCurrent(
                    fallbacks: pairs,
                    id: selected.Id,
                    aftBoundaryZ: SheetFallbackOrder.Boundary(mastZ: 3, fittedSpan: fittedSpan)
                ) != null
            );
        Check(
            Retain(fittedSpan: 2),
            "Shrinking keeps a valid pair instead of moving to the newly eligible near pair."
        );
        Check(!Retain(fittedSpan: 10), "Growth must invalidate a pair ahead of the new boundary.");
        var exhausted = WinchPlacementPolicy.Resolve(
            ledger: ledger,
            owner: owner,
            current: selected,
            native: Array.Empty<WinchCandidate>()
        );
        Check(
            exhausted.Candidate == null && ledger.Count == 0,
            "Exhaustion must release the old pair."
        );
        var native = Candidate(pair: near, vacant: true);
        native.Fallback = false;
        Check(
            WinchPlacementPolicy
                .Resolve(
                    ledger: ledger,
                    owner: owner,
                    current: null,
                    native: new[] { native, free }
                )
                .Candidate == native,
            "Native placement must retain priority even ahead of the fitted boundary."
        );
        ledger.Release(owner: owner);
        Check(
            WinchPlacementPolicy
                .Resolve(ledger: ledger, owner: owner, current: null, native: new[] { native })
                .Candidate == native,
            "A missing eligible fallback must not prevent native placement."
        );
        Console.WriteLine(
            "PASS: fitted sheet span, boat-frame projection, inclusive paired boundaries, pending geometry, growth, shrink retention and exhaustion."
        );
    }

    private static SheetFallbackDefinition Pair(string name, float portZ, float starboardZ) =>
        new SheetFallbackDefinition(
            name: name,
            fallback: new SheetFallbackPair(
                templateMast: 2,
                templateRole: WinchRole.Left,
                templateIndex: 0,
                sourceNormal: Vector3.up,
                offset: 0,
                port: new SheetFallbackSeat(contact: new Vector3(-2, 0, portZ), normal: Vector3.up),
                starboard: new SheetFallbackSeat(
                    contact: new Vector3(2, 0, starboardZ),
                    normal: Vector3.up
                )
            )
        );

    private static WinchCandidate Candidate(SheetFallbackDefinition pair, bool vacant) =>
        new WinchCandidate
        {
            Id = pair.FallbackName,
            Fallback = true,
            Supported = true,
            Vacant = vacant,
            Seats = new[]
            {
                new WinchSeat(
                    identity: pair.Name + "/port",
                    aliases: null,
                    position: pair.Fallback.Port.Contact,
                    rotation: Quaternion.identity
                ),
                new WinchSeat(
                    identity: pair.Name + "/starboard",
                    aliases: null,
                    position: pair.Fallback.Starboard.Contact,
                    rotation: Quaternion.identity
                ),
            },
        };

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
