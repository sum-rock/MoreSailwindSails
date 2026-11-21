using System;
using MoreSailwindSails.Sails.FishermansStaysail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail;

internal static class FixedHeadChecks
{
    internal static void Run()
    {
        CheckTackState();
        Console.WriteLine("PASS: authored staysail tack selection and smoothing.");
    }

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
    }
}
