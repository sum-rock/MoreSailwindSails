using System;
using MoreSailwindSails.Sails.FishermansStaysail;
using MoreSailwindSails.Sails.FishermansStaysail.MkA;
using MoreSailwindSails.Sails.FishermansStaysail.MkB;
using MoreSailwindSails.Sails.FishermansStaysail.MkC;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail;

internal static class BehaviorCases
{
    internal sealed class Mark
    {
        internal readonly string Name;
        internal readonly Func<float, float, FishermansStaysailMeshData> Create;
        internal readonly float HeadAngle;

        internal Mark(
            string name,
            Func<float, float, FishermansStaysailMeshData> create,
            float headAngle
        )
        {
            Name = name;
            Create = create;
            HeadAngle = headAngle;
        }
    }

    private static readonly Mark[] Marks =
    {
        new Mark(
            "Mk.A",
            FishermansStaysailMkAGeometry.Create,
            FishermansStaysailMkAGeometry.FixedUpperHeadAngle
        ),
        new Mark(
            "Mk.B",
            FishermansStaysailMkBGeometry.Create,
            FishermansStaysailMkBGeometry.FixedUpperHeadAngle
        ),
        new Mark(
            "Mk.C",
            FishermansStaysailMkCGeometry.Create,
            FishermansStaysailMkCGeometry.FixedUpperHeadAngle
        ),
    };

    internal static readonly float[] Reefs =
    {
        0,
        0.039f,
        0.04f,
        0.2f,
        0.5f,
        0.75f,
        0.8f,
        0.95f,
        1,
        0.5f,
        0.04f,
        0,
    };

    internal static void ForEach(string scenario, Action<Mark, float, float> check)
    {
        foreach (var mark in Marks)
        {
            Near(mark.HeadAngle, 14, 0, mark.Name + " deployed head policy");
            foreach (float width in new[] { 3f, 6.9f, 13.8f })
            foreach (float slope in new[] { 0f, 8.483f, 20f, 35f, 55f })
                Check(
                    $"{mark.Name}, {scenario}, width {width}, slope {slope}",
                    () => check(mark, width, slope)
                );
            Console.WriteLine($"PASS (executed): {mark.Name} {scenario}.");
        }
    }

    internal static void Check(string scenario, Action check)
    {
        try
        {
            check();
        }
        catch (Exception error)
        {
            throw new Exception(scenario + ": " + error.Message, error);
        }
    }

    internal static void Near(float value, float expected, float tolerance, string name)
    {
        if (!float.IsFinite(value) || Math.Abs(value - expected) > tolerance)
            throw new Exception($"{name}: {value} expected {expected}.");
    }
}
