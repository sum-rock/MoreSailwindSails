// Exercises dependency cycles, inactive donors, complete fallback ordering and shared position claims.
using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Controls;
using MoreSailwindSails.Controls.Configuration;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Controls;

internal static class StayDependencyChecks
{
    internal static void Run()
    {
        var graph = new Dictionary<string, string[]>
        {
            ["aft"] = Array.Empty<string>(),
            ["fore"] = Array.Empty<string>(),
            ["top"] = new[] { "aft" },
            ["inactive stay"] = new[] { "fore", "top" },
            ["cycle"] = new[] { "cycle", "inactive stay" },
            ["unrelated"] = new[] { "fore", "loop" },
            ["loop"] = new[] { "unrelated", null },
        };
        bool Reaches(string option, string mast) =>
            StayDependencies.Reaches(
                option: option,
                requires: current => graph[current],
                matches: current => current == mast
            );
        Check(
            Reaches("inactive stay", "aft") && Reaches("inactive stay", "fore"),
            "Dependencies through topmasts or another endpoint were lost."
        );
        Check(
            Reaches("cycle", "aft") && !Reaches("unrelated", "aft"),
            "Cycles must terminate without hiding reachable support."
        );
        Check(!Reaches("aft", "aft"), "The option itself is not a dependency.");
        SheetFallbackSeat Side(float x, float z) =>
            new SheetFallbackSeat(
                contact: new Vector3(x, 2, z),
                normal: Vector3.up,
                sourceNormal: Vector3.up,
                offset: 0,
                templateMast: 1,
                templateRole: x < 0 ? WinchRole.Left : WinchRole.Right,
                templateIndex: 0
            );
        SheetFallbackDefinition Pair(string name, float z, float otherZ) =>
            new SheetFallbackDefinition(
                name: name,
                fallback: new SheetFallbackPair(port: Side(-2, z), starboard: Side(3, otherZ))
            );
        var far = Pair("far", -8, -9);
        var first = Pair("first", -3, -4);
        var forward = Pair("forward", 1, 1);
        var crossing = Pair("crossing", 1, -6);
        var candidates = SheetFallbackOrder
            .FromMast(fallbacks: new[] { far, forward, crossing, first }, mastZ: 0)
            .ToArray();
        Check(
            candidates.SequenceEqual(new[] { first, far }),
            "Fallbacks must preserve complete pairs and follow fore-to-aft encounter order."
        );
        var ledger = new WinchReservations();
        WinchCandidate Candidate(SheetFallbackDefinition pair) =>
            new WinchCandidate
            {
                Id = pair.Name,
                Fallback = true,
                Supported = true,
                Vacant = true,
                Seats = new[] { pair.Fallback.Port, pair.Fallback.Starboard }
                    .Select(
                        (seat, i) =>
                            new WinchSeat(
                                identity: pair.Name + i,
                                aliases: null,
                                position: seat.Contact,
                                rotation: Quaternion.identity
                            )
                    )
                    .ToArray(),
            };
        var ordered = candidates.Select(Candidate).ToArray();
        var firstOwner = new object();
        Check(
            ledger.TryAcquireSeats(owner: firstOwner, seats: ordered[0].Seats),
            "Initial pair claim failed."
        );
        var secondOwner = new object();
        var resolved = WinchPlacementPolicy.Resolve(
            ledger: ledger,
            owner: secondOwner,
            current: null,
            native: ordered
        );
        Check(
            resolved.Candidate == ordered[1],
            "A claimed fallback must allow the next authored pair aft."
        );
        Check(
            !ledger.TryAcquireSeats(
                owner: new object(),
                seats: new[]
                {
                    new WinchSeat(
                        identity: "different reference",
                        aliases: null,
                        position: ordered[1].Seats[0].Position + new Vector3(0.0005f, 0, 0),
                        rotation: Quaternion.identity
                    ),
                }
            ),
            "Coincident controls with different references must conflict."
        );
        ordered[1].Vacant = false;
        Check(
            WinchPlacementPolicy
                .Resolve(ledger: ledger, owner: secondOwner, current: ordered[1], native: ordered)
                .Candidate == null
                && ledger.Count == 2,
            "Native reclaim must release both fallback claims without consuming another owner's pair."
        );
        ledger.Release(owner: firstOwner);
        Check(
            WinchPlacementPolicy
                .Resolve(ledger: ledger, owner: secondOwner, current: null, native: ordered)
                .Candidate == ordered[0],
            "Released fallback did not recover after exhaustion."
        );
        Console.WriteLine(
            "PASS: transitive/cyclic stay dependencies, ordered complete aft fallbacks, coincident claims, reclaim and exhaustion recovery."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
