// Checks shared Fisherman order recognition and bounded, lossless text wrapping.
using System;
using System.Linq;
using MoreSailwindSails.Compatibility;

namespace MoreSailwindSails.Tests.GeometryChecks.Compatibility;

internal static class OrderTextChecks
{
    internal static void Run()
    {
        // NANDFixes 1.4.4 still recurses with an identical long arrow prefix.
        string removal = "192: (ERROR): Fisherman's Flying Sail (150% x 115%) -> (no sail)";
        string recursive = removal.Substring(
            0,
            removal.IndexOf("->", StringComparison.Ordinal) + 2
        );
        Check(
            recursive.Length > 45
                && recursive.Substring(0, recursive.IndexOf("->", StringComparison.Ordinal) + 2)
                    == recursive,
            "The removal order no longer reproduces non-progressing recursion."
        );
        foreach (
            string name in new[]
            {
                "Fisherman's Flying Sail",
                "Fisherman's Staysail Mk.A",
                "Fisherman's Staysail Mk.B",
                "Fisherman's Staysail Mk.C",
                "Fisherman's Stay",
            }
        )
        {
            foreach (
                string line in new[]
                {
                    $"192: (no sail) -> {name} (65%)",
                    $"192: (ERROR): {name} (150% x 115%) -> (no sail)",
                    $"192: {name} (65%) -> {name} (150%)",
                    $"(ERROR): {name} requires: main mast and topmast",
                    name + " " + new string('x', 200),
                    name + "\nrequires: main mast and topmast",
                    name + "\r\nrequires: main mast and topmast",
                    name.PadRight(45),
                    name.PadRight(46),
                }
            )
            {
                Check(
                    FishermanOrderText.NeedsWrapping(line) == (line.Length > 45),
                    "Only long Fisherman orders should be intercepted."
                );
                var wrapped = FishermanOrderText.Wrap(line).ToArray();
                Check(wrapped.All(s => s.Length <= 45), "An order line exceeds the safe width.");
                Check(
                    string.Concat(wrapped).Replace(" ", "")
                        == line.Replace(" ", "").Replace("\r", "").Replace("\n", ""),
                    "Wrapping lost order details."
                );
            }
        }
        foreach (
            string line in new[]
            {
                null,
                "",
                "192: shipyard fee",
                "(no sail)",
                new string('x', 100),
                "Fisherman's Staying " + new string('x', 80),
                "Fisherman's Staycation " + new string('x', 80),
            }
        )
            Check(
                !FishermanOrderText.NeedsWrapping(line),
                "Unrelated text must keep native handling."
            );
        Check(
            FishermanOrderText.NeedsWrapping(
                "Fisherman's Staycation -> Fisherman's Stay (foremast 1)"
            ),
            "A false name prefix hid a later valid stay name."
        );
        foreach (string newline in new[] { "\n", "\r\n" })
            Check(
                FishermanOrderText
                    .Wrap("first" + newline + newline + "second")
                    .SequenceEqual(new[] { "first", "", "second" }),
                "Explicit line breaks were lost."
            );
        Console.WriteLine(
            "PASS: shared Fisherman order scope, recursion reproducer, width boundaries, long words and LF/CRLF wrapping."
        );
    }

    private static void Check(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
    }
}
