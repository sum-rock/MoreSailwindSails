using System;
using MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail.LooseFootedSpritsail;

// Exercises sheet loading, bounded curvature and both curved-edge budgets using the actual Mk.A cut.
internal static class FlexChecks
{
    internal static void Run()
    {
        Require(
            LooseFootedSpritsailFlex.Load(paidOut: 10, routed: 8) == 0,
            "Slack sheet must not pull."
        );
        Require(
            LooseFootedSpritsailFlex.Load(paidOut: 10, routed: 10) == 1,
            "Taut sheet must pull."
        );
        Require(
            LooseFootedSpritsailFlex.Load(paidOut: float.NaN, routed: 1) == 0,
            "Invalid sheet must not pull."
        );
        var opposing = LooseFootedSpritsailFlex.Pull(
            clew: Vector3.zero,
            port: Vector3.up,
            portLoad: 1,
            starboard: Vector3.down,
            starboardLoad: 1
        );
        Require(opposing.sqrMagnitude < 1e-10f, "Opposing sheets should cancel symmetrically.");
        Require(
            LooseFootedSpritsailFlex
                .Pull(
                    clew: Vector3.zero,
                    port: Vector3.up,
                    portLoad: 0,
                    starboard: Vector3.down,
                    starboardLoad: 0
                )
                .sqrMagnitude == 0,
            "Missing/slack sheets must relax the panel."
        );
        var smooth = LooseFootedSpritsailFlex.Smooth(
            previous: Vector3.up,
            target: Vector3.down,
            seconds: 0.016f
        );
        Require(smooth.y < 1 && smooth.y > 0, "A tack must begin smoothly without snapping.");
        foreach (
            var definition in new[]
            {
                MoreSailwindSails
                    .Sails
                    .Spritsail
                    .LooseFootedSpritsail
                    .MkA
                    .LooseFootedSpritsailMkA
                    .Definition,
                MoreSailwindSails
                    .Sails
                    .Spritsail
                    .LooseFootedSpritsail
                    .MkB
                    .LooseFootedSpritsailMkB
                    .Definition,
            }
        )
            for (int scaleCase = 0; scaleCase < 3; scaleCase++)
            {
                var data = LooseFootedSpritsailGeometry.Create(width: 6, definition: definition);
                var scale =
                    scaleCase == 0 ? Vector3.one
                    : scaleCase == 1 ? new Vector3(0.6f, 1.3f, 1.5f)
                    : new Vector3(2, 2, 2);
                var c = data.Corners;
                float Weight(Vector3 p) =>
                    LooseFootedSpritsailFlex.Weight(point: p, peak: c[1], tack: c[2], clew: c[3]);
                Require(
                    Weight(c[0]) == 0
                        && Weight(c[1]) == 0
                        && Weight(c[2]) == 0
                        && Weight(c[3]) == 1,
                    "Upper corners must stay fixed and the clew must receive full flex."
                );
                for (int i = 0; i <= 32; i++)
                    Require(
                        Weight(Vector3.Lerp(c[1], c[2], i / 32f)) < 1e-10f,
                        "Diagonal must remain fixed."
                    );
                for (int reef = 0; reef <= 4; reef++)
                {
                    float amount = reef / 4f;
                    var scaled = new Vector3[4];
                    for (int i = 0; i < 4; i++)
                        scaled[i] = Vector3.Scale(c[i], scale);
                    var pose = MoreSailwindSails.Sails.Spritsail.SpritsailDeployment.Evaluate(
                        corners: scaled,
                        unroll: 0.02f + amount * 0.96f
                    );
                    var foot = new Vector3[13];
                    var leech = new Vector3[33];
                    for (int i = 0; i < foot.Length; i++)
                        foot[i] = LooseFootedSpritsailGathering.Point(
                            corners: scaled,
                            pose: pose,
                            amount: amount,
                            u: i / 12f,
                            v: 1,
                            normalScale: scale.y / scale.z
                        );
                    for (int i = 0; i < leech.Length; i++)
                        leech[i] = LooseFootedSpritsailGathering.Point(
                            corners: scaled,
                            pose: pose,
                            amount: amount,
                            u: 1,
                            v: i / 32f,
                            normalScale: scale.y / scale.z
                        );
                    float limit =
                        (scaled[3] - scaled[2]).magnitude
                        * LooseFootedSpritsailFlex.MaximumDisplacement
                        * amount;
                    foreach (
                        var direction in new[]
                        {
                            Vector3.up,
                            Vector3.down,
                            new Vector3(-0.2f, 1, -0.3f).normalized,
                            new Vector3(0.4f, -1, 0.2f).normalized,
                        }
                    )
                    {
                        var delta = LooseFootedSpritsailFlex.Fit(
                            requested: direction * limit,
                            foot: foot,
                            leech: leech,
                            limit: limit
                        );
                        Require(
                            delta.magnitude <= limit + 1e-5f,
                            "Clew travel must be bounded at every scale and reef state."
                        );
                        Require(
                            LooseFootedSpritsailFlex.Arc(edge: foot, displacement: delta)
                                <= LooseFootedSpritsailFlex.Arc(
                                    edge: foot,
                                    displacement: Vector3.zero
                                ) + 1e-5f,
                            "Curved foot must not stretch."
                        );
                        Require(
                            LooseFootedSpritsailFlex.Arc(edge: leech, displacement: delta)
                                <= LooseFootedSpritsailFlex.Arc(
                                    edge: leech,
                                    displacement: Vector3.zero
                                ) + 1e-5f,
                            "Curved leech must not stretch."
                        );
                        if (amount == 1 && direction == Vector3.up)
                            Require(
                                delta.y > limit * 0.3f,
                                "A taut transverse sheet must visibly flex the deployed sail."
                            );
                        if (amount == 0)
                            Require(delta.sqrMagnitude == 0, "Struck sail must have no flex.");
                    }
                    if (amount == 1)
                    {
                        // Reflect the complete sail, including its existing camber, across the sail plane.
                        var first = LooseFootedSpritsailFlex.Fit(
                            requested: Vector3.up * limit,
                            foot: foot,
                            leech: leech,
                            limit: limit
                        );
                        for (int i = 0; i < foot.Length; i++)
                            foot[i].y = -foot[i].y;
                        for (int i = 0; i < leech.Length; i++)
                            leech[i].y = -leech[i].y;
                        var second = LooseFootedSpritsailFlex.Fit(
                            requested: Vector3.down * limit,
                            foot: foot,
                            leech: leech,
                            limit: limit
                        );
                        Require(
                            (first - new Vector3(second.x, -second.y, second.z)).magnitude < 1e-5f,
                            "Port and starboard fits must mirror."
                        );
                    }
                }
            }
        Require(
            LooseFootedSpritsailFlex
                .Fit(requested: new Vector3(float.NaN, 0, 0), foot: null, leech: null, limit: 1)
                .sqrMagnitude == 0,
            "Invalid inputs must have a finite fallback."
        );
        Console.WriteLine(
            "PASS: loose-footed sheet load, fixed upper panel, bounded flex, curved edge budgets, scales, reefs and mirrored tacks."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }
}
