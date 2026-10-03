using System;
using MoreSailwindSails.Sails.Spritsail;
using MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Exercises tack selection, penalty limits and localized shaping without Unity simulation.
internal static class ObstructionChecks
{
    internal static void Run()
    {
        Require(
            SpritsailObstructionGeometry.Target(
                boatWind: new Vector3(-3, 0, -4),
                starboardAffected: true,
                previous: 0
            ) == 1,
            "Wind arriving from starboard must obstruct Mk.A."
        );
        Require(
            SpritsailObstructionGeometry.Target(
                boatWind: new Vector3(3, 0, -4),
                starboardAffected: true,
                previous: 1
            ) == 0,
            "Port tack must remain clear."
        );
        Require(
            SpritsailObstructionGeometry.Target(
                boatWind: new Vector3(3, 0, -4),
                starboardAffected: false,
                previous: 0
            ) == 1,
            "Port-mounted makes must reverse the affected tack."
        );
        foreach (float side in new[] { -0.02f, 0f, 0.02f })
            Require(
                SpritsailObstructionGeometry.Target(
                    boatWind: new Vector3(side, 0, 1),
                    starboardAffected: true,
                    previous: 1
                ) == 1,
                "Centerline deadband must retain its state."
            );
        Require(
            SpritsailObstructionGeometry.Target(
                boatWind: Vector3.zero,
                starboardAffected: true,
                previous: 1
            ) == 0,
            "Calm must clear the target."
        );
        Require(
            SpritsailObstructionGeometry.Target(
                boatWind: new Vector3(float.NaN, 0, 0),
                starboardAffected: true,
                previous: 1
            ) == 0,
            "Invalid wind must be neutral."
        );
        float amount = 0;
        for (int i = 0; i < 25; i++)
        {
            float next = SpritsailObstructionGeometry.Step(
                previous: amount,
                target: 1,
                seconds: 0.02f
            );
            Require(
                next >= amount && next - amount <= 0.04001f,
                "Tack transition must be bounded and monotonic."
            );
            amount = next;
        }
        Require(Math.Abs(amount - 1) < 1e-5f, "Penalty should settle in half a second.");
        Require(
            Math.Abs(
                SpritsailObstructionGeometry.Force(obstruction: amount, multiplier: 0.9f) - 0.9f
            ) < 1e-6f,
            "Obstructed tack must lose exactly 10% after settling."
        );
        Require(
            SpritsailObstructionGeometry.Force(obstruction: 0, multiplier: 0.9f) == 1,
            "Clear tack must retain force."
        );
        Require(
            SpritsailObstructionGeometry.Force(obstruction: 1, multiplier: 1) == 1,
            "Configured penalty can be disabled."
        );
        Require(
            SpritsailObstructionGeometry.Force(obstruction: 1, multiplier: 0) == 0,
            "Zero is a valid configured multiplier."
        );
        Require(
            SpritsailObstructionGeometry.ValidMultiplier(value: float.NaN) == 0.9f
                && SpritsailObstructionGeometry.ValidMultiplier(value: 2) == 0.9f,
            "Invalid configuration must use the default."
        );
        var heel = Vector3.zero;
        var tip = new Vector3(2, 0.5f, 2);
        var middle = new Vector3(1, 9, 1);
        Require(
            SpritsailObstructionGeometry.Camber(
                point: middle,
                heel: heel,
                tip: tip,
                width: 4,
                obstruction: 1
            ) == 0.5f,
            "Billow on the projected sprit must be halved."
        );
        Require(
            SpritsailObstructionGeometry.Camber(
                point: middle,
                heel: heel,
                tip: tip,
                width: 4,
                obstruction: 0
            ) == 1,
            "Clear-tack camber must be unchanged."
        );
        Require(
            SpritsailObstructionGeometry.Camber(
                point: middle + new Vector3(2, 0, -2),
                heel: heel,
                tip: tip,
                width: 4,
                obstruction: 1
            ) == 1,
            "Distant fabric must retain its camber."
        );
        Require(
            SpritsailObstructionGeometry.Camber(
                point: middle,
                heel: heel,
                tip: heel,
                width: 4,
                obstruction: 1
            ) == 1,
            "Degenerate sprit must not deform cloth."
        );
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
        foreach (float width in new[] { 2f, 6f, 12f })
        foreach (float unroll in new[] { 0f, 0.2f, 0.6f, 0.98f, 1f })
        {
            var corners = LooseFootedSpritsailGeometry
                .Create(width: width, definition: definition)
                .Corners;
            var pose = SpritsailDeployment.Evaluate(corners: corners, unroll: unroll);
            float deployed = SpritsailDeployment.Amount(unroll: unroll);
            for (int row = 0; row <= 16; row++)
            for (int col = 0; col <= 12; col++)
            {
                float u = col / 12f,
                    v = row / 16f;
                var ordinary = LooseFootedSpritsailGathering.Point(
                    corners: corners,
                    pose: pose,
                    amount: deployed,
                    u: u,
                    v: v
                );
                var restricted = LooseFootedSpritsailGathering.Point(
                    corners: corners,
                    pose: pose,
                    amount: deployed,
                    u: u,
                    v: v,
                    obstruction: 1
                );
                Require(
                    ordinary.x == restricted.x && ordinary.z == restricted.z,
                    "Restriction must preserve gathering and edge positions in the sail plane."
                );
                float camber =
                    LooseFootedSpritsailGeometry.RestCamber(
                        width: corners[3].z - corners[2].z,
                        u: u,
                        v: v,
                        headReach: (corners[1].z - corners[0].z) / (corners[3].z - corners[2].z)
                    ) * deployed;
                Require(
                    ordinary.y - restricted.y >= -1e-6f
                        && ordinary.y - restricted.y <= camber * 0.5f + 1e-6f,
                    "Only up to half the camber may be removed; gathering folds must survive."
                );
                if (unroll == 0 || col == 0 || col == 12)
                    Require(
                        (ordinary - restricted).sqrMagnitude < 1e-10f,
                        "Struck pose and pinned edges must stay unchanged."
                    );
            }
        }
        Console.WriteLine(
            "PASS: spritsail affected tack/reversal, neutral deadband, smooth 10% penalty, local camber reduction and reef folds."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }
}
