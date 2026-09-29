using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Sails.FishermansStaysail;
using MoreSailwindSails.Sails.FishermansStaysail.MkA;
using MoreSailwindSails.Stays.FishermansStay;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail.MkA;

internal static class CutChecks
{
    internal static void Run()
    {
        foreach (float width in new[] { 0.25f, 3f, 13.8f, 40f })
        {
            var mesh = FishermansStaysailMkAGeometry.Create(width);
            var c = mesh.Corners;
            Near(Angle(c[1] - c[0], c[2] - c[0]), 110, 0.001f, "head angle");
            Near(Angle(c[0] - c[2], c[3] - c[2]), 110, 0.001f, "tack angle");
            Near((c[1] - c[3]).magnitude / width, 1.7279405f, 0.0001f, "leech proportion");
            CheckMesh(mesh, width);
            if (
                ReferenceEquals(mesh.Vertices, FishermansStaysailMkAGeometry.Create(width).Vertices)
            )
                throw new Exception("Mk.A cuts share mutable arrays.");
            var points = new Vector3[33];
            if (
                !FishermansStaysailTension.Fit(
                    c[3],
                    c[1],
                    c[2],
                    Vector3.zero,
                    (c[1] - c[3]).magnitude,
                    (c[3] - c[2]).magnitude,
                    1,
                    points
                )
            )
                throw new Exception("Mk.A nominal tension cannot fit.");
        }
        foreach (float bad in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, 101f })
        {
            bool rejected = false;
            try
            {
                FishermansStaysailMkAGeometry.Create(bad);
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            if (!rejected)
                throw new Exception("Mk.A accepted invalid width.");
        }
        CheckProfiles();
        if (
            FishermansStayOrderText.NeedsWrapping(
                "Fisherman's Staysail Mk.A " + new string('x', 70)
            )
        )
            throw new Exception("Stay part text intercepted Mk.A.");
        if (
            !FishermansStaysailOrderText.NeedsWrapping(
                "Fisherman's Staysail Mk.A " + new string('x', 70)
            )
        )
            throw new Exception("Mk.A order text is unprotected.");
        Console.WriteLine(
            "PASS: Mk.A 110-degree cut, pinned straight luff, free stay-independent head, skinning, tension and authored-stay alignment."
        );
    }

    private static void CheckMesh(FishermansStaysailMeshData mesh, float width)
    {
        int pinned = 0;
        for (int row = 0; row <= FishermansStaysailGeometry.Rows; row++)
        for (int col = 0; col <= FishermansStaysailGeometry.Columns; col++)
        {
            int i = row * (FishermansStaysailGeometry.Columns + 1) + col;
            bool expected =
                col == 0
                || (
                    col == FishermansStaysailGeometry.Columns
                    && (row == 0 || row == FishermansStaysailGeometry.Rows)
                );
            if ((mesh.Constraints[i].maxDistance == 0) != expected)
                throw new Exception("Wrong Mk.A pin mask.");
            if (expected)
                pinned++;
            if (col == 0)
            {
                Near(mesh.Vertices[i].y, 0, 1e-6f, "straight luff");
                Near(mesh.Vertices[i].z, -width, 1e-6f, "mast luff");
            }
            var w = mesh.Weights[i];
            Near(w.weight0 + w.weight1 + w.weight2 + w.weight3, 1, 1e-6f, "skin sum");
            var rest =
                mesh.BonePositions[w.boneIndex0] * w.weight0
                + mesh.BonePositions[w.boneIndex1] * w.weight1;
            Near((rest - mesh.Vertices[i]).magnitude, 0, width * 1e-5f, "skin reconstruction");
        }
        if (pinned != FishermansStaysailGeometry.Rows + 3)
            throw new Exception("Wrong luff pin count.");
        for (int i = 0; i < mesh.Triangles.Length; i += 3)
        {
            var a = mesh.Vertices[mesh.Triangles[i]];
            var b = mesh.Vertices[mesh.Triangles[i + 1]];
            var c = mesh.Vertices[mesh.Triangles[i + 2]];
            if (Vector3.Cross(b - a, c - a).y <= 0)
                throw new Exception("Inverted Mk.A cut.");
        }
    }

    private sealed class Spar
    {
        internal float[] Matrix;
        internal int[] Parents;
        internal Vector3 Bottom,
            Top;

        internal Vector3 Point(Vector3 p) =>
            new Vector3(
                Matrix[0] * p.x + Matrix[1] * p.y + Matrix[2] * p.z + Matrix[3],
                Matrix[4] * p.x + Matrix[5] * p.y + Matrix[6] * p.z + Matrix[7],
                Matrix[8] * p.x + Matrix[9] * p.y + Matrix[10] * p.z + Matrix[11]
            );
    }

    private static void CheckProfiles()
    {
        var measurements = new Dictionary<string, Dictionary<int, Spar>>();
        foreach (
            var line in File.ReadLines(
                Path.Combine(AppContext.BaseDirectory, "FishermansStay", "StayMeasurements.txt")
            )
        )
        {
            if (line.StartsWith("#"))
                continue;
            var v = line.Split('|');
            float[] Numbers(string s) =>
                s.Split(',').Select(n => float.Parse(n, CultureInfo.InvariantCulture)).ToArray();
            Vector3 Vector(string s)
            {
                var n = Numbers(s);
                return new Vector3(n[0], n[1], n[2]);
            }
            if (!measurements.TryGetValue(v[0], out var boat))
                measurements[v[0]] = boat = new Dictionary<int, Spar>();
            boat.Add(
                int.Parse(v[1]),
                new Spar
                {
                    Matrix = Numbers(v[3]),
                    Bottom = Vector(v[4]),
                    Top = Vector(v[5]),
                    Parents =
                        v[2].Length == 0
                            ? Array.Empty<int>()
                            : v[2].Split(',').Select(int.Parse).ToArray(),
                }
            );
        }
        int count = 0;
        foreach (var boat in BoatRigCatalog.All)
        foreach (var stay in boat.Stays.SelectMany(g => g.Variants))
        {
            var masts = measurements[boat.BoatName];
            var fore = masts[stay.Fore];
            var aft = masts[stay.Aft];
            int baseId = stay.Fore;
            while (masts[baseId].Parents.Length != 0)
                baseId = masts[baseId].Parents[0];
            if (boat.Base(stay.Fore) != baseId)
                throw new Exception("Wrong authored Mk.A fore-mast base.");
            int aftBase = stay.Aft;
            while (masts[aftBase].Parents.Length != 0)
                aftBase = masts[aftBase].Parents[0];
            if (boat.Base(stay.Aft) != aftBase)
                throw new Exception("Wrong authored Mk.A aft halyard source.");
            var chain = boat.Sections(stay.Aft);
            if (
                chain[0] != stay.Aft
                || chain[chain.Length - 1] != aftBase
                || chain.Distinct().Count() != chain.Length
            )
                throw new Exception("Invalid authored aft support chain.");
            var direction = aft.Point(stay.AftPoint) - fore.Point(stay.ForePoint);
            var axis = (fore.Top - fore.Bottom).normalized;
            float slope = FishermansStaysailInstallationGeometry.HeadSlope(direction, axis);
            var mesh = FishermansStaysailMkAGeometry.Create(5, slope);
            var transverse = Vector3.ProjectOnPlane(direction, axis).normalized;
            var head = mesh.Corners[1] - mesh.Corners[0];
            var physical = axis * head.x + transverse * head.z;
            Near(
                Vector3.Cross(physical.normalized, direction.normalized).magnitude,
                0,
                1e-5f,
                "actual stay alignment"
            );
            CheckMesh(mesh, 5);
            var span = Vector3.ProjectOnPlane(direction, axis).magnitude;
            var room = Vector3.Dot(fore.Point(stay.ForePoint) - fore.Bottom, axis) - 0.15f;
            float width = Math.Min(span - 0.2f, room - 0.05f);
            if (
                FishermansStaysailInstallationGeometry.FitError(width, width, 0, room, span) != null
            )
                throw new Exception(
                    "No mathematical Mk.A fit on " + boat.BoatName + "/" + stay.MountIndex
                );
            if (
                FishermansStaysailInstallationGeometry.FitError(span + 1, 1, 0, room, span) == null
                || FishermansStaysailInstallationGeometry.FitError(1, room + 1, 0, room, span)
                    == null
            )
                throw new Exception("Mk.A fit limits ignored.");
            count++;
        }
        if (count != 93)
            throw new Exception("Missing authored staysail configurations.");
        Console.WriteLine(
            "PASS: Mk.A alignment and fore/aft-mast base references for all 93 supported stays."
        );
    }

    private static float Angle(Vector3 a, Vector3 b) =>
        (float)(
            Math.Acos(Math.Max(-1, Math.Min(1, Vector3.Dot(a.normalized, b.normalized))))
            * 180
            / Math.PI
        );

    internal static void Near(float value, float expected, float tolerance, string name)
    {
        if (float.IsNaN(value) || Math.Abs(value - expected) > tolerance)
            throw new Exception(name + ": " + value + " expected " + expected);
    }
}
