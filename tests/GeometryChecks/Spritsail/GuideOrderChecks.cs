using System;
using MoreSailwindSails.Sails.Spritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Regresses native Junk guide ordering and mast-axis comparisons without Unity transforms.
internal static class GuideOrderChecks
{
    internal static void Run()
    {
        // Installed level24, BOAT junk medium (80), native mainmasts 10/11.
        // Boat-relative positions; all three native slots reuse these pairs.
        var primary = new[]
        {
            new Vector3(-0.04436f, 24.37340f, 1.78215f),
            new Vector3(-0.04435f, 24.37340f, 5.57947f),
        };
        var extension = new[]
        {
            new Vector3(-0.26881f, 5.02526f, 1.85980f),
            new Vector3(-0.26880f, 5.02526f, 5.65853f),
        };
        for (int mast = 0; mast < primary.Length; mast++)
            foreach (float direction in new[] { -1f, 1f })
            {
                bool reversed = SpritsailMastAlignment.ExtensionIsLower(
                    primary: primary[mast],
                    extension: extension[mast],
                    boatLocalAxis: Vector3.up * direction
                );
                Require(
                    value: reversed,
                    message: "Junk mainmast extensions are deck-level return guides, not upper limits."
                );
                var upper = reversed ? primary[mast] : extension[mast];
                Require(
                    value: 15 - extension[mast].y > 0.05f && 15 - upper.y <= 0.05f,
                    message: "A luff at boat height 15 m must fit below the Junk's upper guide despite exceeding the return guide."
                );
                Require(
                    value: 25 - upper.y > 0.05f && 24.35f - upper.y > -0.05f,
                    message: "Selecting the top guide must retain real luff and 5 cm hoist clearance limits."
                );
            }
        foreach (float scale in new[] { 0.01f, 1f, 100f })
        foreach (float direction in new[] { -1f, 1f })
        {
            var axis = new Vector3(0, 1, 1) * (scale * direction);
            // A raked mast's higher axial guide can have a lower boat-space Y.
            var first = new Vector3(0, 8, 0);
            var second = new Vector3(0, 7, 8);
            Require(
                value: !SpritsailMastAlignment.ExtensionIsLower(
                    primary: first,
                    extension: second,
                    boatLocalAxis: axis
                )
                    && SpritsailMastAlignment.ExtensionIsLower(
                        primary: second,
                        extension: first,
                        boatLocalAxis: axis
                    ),
                message: "Guide order must follow the upward mast axis independently of axis sign and scale."
            );
            Require(
                value: !SpritsailMastAlignment.ExtensionIsLower(
                    primary: first,
                    extension: first,
                    boatLocalAxis: axis
                ),
                message: "A shared guide or missing-extension fallback must retain its native order."
            );
        }
        Require(
            value: !SpritsailMastAlignment.ExtensionIsLower(
                primary: Vector3.up * 5,
                extension: Vector3.up * 20,
                boatLocalAxis: Vector3.up
            ),
            message: "Conventional lower-primary/upper-extension pairs must remain unchanged."
        );
        Console.WriteLine(
            "PASS: installed Junk mainmast guide ordering, conventional/shared guides, raked axes and reversed-axis/scale invariance; live shipyard routing remains untested."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message: message);
    }
}
