// Verifies authored sensor axes and fitted marker placement independently of cloth playback.
using System;
using MoreSailwindSails.Sails.FishermansStaysail;
using MoreSailwindSails.Sails.FishermansStaysail.MkA;
using MoreSailwindSails.Sails.FishermansStaysail.MkB;
using MoreSailwindSails.Sails.FishermansStaysail.MkC;
using UnityEngine;
using static MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail.BehaviorCases;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail;

internal static class WindCenterChecks
{
    internal static void Run()
    {
        var cuts = new (Func<FishermansStaysailMeshData> Bake, Vector3 BlenderCenter)[]
        {
            (FishermansStaysailMkABakedMesh.Create, new Vector3(7.514f, 0, -6.900f)),
            (FishermansStaysailMkBBakedMesh.Create, new Vector3(7.254f, 0, -5.580f)),
            (FishermansStaysailMkCBakedMesh.Create, new Vector3(6.467f, 0, -6.460f)),
        };
        foreach (var cut in cuts)
        {
            var baked = cut.Bake();
            var expected = new Vector3(
                cut.BlenderCenter.z,
                -cut.BlenderCenter.y,
                cut.BlenderCenter.x - 13.8f
            );
            Near((baked.Center * 13.8f - expected).magnitude, 0, 0.00001f, "authored center");
            Near(
                (baked.WindRotation * Vector3.right - Vector3.forward).magnitude,
                0,
                0.00001f,
                "sensor chord"
            );
            Near(
                (baked.WindRotation * Vector3.up + Vector3.up).magnitude,
                0,
                0.00001f,
                "sensor normal"
            );
            Near(
                (baked.WindRotation * Vector3.forward - Vector3.right).magnitude,
                0,
                0.00001f,
                "sensor luff axis"
            );
            float along = baked.Center.z + 1;
            float authoredFoot = Mathf.Lerp(baked.Vertices[800].x, baked.Vertices[824].x, along);
            float fraction =
                (baked.Center.x - authoredFoot) / (baked.Vertices[24].x * along - authoredFoot);
            foreach (float width in new[] { 0.25f, 6.9f, 13.8f, 100f })
            foreach (float slope in new[] { 0f, 20f, 40f, 80f })
            {
                var fitted = FishermansStaysailMeshFitting.Fit(
                    data: cut.Bake(),
                    width: width,
                    headSlope: slope
                );
                float foot = Mathf.Lerp(fitted.Corners[2].x, fitted.Corners[3].x, along);
                float head = Mathf.Lerp(fitted.Corners[0].x, fitted.Corners[1].x, along);
                Near(
                    (fitted.Center.x - foot) / (head - foot),
                    fraction,
                    0.00001f,
                    "fitted marker height fraction"
                );
                Near(fitted.Center.z / width + 1, along, 0.00001f, "fitted marker span fraction");
                if (fitted.WindRotation != baked.WindRotation)
                    throw new Exception("Stay slope changed the authored sensor orientation.");
                if (Math.Abs(slope - 20) < 0.001f)
                    Near(
                        (fitted.Center - baked.Center * width).magnitude,
                        0,
                        0.0001f,
                        "nominal marker"
                    );
            }
        }
        // An authored rotation must survive fitting, not be replaced by default axes.
        var rotated = FishermansStaysailMkABakedMesh.Create();
        var rotation = new Quaternion(0, 0, 0.6f, 0.8f);
        rotated.WindRotation = rotation;
        if (
            FishermansStaysailMeshFitting
                .Fit(data: rotated, width: 13.8f, headSlope: 35)
                .WindRotation != rotation
        )
            throw new Exception("Authored WindCenter rotation was discarded.");
        foreach (float side in new[] { -1f, 1f })
        {
            var normal = Vector3.up;
            var force = FishermansStaysailAerodynamics.ForceDirection(
                wind: normal * side,
                normal: normal
            );
            Near((force - normal * side).magnitude, 0, 0, "force faces apparent wind");
        }
        Console.WriteLine(
            "PASS: staysail authored windcenters, native sensor axes, fitted sizes/slopes and force direction."
        );
    }
}
