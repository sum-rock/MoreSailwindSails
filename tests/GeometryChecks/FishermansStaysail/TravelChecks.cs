// Verifies unrestricted fitted trim while retaining protection against native sway.
using System;
using MoreSailwindSails.Sails.FishermansStaysail;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail;

internal static class TravelChecks
{
    internal static void Run()
    {
        Check(
            min: -70,
            max: 75,
            allowedMin: -89,
            allowedMax: 89,
            expectedMin: -70,
            expectedMax: 75
        );
        Check(
            min: -90,
            max: 90,
            allowedMin: -60,
            allowedMax: 80,
            expectedMin: -60,
            expectedMax: 80
        );
        Check(
            min: -45,
            max: 45,
            allowedMin: -20,
            allowedMax: 30,
            expectedMin: -20,
            expectedMax: 30
        );
        Check(min: 11, max: 9, allowedMin: -89, allowedMax: 89, expectedMin: 10, expectedMax: 10);
        Console.WriteLine(
            "PASS: Staysail fitted trim beyond 40 degrees, tighter limits and inverted sway."
        );
    }

    private static void Check(
        float min,
        float max,
        float allowedMin,
        float allowedMax,
        float expectedMin,
        float expectedMax
    )
    {
        FishermansStaysailTravel.ConstrainHinge(
            min: ref min,
            max: ref max,
            allowedMin: allowedMin,
            allowedMax: allowedMax
        );
        if (min != expectedMin || max != expectedMax)
            throw new Exception($"Unexpected staysail hinge limits: {min}, {max}.");
    }
}
