using System;
using System.Linq;
using MoreSailwindSails.Sails.Spritsail;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Exercises balance bounds, invalid tuning and complete, bounded pagination.
internal static class CategoryChecks
{
    internal static void Run()
    {
        foreach (float area in new[] { 0.1f, 1f, 25f, 100f })
        {
            float price = SpritsailRules.Price(area: area);
            Require(
                value: price > area * 9f * 1.25f && price < area * 9f * 1.33f,
                message: "Price must lie strictly between equal-area gaff and junk sails."
            );
            Require(
                value: Math.Abs(SpritsailRules.Mass(realPower: area) - area * 40f) < 0.001f,
                message: "Mass must match the installed gaff/junk calculation."
            );
        }
        foreach (
            float value in new[] { -1f, float.NaN, float.NegativeInfinity, float.PositiveInfinity }
        )
            Require(
                value: SpritsailRules.ValidForceMultiplier(value: value) == 0.75f,
                message: "Invalid force tuning must use the junk baseline."
            );
        foreach (float value in new[] { 0f, 0.75f, 0.85f, 1f, 2f })
            Require(
                value: SpritsailRules.ValidForceMultiplier(value: value) == value,
                message: "Valid tuning, including zero propulsion, must be retained."
            );

        // Native overlap exempts only the gaff/square pair, regardless of order.
        for (int left = 0; left <= 7; left++)
        for (int right = 0; right <= 7; right++)
        {
            int mappedLeft = SpritsailRules.OverlapCategory(category: left);
            int mappedRight = SpritsailRules.OverlapCategory(category: right);
            bool exception =
                (mappedLeft == 3 && mappedRight == 0) || (mappedLeft == 0 && mappedRight == 3);
            bool expected =
                (left == 0 && (right == 3 || right == 6))
                || (right == 0 && (left == 3 || left == 6));
            Require(
                value: exception == expected,
                message: "Only the square/gaff and square/spritsail pairs may bypass vertical overlap."
            );
        }

        foreach (int size in new[] { 4, 12 })
        foreach (int count in new[] { 0, 1, 12, 13, 25 })
        {
            int pages = SpritsailRules.PageCount(count: count, size: size);
            var displayed = Enumerable
                .Range(start: 0, count: pages)
                .SelectMany(page =>
                    Enumerable.Range(start: page * size, count: Math.Min(size, count - page * size))
                )
                .ToArray();
            Require(
                value: displayed.SequenceEqual(Enumerable.Range(start: 0, count: count)),
                message: "Pages must contain every entry exactly once."
            );
            Require(
                value: SpritsailRules.ClampPage(page: -1, count: count, size: size) == 0,
                message: "Previous page must stop at zero."
            );
            Require(
                value: SpritsailRules.ClampPage(page: 999, count: count, size: size)
                    == Math.Max(0, pages - 1),
                message: "Shrinking or empty lists must clamp stale page selections."
            );
        }
        Console.WriteLine(
            "PASS: spritsail price bounds, mass, finite force tuning, symmetric overlap and 0/1/12/13/25-entry pagination."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new Exception(message: message);
    }
}
