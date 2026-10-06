using System;
using MoreSailwindSails.Sails.FishermansStaysail;
using MoreSailwindSails.Sails.FishermansStaysail.MkA;
using MoreSailwindSails.Sails.FishermansStaysail.MkB;
using MoreSailwindSails.Sails.FishermansStaysail.MkC;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail;

internal static class CollisionChecks
{
    internal static void Run()
    {
        var marks = new (string Name, Func<float, float, FishermansStaysailMeshData> Create)[]
        {
            ("Mk.A", FishermansStaysailMkAGeometry.Create),
            ("Mk.B", FishermansStaysailMkBGeometry.Create),
            ("Mk.C", FishermansStaysailMkCGeometry.Create),
        };
        foreach (var mark in marks)
        foreach (float width in new[] { 0.25f, 3f, 13.8f, 100f })
        foreach (float slope in new[] { 0f, 8.483f, 20f, 55f, 80f })
        foreach (
            float clearance in new[]
            {
                0f,
                width * 0.1f,
                width * 0.95f / FishermansStaysailGeometry.Columns - 0.0001f,
                width,
                width * 1.1f,
            }
        )
        {
            var corners = mark.Create(width, slope).Corners;
            int enabled = 0;
            for (int column = 0; column < FishermansStaysailGeometry.Columns; column++)
            {
                bool active = FishermansStaysailInstallationGeometry.CollisionStrip(
                    corners,
                    column,
                    clearance,
                    out var center,
                    out var size
                );
                if (
                    !float.IsFinite(center.sqrMagnitude)
                    || !float.IsFinite(size.sqrMagnitude)
                    || size.x <= 0
                    || size.y <= 0
                    || size.z <= 0
                )
                    throw new Exception(
                        mark.Name + " collision strip must stay finite and positive."
                    );
                if (!active)
                    continue;
                enabled++;
                float tolerance = width * 1e-5f;
                if (
                    center.z - size.z * 0.5f < -width + clearance - tolerance
                    || center.z + size.z * 0.5f > tolerance
                )
                    throw new Exception(
                        mark.Name + " collision strip exceeds mast/span clearance."
                    );
                // Check the entire rectangle against the cut, including both ends.
                for (int sample = 0; sample <= 4; sample++)
                {
                    float z = center.z + size.z * (sample / 4f - 0.5f);
                    float u = (z + width) / width;
                    float head = Vector3.Lerp(corners[0], corners[1], u).x;
                    float foot = Vector3.Lerp(corners[2], corners[3], u).x;
                    if (
                        center.x + size.x * 0.5f > head - 0.05f + tolerance
                        || center.x - size.x * 0.5f < foot + 0.05f - tolerance
                    )
                        throw new Exception(
                            $"{mark.Name} collision strip leaves its cut: width {width}, slope {slope}, column {column}."
                        );
                }
            }
            if (clearance >= width && enabled != 0)
                throw new Exception("Fully clipped collision strips must be disabled.");
            if (clearance < width && enabled == 0)
                throw new Exception(mark.Name + " lost all usable collision strips.");
        }
        var clipped = FishermansStaysailMkCGeometry.Create(3).Corners;
        float lastEnd =
            3f * (FishermansStaysailGeometry.Columns - 0.05f) / FishermansStaysailGeometry.Columns;
        if (
            FishermansStaysailInstallationGeometry.CollisionStrip(
                clipped,
                FishermansStaysailGeometry.Columns - 1,
                lastEnd - 0.0001f,
                out _,
                out _
            )
        )
            throw new Exception("A clipped sliver must not be expanded beyond the mast clearance.");
        Console.WriteLine(
            "PASS (executed): all three cuts retain collision strips inside both edges and mast clearance; shallow/clipped strips are disabled."
        );
    }
}
