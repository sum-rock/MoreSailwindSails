using System;
using MoreSailwindSails.Sails.Spritsail;
using MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Checks fixed sockets, reversible rigid-sprit motion, loose edge budgets and visible gathered fabric.
internal static class DeploymentChecks
{
    internal static void Run()
    {
        foreach (float width in new[] { 0.25f, 1f, 4f, 12f })
        foreach (
            var scale in new[] { Vector3.one, new Vector3(0.5f, 1.2f, 1.4f), new Vector3(2, 2, 2) }
        )
        foreach (float height in new[] { 0f, 3f, 10f })
        {
            var rest = LooseFootedSpritsailGeometry.Create(width: width).Corners;
            var corners = Array.ConvertAll(
                rest,
                p => Vector3.Scale(p, scale) + Vector3.right * height
            );
            var full = SpritsailDeployment.Evaluate(corners: corners, unroll: 1);
            float length = (full.Tip - full.Heel).magnitude;
            float epsilon = length * 0.00002f;
            var struck = SpritsailDeployment.Evaluate(corners: corners, unroll: 0);
            Require(
                value: (struck.Tip - struck.Heel - Vector3.right * length).magnitude < epsilon,
                message: "Struck sprit must stand parallel to the mast."
            );
            Require(
                value: SpritsailDeployment.ExposedArea(pose: struck) < epsilon,
                message: "Visible bundle must have zero exposed area."
            );
            float previousArea = 0;
            for (int frame = 0; frame <= 100; frame++)
            {
                float unroll = frame / 100f;
                float amount = SpritsailDeployment.Amount(unroll: unroll);
                var pose = SpritsailDeployment.Evaluate(corners: corners, unroll: unroll);
                Require(
                    value: (pose.Heel - full.Heel).magnitude < epsilon
                        && (pose.Throat - full.Throat).magnitude < epsilon,
                    message: "Socket and throat must remain fixed."
                );
                Require(
                    value: (pose.Peak - pose.Clew).magnitude
                        <= (corners[1] - corners[3]).magnitude + epsilon
                        && (pose.Clew - pose.Tack).magnitude
                            <= (corners[3] - corners[2]).magnitude + epsilon,
                    message: "Gathering must release edge tension without stretching the foot or leech."
                );
                Require(
                    value: pose.Tack.x <= full.Heel.x + epsilon
                        && pose.Tack.x >= corners[2].x - epsilon,
                    message: "Tack must gather upward toward socket."
                );
                float area = SpritsailDeployment.ExposedArea(pose: pose);
                Require(
                    value: area + epsilon >= previousArea,
                    message: "Reefing must reduce exposed area monotonically."
                );
                previousArea = area;
                foreach (float angle in new[] { -89f, 0f, 89f })
                {
                    var heel = LooseFootedSpritsailFrameGeometry.RotateAroundMast(
                        point: pose.Heel,
                        pivot: corners[0],
                        axis: Vector3.right,
                        degrees: angle
                    );
                    var tip = LooseFootedSpritsailFrameGeometry.RotateAroundMast(
                        point: pose.Tip,
                        pivot: corners[0],
                        axis: Vector3.right,
                        degrees: angle
                    );
                    Require(
                        value: Math.Abs(
                            (heel - corners[0]).magnitude - (full.Heel - corners[0]).magnitude
                        ) < epsilon
                            && Math.Abs((tip - heel).magnitude - length) < epsilon,
                        message: "Tacking must preserve the bolt orbit radius and rigid sprit length."
                    );
                    // A raked mast and translated boat must preserve these same invariants.
                    var rakedHeel = LooseFootedSpritsailFrameGeometry.RotateAroundMast(
                        point: heel,
                        pivot: Vector3.zero,
                        axis: Vector3.forward,
                        degrees: 18
                    );
                    var rakedTip = LooseFootedSpritsailFrameGeometry.RotateAroundMast(
                        point: tip,
                        pivot: Vector3.zero,
                        axis: Vector3.forward,
                        degrees: 18
                    );
                    Require(
                        value: Math.Abs((rakedTip - rakedHeel).magnitude - length) < epsilon,
                        message: "Mast rake must preserve the rigid sprit."
                    );
                }
                for (int row = 0; row <= 32; row++)
                for (int col = 0; col <= 12; col++)
                {
                    var point = LooseFootedSpritsailGathering.Point(
                        corners: corners,
                        pose: pose,
                        amount: amount,
                        u: col / 12f,
                        v: row / 32f
                    );
                    Require(
                        value: SpritsailDeployment.Finite(value: point.x)
                            && SpritsailDeployment.Finite(value: point.y)
                            && SpritsailDeployment.Finite(value: point.z),
                        message: "Gathered skin must remain finite."
                    );
                    if (col == 0 && row <= 24)
                        Require(
                            value: (
                                point - Vector3.Lerp(corners[0], corners[2], row / 32f)
                            ).magnitude < epsilon,
                            message: "Upper luff must retain its fixed mast attachments."
                        );
                }
            }
            for (int row = 0; row <= 32; row++)
            for (int col = 0; col <= 12; col++)
                foreach (float camber in new[] { -1f, 1f })
                {
                    float u = col / 12f,
                        v = row / 32f;
                    var expected =
                        Vector3.Lerp(
                            Vector3.Lerp(corners[0], corners[2], v),
                            Vector3.Lerp(corners[1], corners[3], v),
                            u
                        )
                        + Vector3.up
                            * LooseFootedSpritsailGeometry.RestCamber(width: width, u: u, v: v)
                            * scale.y
                            * camber;
                    var point = LooseFootedSpritsailGathering.Point(
                        corners: corners,
                        pose: full,
                        amount: 1,
                        u: u,
                        v: v,
                        camber: camber,
                        normalScale: scale.y / scale.z
                    );
                    Require(
                        value: (point - expected).magnitude < epsilon,
                        message: "Entering the Cloth state must retain the cambered skin on either tack."
                    );
                }
            var lowerFold = LooseFootedSpritsailGathering.Point(
                corners: corners,
                pose: struck,
                amount: 0,
                u: 0,
                v: 25f / 32
            );
            Require(
                value: Math.Abs(lowerFold.y) > epsilon,
                message: "Lower luff skin bones must visibly bunch rather than form a taut line."
            );
            var deployedPurchase = SpritsailDeployment.PurchasePoint(
                heel: full.Heel,
                tip: full.Tip
            );
            var struckPurchase = SpritsailDeployment.PurchasePoint(
                heel: struck.Heel,
                tip: struck.Tip
            );
            var guide = struck.Tip + Vector3.right * width;
            Require(
                value: (guide - struckPurchase).magnitude < (guide - deployedPurchase).magnitude,
                message: "Pulling the upper purchase must raise the sprit toward the guide."
            );
            var folded = LooseFootedSpritsailGathering.Point(
                corners: corners,
                pose: struck,
                amount: 0,
                u: 0.25f,
                v: 0.5f
            );
            Require(
                value: Math.Abs(folded.z - struck.Heel.z) > epsilon,
                message: "Struck panel must retain visible bundle thickness."
            );
        }
        Console.WriteLine(
            "PASS: fixed socket/throat, upright struck sprit, upward gathering, edge budgets, exposed area, mast rake and both tacks across scaling and installation heights."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new Exception(message: message);
    }
}
