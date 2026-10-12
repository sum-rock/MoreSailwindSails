using System;
using MoreSailwindSails.Sails.FishermansStaysail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail;

internal static class ReefingChecks
{
    internal static void Run()
    {
        BehaviorCases.ForEach(
            "native reef-channel normalization, upward gathering, halyard reversals and partial-reef edge budgets",
            (mark, width, slope) =>
            {
                foreach (float t in BehaviorCases.Reefs)
                    BehaviorCases.Check($"unroll {t}", () => CheckReef(mark, width, slope, t));
            }
        );
        if (
            FishermansStaysailGeometry.RenderState(0) != 0
            || FishermansStaysailGeometry.RenderState(0.039f) != 0
            || FishermansStaysailGeometry.RenderState(0.04f) != 1
            || FishermansStaysailGeometry.RenderState(0.5f) != 1
            || FishermansStaysailGeometry.RenderState(1) != 2
        )
            throw new Exception("Native furled threshold or renderer selection changed.");
        Console.WriteLine(
            "PASS (executed): family visible-bundle threshold and renderer selection."
        );
    }

    private static void CheckReef(BehaviorCases.Mark mark, float width, float slope, float t)
    {
        // Exercise normalized motion independently of the donor's animation curve.
        var open = new Vector3(2, 5, -3);
        var closed = new Vector3(-1, 2, 4);
        var corners = mark.Create(width, slope).Corners;
        foreach (var corner in corners)
            BehaviorCases.Near(corner.y, 0, 0, "reef corners have no spread to scale");
        float amount = FishermansStaysailReefingGeometry.Progress(
            Vector3.Lerp(closed, open, t),
            closed,
            open
        );
        BehaviorCases.Near(amount, t, 1e-5f, "native channel normalization/reversal");
        foreach (float along in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
        {
            var head = Vector3.Lerp(corners[0], corners[1], along);
            var foot = Vector3.Lerp(corners[2], corners[3], along);
            var posedHead = FishermansStaysailReefingGeometry.Pose(
                head,
                corners[0],
                corners[1],
                amount
            );
            var posedFoot = FishermansStaysailReefingGeometry.Pose(
                foot,
                corners[0],
                corners[1],
                amount
            );
            BehaviorCases.Near((posedHead - head).magnitude, 0, 1e-5f, "reef head stays in place");
            BehaviorCases.Near(posedFoot.z, foot.z, 1e-5f, "reef retains head span");
            BehaviorCases.Near(posedFoot.y, 0, 1e-6f, "reef retains mast plane");
            if (posedFoot.x < foot.x - 1e-5f || posedFoot.x > head.x)
                throw new Exception("Reefed foot must rise toward its head.");
            if (t == 1)
                BehaviorCases.Near(
                    (posedFoot - foot).magnitude,
                    0,
                    1e-5f,
                    "full deployment unchanged"
                );
            if (t == 0)
                BehaviorCases.Near(
                    (head - posedFoot).magnitude,
                    (head - foot).magnitude * 0.015f,
                    1e-5f,
                    "gathers to narrow head strip"
                );
        }
        foreach (int side in new[] { -1, 1 })
            for (int angle = -40; angle <= 40; angle += 5)
                BehaviorCases.Check(
                    $"side {side}, sheet {angle}",
                    () =>
                    {
                        float deployment = FishermansStaysailBillow.Deployment(t);
                        var tack = FishermansStaysailReefingGeometry.Pose(
                            corners[2],
                            corners[0],
                            corners[1],
                            amount
                        );
                        var clew = FishermansStaysailReefingGeometry.Pose(
                            corners[3],
                            corners[0],
                            corners[1],
                            amount
                        );
                        clew = FishermansStaysailFrameGeometry.RotateAroundMast(
                            clew,
                            corners[0],
                            Vector3.right,
                            FishermansStaysailFixedHead.LowerAngle(angle, side * mark.HeadAngle, t)
                        );
                        var head = FishermansStaysailFixedHead.Position(
                            corners[1],
                            corners[0],
                            Vector3.right,
                            side,
                            mark.HeadAngle,
                            t
                        );
                        var points = new Vector3[33];
                        if (
                            !FishermansStaysailTension.Fit(
                                clew,
                                head,
                                tack,
                                Vector3.zero,
                                (corners[1] - corners[3]).magnitude * Math.Max(0.015f, amount),
                                (clew - tack).magnitude,
                                deployment,
                                points
                            )
                        )
                            throw new Exception(
                                $"Reef edge budgets fail at {slope} slope, {t} deployment, {angle} sheet."
                            );
                    }
                );
    }
}
