// Exercises dependency cycles, inactive donors, complete fallback ordering and shared position claims.
using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Controls;
using MoreSailwindSails.Controls.Configuration;
using MoreSailwindSails.Controls.Discovery;
using MoreSailwindSails.Controls.Placement;
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
        bool Matches(string option, string fore, string aft) =>
            StayDependencies.ReachesBoth(
                option: option,
                requires: current => graph[current],
                matchesFirst: current => current == fore,
                matchesSecond: current => current == aft
            );
        Check(
            Matches("inactive stay", "fore", "aft")
                && Matches("inactive stay", "aft", "fore")
                && Matches("inactive stay", "top", "aft"),
            "Both mast dependencies must be reachable, including below an already matched section."
        );
        Check(
            Matches("cycle", "fore", "aft")
                && !Matches("unrelated", "fore", "aft")
                && !Matches("inactive stay", "missing", "aft")
                && !Matches("inactive stay", "fore", "missing")
                && !Matches(null, "fore", "aft"),
            "Cycles must terminate and one missing endpoint must reject the donor."
        );
        Check(
            !Matches("inactive stay", "inactive stay", "aft"),
            "The source itself is not a mast dependency."
        );
        // Installed Chronian dependencies, including each top section's physical base.
        graph["mizzen"] = Array.Empty<string>();
        graph["fore top"] = new[] { "fore" };
        graph["mizzen top"] = new[] { "mizzen" };
        graph["11"] = new[] { "fore", "aft" };
        graph["12"] = new[] { "fore top", "top" };
        graph["13"] = new[] { "aft", "mizzen" };
        graph["14"] = new[] { "top", "mizzen top" };
        var chronian = new[] { "11", "12", "13", "14" };
        Check(
            chronian.Where(id => Matches(id, "fore", "aft")).SequenceEqual(new[] { "11", "12" })
                && chronian
                    .Where(id => Matches(id, "aft", "mizzen"))
                    .SequenceEqual(new[] { "13", "14" }),
            "Chronian must separate fore/main donors from main/mizzen donors."
        );
        var visits = new Dictionary<string, int>();
        Check(
            !StayDependencies.ReachesBoth(
                option: "cycle",
                requires: node =>
                {
                    visits[node] = visits.GetValueOrDefault(node) + 1;
                    return graph[node];
                },
                matchesFirst: node => node == "fore",
                matchesSecond: node => node == "missing"
            ) && visits.Values.All(count => count == 1),
            "Both targets must share one cycle-safe traversal."
        );
        var pairLedger = new WinchReservations();
        var wrongPair = new WinchCandidate
        {
            Id = "14",
            Supported = true,
            Vacant = true,
            Seats = new[]
            {
                new WinchSeat(
                    identity: "royalstay",
                    aliases: null,
                    position: Vector3.zero,
                    rotation: Quaternion.identity
                ),
            },
        };
        var authored = new WinchCandidate
        {
            Id = "main fallback",
            Fallback = true,
            Supported = true,
            Vacant = true,
            Seats = new[]
            {
                new WinchSeat(
                    identity: "main fallback",
                    aliases: null,
                    position: Vector3.right,
                    rotation: Quaternion.identity
                ),
            },
        };
        var pairOwner = new object();
        pairLedger.TryAcquireSeats(owner: pairOwner, seats: wrongPair.Seats);
        Check(
            !WinchPlacementPolicy.TryRetain(
                ledger: pairLedger,
                owner: pairOwner,
                current: wrongPair,
                valid: Matches(wrongPair.Id, "fore", "aft")
            )
                && WinchPlacementPolicy
                    .Resolve(
                        ledger: pairLedger,
                        owner: pairOwner,
                        current: wrongPair,
                        native: new[] { wrongPair }
                            .Where(candidate => Matches(candidate.Id, "fore", "aft"))
                            .ToArray(),
                        fallback: authored
                    )
                    .Candidate == authored
                && !pairLedger.HoldsSeats(owner: pairOwner, seats: wrongPair.Seats),
            "A retained cross-pair donor must release its claim and allow the authored fallback."
        );
        SheetFallbackSeat Side(float x, float z) =>
            new SheetFallbackSeat(contact: new Vector3(x, 2, z), normal: Vector3.up);
        SheetFallbackDefinition Pair(string name, float z, float otherZ) =>
            new SheetFallbackDefinition(
                name: name,
                fallback: new SheetFallbackPair(
                    templateMast: 5,
                    templateRole: WinchRole.Left,
                    templateIndex: 0,
                    sourceNormal: Vector3.up,
                    offset: 0.044704f,
                    port: Side(-2, z),
                    starboard: Side(3, otherZ)
                )
            );
        var far = Pair("far", -8, -9);
        var first = Pair("first", -3, -4);
        var forward = Pair("forward", 1, 1);
        var crossing = Pair("crossing", 1, -6);
        const float forwardMastZ = 0f;
        const float aftMastZ = -6f;
        var candidates = SheetFallbackOrder
            .FromBoundary(
                fallbacks: new[] { far, forward, crossing, first },
                aftBoundaryZ: forwardMastZ
            )
            .ToArray();
        Check(
            candidates.SequenceEqual(new[] { first, far }),
            "Fallbacks must preserve complete pairs and follow fore-to-aft encounter order."
        );
        Check(
            SheetFallbackOrder
                .FromBoundary(fallbacks: candidates, aftBoundaryZ: aftMastZ)
                .SequenceEqual(new[] { far })
                && SheetFallbackOrder.FindCurrent(
                    fallbacks: candidates,
                    id: first.FallbackName,
                    aftBoundaryZ: forwardMastZ
                ) == first
                && SheetFallbackOrder.FindCurrent(
                    fallbacks: candidates,
                    id: first.FallbackName,
                    aftBoundaryZ: aftMastZ
                ) == null,
            "The forward mast boundary must allow selecting and retaining the first pair between the masts."
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
                    fallback: new SheetFallbackPair(
                        templateMast: 5,
                        templateRole: WinchRole.Left,
                        templateIndex: 0,
                        sourceNormal: Vector3.up,
                        offset: 0.044704f,
                        port: Side(x: -2, z: -3),
                        starboard: null
                    )
                ),
                Pair(name: "nonfinite", z: float.NaN, otherZ: -4),
            }
        );
        foreach (float mastZ in new[] { -10f, -8f, -3.001f, -3f, 0f, 1f, float.NaN })
        foreach (string id in fallbacks.Select(item => item.FallbackName).Append("missing"))
            Check(
                SheetFallbackOrder.FindCurrent(fallbacks: fallbacks, id: id, aftBoundaryZ: mastZ)
                    == SheetFallbackOrder
                        .FromBoundary(fallbacks: fallbacks, aftBoundaryZ: mastZ)
                        .FirstOrDefault(item => item.FallbackName == id),
                "Direct fallback lookup differs from ordered selection: " + id
            );
        Check(
            SheetFallbackOrder.FindCurrent(
                fallbacks: fallbacks,
                id: first.FallbackName,
                aftBoundaryZ: -3
            ) == first
                && SheetFallbackOrder.FindCurrent(
                    fallbacks: fallbacks,
                    id: first.FallbackName,
                    aftBoundaryZ: -3.001f
                ) == null
                && SheetFallbackOrder.FindCurrent(
                    fallbacks: fallbacks,
                    id: first.FallbackName,
                    aftBoundaryZ: 0
                ) == first,
            "Fallback retention must include the mast boundary and react immediately to mast movement."
        );
        Check(
            SheetFallbackOrder.FindCurrent(
                fallbacks: Array.Empty<SheetFallbackDefinition>(),
                id: first.FallbackName,
                aftBoundaryZ: 0
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
                    aftBoundaryZ: SheetFallbackOrder.Boundary(mastZ: 5, fittedSpan: 5)
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
