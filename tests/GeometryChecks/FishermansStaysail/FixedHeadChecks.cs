using System;
using MoreSailwindSails.Sails.FishermansStaysail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail;

internal static class FixedHeadChecks
{
    internal static void Run()
    {
        BehaviorCases.ForEach(
            "14-to-zero reef angle, independent sheet sweep and coupled edge budgets",
            (mark, width, slope) =>
            {
                foreach (int side in new[] { -1, 1 })
                foreach (float unroll in BehaviorCases.Reefs)
                    BehaviorCases.Check(
                        $"side {side}, unroll {unroll}",
                        () => CheckReef(mark, width, slope, side, unroll)
                    );
            }
        );
        CheckTackState();
        BehaviorCases.Near(
            FishermansStaysailFixedHead.ReefFraction(0.5f) * 14,
            7,
            0,
            "half reef gives seven degrees"
        );
        BehaviorCases.Near(
            FishermansStaysailFixedHead.LowerAngle(40, 14, 0.5f),
            7,
            0,
            "gathered foot follows reefed head"
        );
        Console.WriteLine(
            "PASS (executed): family tack deadband, wind-strength independence, smooth transitions, mast-frame invariance and half-reef angles."
        );
    }

    private static void CheckReef(
        BehaviorCases.Mark mark,
        float width,
        float slope,
        int side,
        float unroll
    )
    {
        var c = mark.Create(width, slope).Corners;
        float deployment = FishermansStaysailBillow.Deployment(unroll);
        var head = FishermansStaysailFixedHead.Position(
            c[1],
            c[0],
            Vector3.right,
            side,
            mark.HeadAngle,
            unroll
        );
        BehaviorCases.Near(
            FishermansStaysailTravel.SignedAngle(c[1] - c[0], head - c[0], Vector3.right),
            side * 14 * unroll,
            1e-4f,
            "fixed head angle"
        );
        BehaviorCases.Near(head.x, c[1].x, width * 1e-5f, "fixed head height");
        BehaviorCases.Near(
            (head - c[0]).magnitude,
            (c[1] - c[0]).magnitude,
            width * 1e-5f,
            "fixed head span"
        );
        if (unroll == 0)
            BehaviorCases.Near(
                (head - c[1]).magnitude,
                0,
                width * 1e-5f,
                "reef parks upper corner at head"
            );
        var tack = FishermansStaysailReefingGeometry.Pose(c[2], c[0], c[1], unroll);
        var pulley = c[0] + (c[1] - c[0]) * 1.5f;
        float originalDistance = DistanceToStay(head, c[0], pulley);
        for (int sheet = -40; sheet <= 40; sheet += 5)
            BehaviorCases.Check(
                $"sheet {sheet}",
                () =>
                {
                    // The runtime body still rotates under its native hinge. Its
                    // local upper-bone coordinates must cancel that rotation.
                    var localHead = FishermansStaysailFrameGeometry.RotateAroundMast(
                        head,
                        c[0],
                        Vector3.right,
                        -sheet
                    );
                    var worldHead = FishermansStaysailFrameGeometry.RotateAroundMast(
                        localHead,
                        c[0],
                        Vector3.right,
                        sheet
                    );
                    BehaviorCases.Near(
                        (worldHead - head).magnitude,
                        0,
                        width * 1e-5f,
                        "upper corner independent of sheet body"
                    );
                    BehaviorCases.Near(
                        DistanceToStay(worldHead, c[0], pulley),
                        originalDistance,
                        width * 1e-5f,
                        "fixed distance from stay"
                    );
                    var clew = FishermansStaysailReefingGeometry.Pose(c[3], c[0], c[1], unroll);
                    clew = FishermansStaysailFrameGeometry.RotateAroundMast(
                        clew,
                        c[0],
                        Vector3.right,
                        FishermansStaysailFixedHead.LowerAngle(sheet, side * mark.HeadAngle, unroll)
                    );
                    float foot = (clew - tack).magnitude;
                    float leech = (c[1] - c[3]).magnitude * Math.Max(0.015f, unroll);
                    var points = new Vector3[33];
                    if (
                        !FishermansStaysailEdgeFit.Fit(
                            head,
                            clew,
                            tack,
                            Vector3.up,
                            -Vector3.right,
                            width,
                            side,
                            unroll,
                            leech,
                            foot,
                            points
                        )
                    )
                        throw new Exception(
                            $"Fixed-head fit failed at slope {slope}, side {side}, unroll {unroll}, sheet {sheet}."
                        );
                    BehaviorCases.Near(
                        (points[32] - head).magnitude,
                        0,
                        width * 1e-5f,
                        "leech ends at fixed corner"
                    );
                    BehaviorCases.Near(
                        (points[0] - tack).magnitude,
                        foot * (1 - 0.01f * deployment),
                        width * 2e-5f,
                        "foot remains constrained"
                    );
                    float arc = 0;
                    for (int i = 1; i < points.Length; i++)
                        arc += (points[i] - points[i - 1]).magnitude;
                    if (arc > leech * (1 - 0.01f * deployment) + width * 2e-5f)
                        throw new Exception("Fixed upper corner stretched the leech.");
                }
            );
    }

    private static float DistanceToStay(Vector3 point, Vector3 fore, Vector3 aft) =>
        Vector3.Cross(point - fore, (aft - fore).normalized).magnitude;

    private static void CheckTackState()
    {
        var state = new FishermansStaysailFixedHead();
        state.Update(Vector3.zero, Vector3.right, Vector3.forward, 0);
        BehaviorCases.Near(state.Side, 1, 0, "indeterminate initialization defaults positive");
        state = new FishermansStaysailFixedHead();
        state.Update(Vector3.up, Vector3.right, Vector3.forward, 0);
        BehaviorCases.Near(state.Side, -1, 0, "initial wind sets leeward side");
        foreach (float weak in new[] { -0.6f, -0.1f, 0, 0.1f, 0.6f, float.NaN })
        {
            state.Update(Vector3.up * weak, Vector3.right, Vector3.forward, 0.1f);
            BehaviorCases.Near(state.Side, -1, 0, "deadband retains prior tack");
        }
        foreach (float strength in new[] { 1f, 3f, 15f })
        {
            state.Update(Vector3.up * strength, Vector3.right, Vector3.forward, 0.1f);
            BehaviorCases.Near(state.Side, -1, 0, "strength cannot trim upper angle");
        }
        foreach (int fps in new[] { 15, 60, 144 })
        foreach (int desired in new[] { 1, -1, 1, -1 })
        {
            for (int i = 0; i < fps * 4; i++)
            {
                float previous = state.Side;
                state.Update(-Vector3.up * desired * 8, Vector3.right, Vector3.forward, 1f / fps);
                if (Math.Abs(state.Side - previous) > 6.01f / fps || Math.Abs(state.Side) > 1)
                    throw new Exception("Fixed head jumps or overshoots during tacking.");
            }
            BehaviorCases.Near(state.Side, desired, 0, "tack settles at exact fixed angle");
        }
        Vector3 Rotate(Vector3 p) =>
            FishermansStaysailFrameGeometry.RotateAroundMast(
                p,
                Vector3.zero,
                new Vector3(1, 2, 3),
                53
            );
        var moved = new FishermansStaysailFixedHead();
        moved.Update(Rotate(Vector3.up * 8), Rotate(Vector3.right), Rotate(Vector3.forward), 0);
        BehaviorCases.Near(moved.Side, -1, 0, "heel/rake cannot change tack selection");
        var head = new Vector3(2, 0, 6);
        var expected = FishermansStaysailFixedHead.Position(
            head,
            Vector3.zero,
            Vector3.right,
            -1,
            14,
            1
        );
        var shift = new Vector3(12, 3, -5);
        var transformed = FishermansStaysailFixedHead.Position(
            Rotate(head) + shift,
            shift,
            Rotate(Vector3.right),
            -1,
            14,
            1
        );
        BehaviorCases.Near(
            (transformed - (Rotate(expected) + shift)).magnitude,
            0,
            1e-5f,
            "fixed head follows mast frame"
        );
    }
}
