using System;
using MoreSailwindSails.Sails.FishermansStaysail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail;

internal static class SheetChecks
{
    internal static void Run()
    {
        BehaviorCases.ForEach(
            "fixed 14-degree head, coupled sheet budgets and aerodynamic frames",
            (mark, width, slope) =>
            {
                foreach (int side in new[] { -1, 1 })
                    for (int angle = -40; angle <= 40; angle += 5)
                        BehaviorCases.Check(
                            $"side {side}, sheet {angle}",
                            () =>
                            {
                                var c = mark.Create(width, slope).Corners;
                                var head = FishermansStaysailFixedHead.Position(
                                    c[1],
                                    c[0],
                                    Vector3.right,
                                    side,
                                    mark.HeadAngle,
                                    1
                                );
                                BehaviorCases.Near(
                                    FishermansStaysailTravel.SignedAngle(
                                        c[1] - c[0],
                                        head - c[0],
                                        Vector3.right
                                    ),
                                    side * 14,
                                    1e-4f,
                                    "head stays fixed across sheets"
                                );
                                var clew = FishermansStaysailFrameGeometry.RotateAroundMast(
                                    c[3],
                                    c[2],
                                    Vector3.right,
                                    angle
                                );
                                var points = new Vector3[33];
                                float foot = (c[3] - c[2]).magnitude,
                                    leech = (c[1] - c[3]).magnitude;
                                if (
                                    !FishermansStaysailTension.Fit(
                                        clew,
                                        head,
                                        c[2],
                                        new Vector3(0, -width * 0.02f, 0),
                                        leech,
                                        foot,
                                        1,
                                        points
                                    )
                                )
                                    throw new Exception("Staysail tension failed on a legal tack.");
                                BehaviorCases.Near(
                                    (points[0] - c[2]).magnitude,
                                    foot * 0.99f,
                                    width * 1e-4f,
                                    "coupled foot budget"
                                );
                                float length = 0;
                                for (int i = 1; i < points.Length; i++)
                                    length += (points[i] - points[i - 1]).magnitude;
                                if (length > leech * 0.9901f)
                                    throw new Exception("Staysail stretched the leech.");
                                if (
                                    !FishermansStaysailAerodynamics.TryFrame(
                                        c[0],
                                        c[2],
                                        head,
                                        points[0],
                                        out var frame
                                    )
                                )
                                    throw new Exception(
                                        "Staysail lost its aerodynamic frame on tack."
                                    );
                                BehaviorCases.Near(frame.Normal.magnitude, 1, 1e-5f, "wind normal");
                            }
                        );
            }
        );
        // Tilt the entire frame for mast rake and boat heel, and give the
        // reference stay its own pitch. Readout must show actual sheet travel.
        foreach (var axis in new[] { Vector3.right, new Vector3(0.2f, 1, 0.3f).normalized })
        foreach (float pitch in new[] { 8.483f, 20f, 55f })
        foreach (float angle in new[] { -60f, -40f, -15f, 0f, 15f, 40f, 60f })
        {
            var neutral = Vector3.Cross(axis, Vector3.forward).normalized;
            var stay = neutral + axis * (float)Math.Tan(pitch * Math.PI / 180);
            var current = FishermansStaysailFrameGeometry.RotateAroundMast(
                neutral,
                Vector3.zero,
                axis,
                angle
            );
            BehaviorCases.Near(
                FishermansStaysailTravel.SignedAngle(stay, current, axis),
                angle,
                1e-4f,
                "mast-relative angle (unclipped)"
            );
        }
        float fullMin = -85,
            fullMax = 85;
        FishermansStaysailTravel.ConstrainHinge(ref fullMin, ref fullMax, -85, 85);
        if (fullMin != -40 || fullMax != 40)
            throw new Exception("Staysail final hinge exceeds 40 degrees.");
        float crossedMin = 30,
            crossedMax = -20;
        FishermansStaysailTravel.ConstrainHinge(ref crossedMin, ref crossedMax, -40, 40);
        if (crossedMin != crossedMax || crossedMin < -40 || crossedMax > 40)
            throw new Exception("Crossed sheet limits must remain a legal hinge range.");
        float min = -85,
            max = 85;
        FishermansStaysailTravel.ConstrainHinge(ref min, ref max, -25, 30);
        if (min != -25 || max != 30)
            throw new Exception("Staysail lost tighter collision restrictions.");
        Console.WriteLine(
            "PASS (executed): family mast-relative readout and final 40-degree/tighter travel limits."
        );
    }
}
