using System;
using MoreSailwindSails.Controls;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Controls;

internal static class NativeSeatAllocationChecks
{
    internal static void Run()
    {
        var ledger = new WinchReservations();
        var flying = new object();
        var stay = new object();
        WinchSeat Seat(int id, float x, params object[] aliases) =>
            new WinchSeat(id, aliases, new Vector3(x, 0, 0), Quaternion.identity);
        var pair = new[] { Seat(1, -2), Seat(2, 3) };
        Check(ledger.AcquireSeats(flying, pair) != null && ledger.Count == 2, "Pair not acquired.");
        Check(
            ledger.AcquireSeats(stay, new[] { Seat(3, -4), Seat(4, 3, 2) }) == null
                && ledger.Count == 2,
            "One-sided conflict left a partial reservation."
        );
        Check(
            ledger.AcquireSeats(stay, new[] { Seat(5, 10, 1) }) == null,
            "Alias bypassed native seat identity."
        );
        Check(
            ledger.AcquireSeats(stay, new[] { Seat(1, 10) }) == null,
            "Moved native seat bypassed identity reservation."
        );
        Check(
            ledger.AcquireSeats(stay, new[] { Seat(6, 10), Seat(7, 10.1f, 6) }) == null,
            "Pair claimed the same seat through an alias."
        );
        Check(
            ledger.AcquireSeats(stay, new[] { Seat(8, float.NaN), Seat(9, 20) }) == null,
            "Non-finite pair accepted."
        );
        Check(
            ledger.AcquireSeats(stay, new[] { pair[0], null }) == null && ledger.Count == 2,
            "Incomplete pair consumed a seat."
        );
        Check(
            ledger.AcquireSeats(flying, pair) != null && ledger.Count == 2,
            "Stable re-acquisition duplicated claims."
        );
        ledger.Release(flying);
        Check(
            ledger.Count == 0 && ledger.AcquireSeats(stay, pair) != null,
            "Removal failed to release both seats."
        );
        var halyard = new object();
        Check(
            ledger.AcquireSeats(halyard, new[] { Seat(20, 3, 2) }) == null,
            "Halyard ignored sheet reservations."
        );
        Check(ledger.AcquireSeats(halyard, new[] { Seat(20, 8) }) != null, "Free halyard lost.");
        ledger.Release(stay);
        Check(ledger.Count == 1, "Pair release removed another owner's halyard.");
        Check(Seat(1, 0).Valid, "Origin incorrectly treated as missing data.");
        Check(
            new WinchReservations().AcquireSeats(stay, pair) != null,
            "Boat ledgers shared state."
        );
        var nearbyLedger = new WinchReservations();
        Check(
            nearbyLedger.AcquireSeats(flying, new[] { Seat(30, 0), Seat(31, 0.1f) }) != null,
            "Distinct nearby seats within one pair were rejected."
        );
        Check(
            nearbyLedger.AcquireSeats(stay, new[] { Seat(32, 0.05f), Seat(33, 0.15f) }) != null
                && nearbyLedger.Count == 4,
            "Nearby seats owned by another sail blocked distinct identities."
        );
        Console.WriteLine(
            "PASS: atomic sheet pairs, native aliases, finite poses, mixed ownership, shared halyard reservations and unrestricted seat proximity."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
