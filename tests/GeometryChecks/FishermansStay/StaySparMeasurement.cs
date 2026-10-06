using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

// Reads the installed spar-frame fixtures for shared invariants and boat-specific endpoint checks.
internal sealed class StaySparMeasurement
{
    internal int Id;
    internal int[] Parents;
    internal float[] Matrix;
    internal Vector3 Bottom,
        Top,
        Guide;

    internal Vector3 Point(Vector3 local) =>
        new Vector3(
            Matrix[0] * local.x + Matrix[1] * local.y + Matrix[2] * local.z + Matrix[3],
            Matrix[4] * local.x + Matrix[5] * local.y + Matrix[6] * local.z + Matrix[7],
            Matrix[8] * local.x + Matrix[9] * local.y + Matrix[10] * local.z + Matrix[11]
        );

    internal static Dictionary<string, Dictionary<int, StaySparMeasurement>> Load()
    {
        var measurements = new Dictionary<string, Dictionary<int, StaySparMeasurement>>();
        foreach (
            string line in File.ReadLines(
                Path.Combine(AppContext.BaseDirectory, "FishermansStay", "StayMeasurements.txt")
            )
        )
        {
            if (line.StartsWith("#"))
                continue;
            var values = line.Split('|');
            if (!measurements.TryGetValue(values[0], out var masts))
                measurements.Add(values[0], masts = new Dictionary<int, StaySparMeasurement>());
            masts.Add(
                int.Parse(values[1]),
                new StaySparMeasurement
                {
                    Id = int.Parse(values[1]),
                    Parents = values[2]
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(int.Parse)
                        .ToArray(),
                    Matrix = Floats(values[3]),
                    Bottom = Vector(values[4]),
                    Top = Vector(values[5]),
                    Guide = Vector(values[6]),
                }
            );
        }
        return measurements;
    }

    private static float[] Floats(string value) =>
        value.Split(',').Select(v => float.Parse(v, CultureInfo.InvariantCulture)).ToArray();

    private static Vector3 Vector(string value)
    {
        var components = Floats(value);
        return new Vector3(components[0], components[1], components[2]);
    }
}
