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
        Check(
            ledger.TryAcquireSeats(flying, pair) == true && ledger.Count == 2,
            "Pair not acquired."
        );
        Check(
            ledger.TryAcquireSeats(stay, new[] { Seat(3, -4), Seat(4, 3, 2) }) == false
                && ledger.Count == 2,
            "One-sided conflict left a partial reservation."
        );
        Check(
            ledger.TryAcquireSeats(stay, new[] { Seat(5, 10, 1) }) == false,
            "Alias bypassed native seat identity."
        );
        Check(
            ledger.TryAcquireSeats(stay, new[] { Seat(1, 10) }) == false,
            "Moved native seat bypassed identity reservation."
        );
        Check(
            ledger.TryAcquireSeats(stay, new[] { Seat(6, 10), Seat(7, 10.1f, 6) }) == false,
            "Pair claimed the same seat through an alias."
        );
        Check(
            ledger.TryAcquireSeats(stay, new[] { Seat(8, float.NaN), Seat(9, 20) }) == false,
            "Non-finite pair accepted."
        );
        Check(
            ledger.TryAcquireSeats(stay, new[] { pair[0], null }) == false && ledger.Count == 2,
            "Incomplete pair consumed a seat."
        );
        Check(
            ledger.TryAcquireSeats(flying, pair) == true && ledger.Count == 2,
            "Stable re-acquisition duplicated claims."
        );
        ledger.Release(flying);
        Check(
            ledger.Count == 0 && ledger.TryAcquireSeats(stay, pair) == true,
            "Removal failed to release both seats."
        );
        var halyard = new object();
        Check(
            ledger.TryAcquireSeats(halyard, new[] { Seat(20, 3, 2) }) == false,
            "Halyard ignored sheet reservations."
        );
        Check(ledger.TryAcquireSeats(halyard, new[] { Seat(20, 8) }) == true, "Free halyard lost.");
        ledger.Release(stay);
        Check(ledger.Count == 1, "Pair release removed another owner's halyard.");
        Check(Seat(1, 0).Valid, "Origin incorrectly treated as missing data.");
        Check(
            new WinchReservations().TryAcquireSeats(stay, pair) == true,
            "Boat ledgers shared state."
        );
        var nearbyLedger = new WinchReservations();
        Check(
            nearbyLedger.TryAcquireSeats(flying, new[] { Seat(30, 0), Seat(31, 0.1f) }) == true,
            "Distinct nearby seats within one pair were rejected."
        );
        Check(
            nearbyLedger.TryAcquireSeats(stay, new[] { Seat(32, 0.05f), Seat(33, 0.15f) }) == true
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
