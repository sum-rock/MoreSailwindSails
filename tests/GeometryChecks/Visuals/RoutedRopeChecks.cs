using System;
using System.Linq;
using MoreSailwindSails.Visuals;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Visuals;

// Verifies rope volume, lighting normals and topology independently of Unity rendering.
internal static class RoutedRopeChecks
{
    internal static void Run()
    {
        foreach (
            var axis in new[] { Vector3.up, Vector3.forward, new Vector3(2, -3, 4).normalized }
        )
        foreach (float diameter in new[] { 0.02f, 0.05f, 0.075f })
            CheckRoute(points: new[] { Vector3.zero, axis * 2, axis * 4 }, diameter: diameter);

        var coil = Enumerable
            .Range(start: 0, count: 97)
            .Select(i => new Vector3(
                x: 0.4f * Mathf.Cos(f: i * 6 * Mathf.PI / 96),
                y: i * 0.003f,
                z: 0.4f * Mathf.Sin(f: i * 6 * Mathf.PI / 96)
            ))
            .ToArray();
        CheckRoute(points: coil, diameter: 0.06f);
        CheckRoute(
            points: new[] { Vector3.zero, Vector3.up * 4, new Vector3(3, 2, 1) },
            diameter: 0.05f
        );

        // Repeated nodes and exact reversals must remain finite, with a round cross-section.
        CheckRoute(
            points: new[] { Vector3.zero, Vector3.zero, Vector3.up, Vector3.up, Vector3.zero },
            diameter: 0.05f,
            checkFaces: false
        );
        foreach (
            var points in new[]
            {
                Array.Empty<Vector3>(),
                new[] { Vector3.zero },
                new[] { Vector3.one, Vector3.one },
                new[] { Vector3.zero, new Vector3(float.NaN, 0, 0) },
                new[] { Vector3.zero, new Vector3(0, float.PositiveInfinity, 0) },
            }
        )
            Require(
                value: !Pose(points: points, diameter: 0.05f),
                message: "Invalid/collapsed route must be hidden."
            );
        foreach (float diameter in new[] { 0, -1, float.NaN, float.PositiveInfinity })
            Require(
                value: !Pose(points: new[] { Vector3.zero, Vector3.one }, diameter: diameter),
                message: "Invalid width must be hidden."
            );
        Console.WriteLine(
            "PASS: round rope diameters, outward triangle winding, unit normals, coils, bends, translation, repeated/reversed nodes and invalid routes; rendering remains an in-game check."
        );
    }

    private static bool Pose(Vector3[] points, float diameter) =>
        RoutedRopeGeometry.Pose(
            points: points,
            diameter: diameter,
            vertices: new Vector3[points.Length * RoutedRopeGeometry.Sides],
            normals: new Vector3[points.Length * RoutedRopeGeometry.Sides]
        );

    private static void CheckRoute(Vector3[] points, float diameter, bool checkFaces = true)
    {
        int sides = RoutedRopeGeometry.Sides;
        var vertices = new Vector3[points.Length * sides];
        var normals = new Vector3[vertices.Length];
        Require(
            value: RoutedRopeGeometry.Pose(
                points: points,
                diameter: diameter,
                vertices: vertices,
                normals: normals
            ),
            message: "Valid route was hidden."
        );
        for (int ring = 0; ring < points.Length; ring++)
        {
            var center = points[ring] - points[0];
            var average = Vector3.zero;
            for (int side = 0; side < sides; side++)
            {
                int index = ring * sides + side;
                var radial = vertices[index] - center;
                Require(
                    value: Math.Abs(radial.magnitude - diameter / 2) < 1e-5f,
                    message: "Rope lost its circular diameter."
                );
                Require(
                    value: Math.Abs(normals[index].magnitude - 1) < 1e-5f
                        && Vector3.Dot(lhs: radial.normalized, rhs: normals[index]) > 0.999f,
                    message: "Rope lighting normal is invalid."
                );
                average += vertices[index] / sides;
            }
            Require(
                value: (average - center).magnitude < 1e-5f,
                message: "Rope moved off its authored route."
            );
        }
        var triangles = RoutedRopeGeometry.Triangles(count: points.Length);
        Require(
            value: triangles.Length == (points.Length - 1) * sides * 6,
            message: "Rope segment topology changed."
        );
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i],
                b = triangles[i + 1],
                c = triangles[i + 2];
            Require(
                value: new[] { a, b, c }.All(index => index >= 0 && index < vertices.Length),
                message: "Rope triangle escaped its vertices."
            );
            if (checkFaces)
                Require(
                    value: Vector3.Dot(
                        lhs: Vector3.Cross(
                            lhs: vertices[b] - vertices[a],
                            rhs: vertices[c] - vertices[a]
                        ),
                        rhs: normals[a] + normals[b] + normals[c]
                    ) > 0,
                    message: "Rope triangle is inside out or collapsed."
                );
        }
        var translated = new Vector3[vertices.Length];
        var shift = new Vector3(20, -10, 30);
        Require(
            value: RoutedRopeGeometry.Pose(
                points: points.Select(p => p + shift).ToArray(),
                diameter: diameter,
                vertices: translated,
                normals: new Vector3[vertices.Length]
            ),
            message: "Translated route was hidden."
        );
        for (int i = 0; i < vertices.Length; i++)
            Require(
                value: (vertices[i] - translated[i]).magnitude < 1e-5f,
                message: "Translation distorted rope geometry."
            );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new Exception(message: message);
    }
}
