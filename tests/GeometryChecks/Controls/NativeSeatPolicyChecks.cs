using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Controls;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Controls;

internal static class NativeSeatPolicyChecks
{
    internal static void Run()
    {
        var left = new[] { new object(), null, new object() };
        var right = new[] { new object(), new object() };
        int unmatched = 0;
        var indices = NativeSheetPairing
            .Indices(left, right, o => o != null, _ => unmatched++)
            .ToArray();
        Check(
            indices.Length == 1 && indices[0][0] == 0 && indices[0][1] == 0 && unmatched == 2,
            "Unequal/null arrays synthesized a pair or missed diagnostics."
        );
        Check(
            !NativeSheetPairing.Indices(left, null, o => o != null, _ => { }).Any(),
            "Missing right array produced a partial pair."
        );
        var ledger = new WinchReservations();
        var owner = new object();
        WinchCandidate Pair(string id, float x, bool fallback = false) =>
            new WinchCandidate
            {
                Id = id,
                Supported = true,
                Vacant = true,
                Fallback = fallback,
                Seats = new[]
                {
                    new WinchSeat(id + "/L", null, new Vector3(x, 0, 1), Quaternion.identity),
                    new WinchSeat(
                        id + "/R",
                        null,
                        new Vector3(x + 4, 0.3f, -2),
                        Quaternion.identity
                    ),
                },
            };
        // Distinct coincident references: inactive first, active second. Test a
        // whole sheet pair and a single halyard without mixing their identities.
        foreach (int count in new[] { 1, 2 })
        {
            var inactive = Pair("inactive", 0);
            var active = Pair("active", 0);
            inactive.Supported = false;
            inactive.Seats = inactive.Seats.Take(count).ToArray();
            active.Seats = active
                .Seats.Take(count)
                .Select(
                    (seat, i) =>
                        new WinchSeat(
                            identity: seat.Identity,
                            aliases: new[] { inactive.Seats[i].Identity },
                            position: seat.Position,
                            rotation: seat.Rotation
                        )
                )
                .ToArray();
            var representatives = new List<WinchCandidate>();
            WinchPlacementPolicy.AddSupportedRepresentative(
                candidates: representatives,
                candidate: inactive
            );
            WinchPlacementPolicy.AddSupportedRepresentative(
                candidates: representatives,
                candidate: active
            );
            Check(
                representatives.Count == 1 && representatives[0] == active,
                "Inactive coincident representative hid active controls."
            );
            var localLedger = new WinchReservations();
            Check(
                WinchPlacementPolicy
                    .Resolve(
                        ledger: localLedger,
                        owner: owner,
                        current: null,
                        native: representatives.ToArray()
                    )
                    .Candidate == active,
                "Supported equivalent was not selected."
            );
            Check(
                !localLedger.TryAcquireSeats(owner: new object(), seats: inactive.Seats),
                "Coincident references allowed duplicate reservations."
            );
            active.Vacant = false; // live occupancy includes every alias, even inactive references
            Check(
                WinchPlacementPolicy
                    .Resolve(
                        ledger: localLedger,
                        owner: owner,
                        current: active,
                        native: representatives.ToArray()
                    )
                    .Candidate == null
                    && localLedger.Count == 0,
                "Occupied alias allowed borrowing or retained a claim."
            );
        }
        var a = Pair("a", 0);
        var b = Pair("b", 10);
        var fallback = Pair("fallback", 20, true);
        var native = new[] { a, b };
        WinchResolution Resolve(WinchCandidate current = null, string missing = null) =>
            WinchPlacementPolicy.Resolve(ledger, owner, current, native, fallback, missing);
        Check(Resolve().Candidate == a, "Native order ignored.");
        Check(a.Seats[1].Position.y == 0.3f, "Asymmetry lost.");
        a.Vacant = false;
        Check(
            Resolve(a).Candidate == b && ledger.Count == 2,
            "Native reclaim did not move whole pair."
        );
        b.Vacant = false;
        Check(Resolve(b).Candidate == fallback, "Occupied native pairs did not select fallback.");
        a.Vacant = true;
        Check(
            Resolve(fallback).Candidate == fallback,
            "A newly free native seat moved a valid fallback."
        );
        fallback.Supported = false;
        Check(Resolve(fallback).Candidate == a, "Absent support retained a claim.");
        var missing = WinchPlacementPolicy.Resolve(
            ledger,
            owner,
            a,
            native,
            null,
            "port/starboard"
        );
        Check(
            missing.Candidate == a && missing.MissingFallbackSide == null,
            "Missing fallback blocked native success."
        );
        a.Vacant = false;
        missing = WinchPlacementPolicy.Resolve(ledger, owner, a, native, null, "starboard");
        Check(
            missing.Candidate == null
                && missing.MissingFallbackSide == "starboard"
                && ledger.Count == 0,
            "Missing fallback left controls reserved."
        );
        for (int i = 0; i < 3; i++)
            Check(Resolve(null).Candidate == null && ledger.Count == 0, "Retry leaked claims.");
        b.Vacant = true;
        Check(Resolve().Candidate == b && ledger.Count == 2, "Native recovery failed.");
        // Movement invalidates the previous pose, then claims the newly measured pair.
        var moved = Pair("b", 30);
        Check(!moved.SamePlacement(b), "Boat-relative source movement ignored.");
        Check(
            WinchPlacementPolicy.Resolve(ledger, owner, b, new[] { moved }).Candidate == moved,
            "Moved native pair not reacquired."
        );
        var other = new object();
        Check(
            WinchPlacementPolicy.Resolve(ledger, other, null, new[] { moved }).Reserved == 1,
            "Custom reservation failure not classified."
        );
        // Fallbacks trust authored geometry, even near an existing native reservation.
        var nearbyFallback = Pair("nearbyFallback", 30.01f, true);
        var fallbackOwner = new object();
        Check(
            WinchPlacementPolicy
                .Resolve(ledger, fallbackOwner, null, new[] { moved }, nearbyFallback)
                .Candidate == nearbyFallback,
            "Native reservation proximity blocked an authored fallback."
        );
        Check(
            WinchPlacementPolicy
                .Resolve(ledger, new object(), null, Array.Empty<WinchCandidate>(), nearbyFallback)
                .FallbackBlocked == 1,
            "Another sail reused the same fallback seats."
        );
        var invalidSeat = new SheetFallbackSeat(
            new Vector3(float.NaN, 0, 0),
            Vector3.up,
            Vector3.up,
            0,
            1,
            WinchRole.Left,
            0
        );
        Check(
            new SheetFallbackPair(invalidSeat, null).InvalidSide == "port",
            "Invalid vector accepted."
        );
        var origin = new SheetFallbackSeat(
            Vector3.zero,
            Vector3.up,
            Vector3.up,
            0,
            1,
            WinchRole.Left,
            0
        );
        Check(
            origin.Valid && new SheetFallbackPair(origin, null).InvalidSide == "starboard",
            "Origin or half-defined fallback mishandled."
        );
        Console.WriteLine(
            "PASS: native-first pairs, stable fallback, native reclaim, support loss, missing-data recovery and asymmetric poses."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
