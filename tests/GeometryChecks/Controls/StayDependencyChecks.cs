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
            new SheetFallbackSeat(contact: new Vector3(x, 2, z), normal: Vector3.up);
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
        var fallbacks = Array.AsReadOnly(
            new[]
            {
                far,
                forward,
                crossing,
                first,
                new SheetFallbackDefinition(name: "empty", fallback: null),
                new SheetFallbackDefinition(
                    name: "incomplete",
                    fallback: new SheetFallbackPair(port: Side(x: -2, z: -3), starboard: null)
                ),
                Pair(name: "nonfinite", z: float.NaN, otherZ: -4),
            }
        );
        foreach (float mastZ in new[] { -10f, -8f, -3.001f, -3f, 0f, 1f, float.NaN })
        foreach (string id in fallbacks.Select(item => item.FallbackName).Append("missing"))
            Check(
                SheetFallbackOrder.FindCurrent(fallbacks: fallbacks, id: id, mastZ: mastZ)
                    == SheetFallbackOrder
                        .FromMast(fallbacks: fallbacks, mastZ: mastZ)
                        .FirstOrDefault(item => item.FallbackName == id),
                "Direct fallback lookup differs from ordered selection: " + id
            );
        Check(
            SheetFallbackOrder.FindCurrent(fallbacks: fallbacks, id: first.FallbackName, mastZ: -3)
                == first
                && SheetFallbackOrder.FindCurrent(
                    fallbacks: fallbacks,
                    id: first.FallbackName,
                    mastZ: -3.001f
                ) == null
                && SheetFallbackOrder.FindCurrent(
                    fallbacks: fallbacks,
                    id: first.FallbackName,
                    mastZ: 0
                ) == first,
            "Fallback retention must include the mast boundary and react immediately to mast movement."
        );
        Check(
            SheetFallbackOrder.FindCurrent(
                fallbacks: Array.Empty<SheetFallbackDefinition>(),
                id: first.FallbackName,
                mastZ: 0
            ) == null,
            "An empty profile must have no retained fallback."
        );
        foreach (var item in fallbacks)
            Check(
                item.FallbackName == item.Name + "Fallback"
                    && ReferenceEquals(item.FallbackName, item.FallbackName),
                "Sheet fallback identifiers must preserve their value and reuse the stored string."
            );
        void ReadCurrent()
        {
            if (
                SheetFallbackOrder.FindCurrent(
                    fallbacks: fallbacks,
                    id: first.FallbackName,
                    mastZ: 0
                ) != first
            )
                throw new Exception("Warmed fallback lookup lost its selected pair.");
        }
        for (int i = 0; i < 1000; i++)
            ReadCurrent();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++)
            ReadCurrent();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, "Warmed fallback ID/lookup allocated bytes: " + allocated);
        Console.WriteLine(
            "PASS: fallback lookup equivalence, live mast boundary and 0 bytes across 10,000 warmed ID/lookups (.NET host)."
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
