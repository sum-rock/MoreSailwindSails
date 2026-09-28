using System;
using MoreSailwindSails.Controls;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Controls;

// Executes the production discovery schedule and retention policy with fixed data;
// counts represent a model of the old coordinator, not Unity profiling.
internal static class WinchRefreshChecks
{
    internal static void Run()
    {
        var discovery = new WinchDiscoverySchedule();
        var ledger = new WinchReservations();
        var owner = new object();
        var pair = new WinchCandidate
        {
            Id = "pair",
            Supported = true,
            Vacant = true,
            Seats = new[]
            {
                new WinchSeat(
                    identity: 1,
                    aliases: new object[] { 11 },
                    position: Vector3.zero,
                    rotation: Quaternion.identity
                ),
                new WinchSeat(
                    identity: 2,
                    aliases: null,
                    position: Vector3.right,
                    rotation: Quaternion.identity
                ),
            },
        };
        WinchCandidate current = null;
        int searches = 0,
            scans = 0;
        const int frames = 600;
        for (int frame = 0; frame < frames; frame++)
        {
            float now = frame / 60f;
            if (discovery.Due(now: now))
            {
                scans += 2;
                discovery.Discovered(now: now);
            }
            if (
                !WinchPlacementPolicy.TryRetain(
                    ledger: ledger,
                    owner: owner,
                    current: current,
                    valid: true
                )
            )
            {
                searches++;
                current = WinchPlacementPolicy
                    .Resolve(ledger: ledger, owner: owner, current: current, native: new[] { pair })
                    .Candidate;
            }
        }
        Check(
            scans == 20 && searches == 1 && ledger.EntriesCreated == 2,
            "Settled placement rediscovered, searched or recreated claims every frame."
        );
        Console.WriteLine(
            $"PASS (model, {frames} frames/60 Hz, one pair): hierarchy-scan requests {frames * 2}->{scans}, candidate builds {frames}->{searches}, reservation entries {frames * 2}->{ledger.EntriesCreated}. Not Unity frame-time measurements."
        );
        Check(!discovery.Due(now: 9.99f), "Periodic discovery ran too soon.");
        discovery.Invalidate();
        Check(discovery.Due(now: 9.99f), "Lifecycle change waits for periodic discovery.");
        discovery.Discovered(now: 9.99f);
        Check(discovery.Due(now: 11f), "Incomplete startup inventory cannot recover periodically.");
        Check(
            !WinchPlacementPolicy.TryRetain(
                ledger: ledger,
                owner: owner,
                current: current,
                valid: false
            ),
            "Reclaim, changed arrays/pose or lost support retained an invalid placement."
        );
        pair.Vacant = false;
        Check(
            WinchPlacementPolicy
                .Resolve(ledger: ledger, owner: owner, current: current, native: new[] { pair })
                .Candidate == null
                && ledger.Count == 0,
            "Native reclaim did not release the whole pair."
        );
        pair.Vacant = true;
        current = WinchPlacementPolicy
            .Resolve(ledger: ledger, owner: owner, current: null, native: new[] { pair })
            .Candidate;
        Check(current == pair && ledger.Count == 2, "Freed seats did not recover.");
        long allocations = ledger.EntriesCreated;
        Check(
            ledger.TryAcquireSeats(owner: owner, seats: pair.Seats)
                && ledger.EntriesCreated == allocations,
            "Unchanged acquisition recreated entries."
        );
        Check(
            !ledger.TryAcquireSeats(owner: owner, seats: new[] { pair.Seats[0], pair.Seats[0] })
                && ledger.HoldsSeats(owner: owner, seats: pair.Seats),
            "Failed replacement discarded an existing claim."
        );
        ledger.Release(owner: owner);
        Check(
            !WinchPlacementPolicy.TryRetain(
                ledger: ledger,
                owner: owner,
                current: current,
                valid: true
            ),
            "Released owner retained unclaimed controls."
        );
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
