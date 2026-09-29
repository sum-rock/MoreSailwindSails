using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Stays.FishermansStay;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

// Exercises attachment fitting using synthetic meshes; no proprietary game geometry is stored here.
internal static class CollarChecks
{
    internal static void Run()
    {
        var points = new List<Vector3>();
        var triangles = new List<int>();
        Tube(points, triangles, -10, 0, 0.04f, 0.04f);
        int ropeEnd = points.Count;
        Ring(points, triangles, -10);
        int foreEnd = points.Count;
        Ring(points, triangles, 0);
        int used = points.Count;
        points.Add(new Vector3(900, 800, 700));
        var originalPoints = points.ToArray();
        var originalTriangles = triangles.ToArray();
        foreach (bool reverse in new[] { false, true })
        {
            var input = reverse ? points.AsEnumerable().Reverse().ToArray() : points.ToArray();
            var indices = triangles.Select(i => reverse ? points.Count - 1 - i : i).ToArray();
            var regions = FishermansStayMeshRegions.Partition(vertices: input, triangles: indices);
            for (int i = 0; i < points.Count; i++)
            {
                int original = reverse ? points.Count - 1 - i : i;
                int expected =
                    original < ropeEnd ? 0
                    : original < foreEnd ? 1
                    : original < used ? 2
                    : -1;
                Check(
                    regions[i] == expected,
                    "Combined mesh extraction lost a rope/collar region or included unused vertices."
                );
            }
            // Independent submeshes must retain their original vertex/channel indices and winding.
            var selected = Enumerable
                .Range(0, 3)
                .SelectMany(region =>
                    FishermansStayMeshRegions.Select(
                        triangles: indices,
                        regions: regions,
                        region: region
                    )
                )
                .ToArray();
            Check(
                selected.SequenceEqual(indices),
                "Region selection changed winding or original channel association."
            );
            Check(
                FishermansStayMeshRegions
                    .Select(triangles: Array.Empty<int>(), regions: regions, region: 1)
                    .Length == 0,
                "Empty material submesh rejected."
            );
        }
        Check(
            points.SequenceEqual(originalPoints) && triangles.SequenceEqual(originalTriangles),
            "Donor arrays were modified."
        );
        var ring = points.Skip(ropeEnd).Take(foreEnd - ropeEnd).ToArray();
        foreach (float scale in new[] { 0.5f, 1f, 3f })
        foreach (bool raked in new[] { false, true })
        {
            var shifted = ring.Select(p =>
                    Rotate((p + new Vector3(0, 0, 10)) * scale, raked) + new Vector3(4, -7, 12)
                )
                .ToArray();
            var frame = new FishermansStayCollarFrame(points: shifted);
            var axis = Rotate(Vector3.forward, raked);
            Check(
                Math.Abs(Vector3.Dot(frame.Axis, axis)) > 0.9999f,
                "Collar plane did not follow the raked donor."
            );
            Check(
                (frame.Center - new Vector3(4, -7, 12)).magnitude < 0.0001f,
                "Collar opening is not centered."
            );
            Check(
                Math.Abs(frame.InnerRadius - 0.18f * scale) < 0.0001f,
                "Collar opening changed with its donor pose."
            );
            Check(
                Math.Abs(frame.HalfHeight - 0.04f * scale) < 0.0001f,
                "Collar height changed with its donor pose."
            );
            foreach (var point in shifted)
            {
                var local = frame.Point(point: point);
                var restored =
                    frame.Center
                    + frame.Right * local.x
                    + frame.Up * local.y
                    + frame.Axis * local.z;
                Check(
                    (restored - point).magnitude < 0.0001f,
                    "Canonical collar conversion distorted the native shape."
                );
            }
        }
        var sparVertices = new List<Vector3>();
        var sparTriangles = new List<int>();
        Tube(sparVertices, sparTriangles, 0, 10, 0.3f, 0.1f);
        var surface = new FishermansStaySparSurface(
            vertices: sparVertices.ToArray(),
            triangles: sparTriangles.ToArray(),
            axis: Vector3.zero
        );
        foreach (float requested in new[] { 0f, 4f, 9.95f, 10.067f })
        {
            var center = surface.Seat(
                attachment: new Vector3(0, 0, requested),
                halfHeight: 0.1f,
                radius: out float radius
            );
            Check(
                center.z - 0.1f >= 0.0049f && center.z + 0.1f <= 9.9951f,
                "A masthead collar protrudes beyond the rendered spar."
            );
            float expectedRadius = 0.3f - (center.z - 0.1f) * 0.02f;
            Check(
                Math.Abs(radius - expectedRadius) < 0.0001f,
                "Collar does not surround the tapered rendered spar."
            );
            if (requested == 4)
                Check(
                    Math.Abs(center.z - 4) < 0.0001f,
                    "A mid-mast attachment was unnecessarily shifted."
                );
        }
        CheckContacts();
        var measuredTopmast = FishermansStaySparSurface.Taper(
            bottom: Sanbuq.StayTopmastBottom,
            top: Sanbuq.StayTopmastTop,
            bottomRadius: Sanbuq.StayTopmastBottomRadius,
            topRadius: Sanbuq.StayTopmastTopRadius,
            axis: Vector3.zero
        );
        var topmastSeat = measuredTopmast.Seat(
            attachment: new Vector3(0, 0, 0.38279f),
            halfHeight: 0.11210f,
            radius: out float topmastRadius
        );
        Check(
            Math.Abs(topmastSeat.z - 0.38279f) < 0.00001f
                && Math.Abs(topmastRadius - 0.09925f) < 0.00002f,
            "Unreadable Sanbuq topmast no longer matches its installed rendered-spar measurement."
        );
        Check(
            new[] { Sanbuq.Definition, LargeDhow.Definition }.Sum(b =>
                b.Stays.Sum(g => g.Variants.Count)
            ) == 40,
            "Affected collar variant coverage changed."
        );
        Reject(() =>
            FishermansStayMeshRegions.Partition(
                vertices: originalPoints,
                triangles: new[] { 0, 1, 99999 }
            )
        );
        Reject(() =>
            FishermansStayMeshRegions.Partition(vertices: originalPoints, triangles: new[] { 0, 1 })
        );
        Reject(() =>
            FishermansStayMeshRegions.Partition(
                vertices: originalPoints,
                triangles: originalTriangles.Take(ropeEnd).ToArray()
            )
        );
        var invalid = (Vector3[])originalPoints.Clone();
        invalid[0] = new Vector3(float.NaN, 0, 0);
        Reject(() =>
            FishermansStayMeshRegions.Partition(vertices: invalid, triangles: originalTriangles)
        );
        Reject(() =>
            new FishermansStayCollarFrame(points: Enumerable.Repeat(Vector3.zero, 6).ToArray())
        );
        Reject(() => surface.Seat(attachment: Vector3.zero, halfHeight: 6, radius: out _));
        Reject(() => surface.Seat(attachment: Vector3.zero, halfHeight: float.NaN, radius: out _));
        Console.WriteLine(
            "PASS: separate/combined native collar geometry, seam welding, channel indices, raked frames, rendered masthead seating, taper fit and malformed donor rejection."
        );
    }

    private static void CheckContacts()
    {
        // A deliberately asymmetric collar surface makes each rope exit unambiguous.
        var points = new[]
        {
            new Vector3(0.2f, 0, 0.04f),
            new Vector3(-0.2f, 0, 0.03f),
            new Vector3(0, 0.2f, -0.04f),
            new Vector3(0, -0.2f, 0.02f),
        };
        var pose = new Matrix4x4
        {
            m00 = 0.8f,
            m02 = 0.6f,
            m03 = 2,
            m11 = 1,
            m13 = 3,
            m20 = -0.6f,
            m22 = 0.8f,
            m23 = 7,
            m33 = 1,
        };
        var toward = pose.MultiplyPoint3x4(new Vector3(5, 1, 1));
        var contact = FishermansStayCollarFrame.Contact(
            points: points,
            localToOwner: pose,
            toward: toward
        );
        Check(
            (contact - pose.MultiplyPoint3x4(points[0])).magnitude < 0.00001f,
            "Rope must meet the outward collar surface, not its empty center."
        );
        var toWalk = new Matrix4x4
        {
            m02 = 1,
            m03 = 400,
            m11 = 1,
            m13 = -30,
            m20 = -1,
            m23 = 80,
            m33 = 1,
        };
        var walking = FishermansStayCollarFrame.Contact(
            points: points,
            localToOwner: toWalk * pose,
            toward: toWalk.MultiplyPoint3x4(toward)
        );
        Check(
            (walking - toWalk.MultiplyPoint3x4(contact)).magnitude < 0.0001f,
            "The displaced walking model has a different collar contact."
        );
        var opposite = contact + new Vector3(2, 5, 8);
        float span = (opposite - contact).magnitude;
        FishermansStayGeometry.FitAxis(
            min: -15.75f,
            max: 0.4074f,
            span: span,
            scale: out float scale,
            offset: out float offset
        );
        var direction = (opposite - contact) / span;
        Check(
            (opposite + direction * (-15.75f * scale + offset) - contact).magnitude < 0.0001f
                && (opposite + direction * (0.4074f * scale + offset) - opposite).magnitude
                    < 0.0001f,
            "Fitted rope left a gap at a seated collar."
        );
    }

    private static Vector3 Rotate(Vector3 p, bool raked) =>
        raked ? new Vector3(p.x * 0.8f + p.z * 0.6f, p.y, -p.x * 0.6f + p.z * 0.8f) : p;

    private static void Triangle(
        List<Vector3> points,
        List<int> triangles,
        Vector3 a,
        Vector3 b,
        Vector3 c
    )
    {
        // Deliberately split every triangle's vertices, like the native hard-normal seams.
        foreach (var point in new[] { a, b, c })
        {
            triangles.Add(points.Count);
            points.Add(point);
        }
    }

    private static Vector3 Polar(int angle, float radius, float z)
    {
        double radians = angle * Math.PI / 4;
        return new Vector3((float)Math.Cos(radians) * radius, (float)Math.Sin(radians) * radius, z);
    }

    private static void Tube(
        List<Vector3> points,
        List<int> triangles,
        float low,
        float high,
        float bottomRadius,
        float topRadius
    )
    {
        for (int i = 0; i < 8; i++)
        {
            var a = Polar(i, bottomRadius, low);
            var b = Polar(i + 1, bottomRadius, low);
            var c = Polar(i, topRadius, high);
            var d = Polar(i + 1, topRadius, high);
            Triangle(points, triangles, a, b, c);
            Triangle(points, triangles, b, d, c);
        }
    }

    private static void Ring(List<Vector3> points, List<int> triangles, float height)
    {
        for (int i = 0; i < 8; i++)
        for (int side = 0; side < 4; side++)
        {
            Vector3 Point(int angle, int corner) =>
                Polar(
                    angle,
                    corner % 4 < 2 ? 0.18f : 0.22f,
                    height + (corner % 4 == 0 || corner % 4 == 3 ? -0.04f : 0.04f)
                );
            var a = Point(i, side);
            var b = Point(i + 1, side);
            var c = Point(i, side + 1);
            var d = Point(i + 1, side + 1);
            Triangle(points, triangles, a, b, c);
            Triangle(points, triangles, b, d, c);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    private static void Reject(Action action)
    {
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            return;
        }
        throw new Exception("Malformed attachment geometry was accepted.");
    }
}
