using System;
using System.Linq;
using MoreSailwindSails.Utils;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Utils;

// Exercises diagnostic grouping and wire geometry without executing Unity rendering.
internal static class WinchMountOverlayChecks
{
    internal static void Run()
    {
        var hiddenBound = WinchMountOverlayGeometry.Classify(
            owned: false,
            visible: false,
            occupied: true,
            mounted: false
        );
        Require(
            hiddenBound == WinchMountStatus.Occupied,
            "Hidden native bound rope must remain occupied."
        );
        Require(
            WinchMountOverlayGeometry.Classify(
                owned: true,
                visible: false,
                occupied: true,
                mounted: true
            ) == WinchMountStatus.Hidden,
            "Hidden spare custom clones must not block or draw diagnostic locations."
        );
        Require(
            WinchMountOverlayGeometry.Classify(
                owned: true,
                visible: true,
                occupied: false,
                mounted: true
            ) == WinchMountStatus.Occupied,
            "Visible custom controls occupy their displayed location."
        );
        Require(
            WinchMountOverlayGeometry.Classify(
                owned: false,
                visible: false,
                occupied: false,
                mounted: true
            ) == WinchMountStatus.Unused,
            "Hidden unused native control on fitted support must be green."
        );
        Require(
            WinchMountOverlayGeometry.Classify(
                owned: false,
                visible: false,
                occupied: false,
                mounted: false
            ) == WinchMountStatus.Unfitted,
            "Unfitted support must be distinguished from unused fitted support."
        );

        var positions = new[]
        {
            Vector3.zero,
            new Vector3(0.0005f, 0f, 0f),
            new Vector3(0.0012f, 0f, 0f),
            new Vector3(2f, 0f, 0f),
            new Vector3(2f, 0f, 0f),
            new Vector3(4f, 0f, 0f),
            new Vector3(float.NaN, 0f, 0f),
        };
        var states = new[]
        {
            WinchMountStatus.Unfitted,
            WinchMountStatus.Unused,
            hiddenBound,
            WinchMountStatus.Unused,
            WinchMountStatus.Hidden,
            WinchMountStatus.Unfitted,
            WinchMountStatus.Occupied,
        };
        var groups = new int[positions.Length];
        var scratch = new int[positions.Length];
        WinchMountOverlayGeometry.Group(
            positions: positions,
            states: states,
            groups: groups,
            best: scratch
        );
        Require(
            groups.SequenceEqual(new[] { 2, 2, 2, 3, -1, 5, -1 }),
            "Coincident groups must aggregate occupancy and omit hidden/invalid entries."
        );
        // Simulate a native claim disappearing and a support moving between frames.
        states[2] = WinchMountStatus.Unfitted;
        positions[2] = new Vector3(0.003f, 0f, 0f);
        WinchMountOverlayGeometry.Group(
            positions: positions,
            states: states,
            groups: groups,
            best: scratch
        );
        Require(
            groups.SequenceEqual(new[] { 1, 1, 2, 3, -1, 5, -1 }),
            "Movement/occupancy changes must split groups and refresh precedence without stale state."
        );
        // A hidden spare cannot bridge two distinct native locations.
        positions = new[]
        {
            Vector3.zero,
            new Vector3(0.0008f, 0f, 0f),
            new Vector3(0.0016f, 0f, 0f),
        };
        states = new[]
        {
            WinchMountStatus.Unused,
            WinchMountStatus.Hidden,
            WinchMountStatus.Occupied,
        };
        groups = new int[3];
        WinchMountOverlayGeometry.Group(
            positions: positions,
            states: states,
            groups: groups,
            best: new int[3]
        );
        Require(
            groups.SequenceEqual(new[] { 0, -1, 2 }),
            "Hidden spare clone must not merge neighboring locations."
        );
        WinchMountOverlayGeometry.Group(
            positions: Array.Empty<Vector3>(),
            states: Array.Empty<WinchMountStatus>(),
            groups: Array.Empty<int>(),
            best: Array.Empty<int>()
        );

        var vertices = new[]
        {
            Vector3.zero,
            Vector3.right,
            Vector3.up,
            Vector3.right + Vector3.up,
        };
        var edges = WinchMountOverlayGeometry.MeshEdges(
            vertices: vertices,
            triangles: new[] { 0, 1, 2, 2, 1, 3 }
        );
        Require(
            edges.Length == 10,
            "Two triangles sharing an edge must produce five unique lines."
        );
        var bounds = new Bounds(center: new Vector3(2f, 3f, 4f), size: new Vector3(4f, 6f, 8f));
        edges = WinchMountOverlayGeometry.BoundsEdges(bounds: bounds);
        Require(edges.Length == 24, "Unreadable mesh fallback must draw twelve box edges.");
        for (int i = 0; i < edges.Length; i += 2)
        {
            int different = 0;
            for (int axis = 0; axis < 3; axis++)
            {
                Require(
                    edges[i][axis] == bounds.min[axis] || edges[i][axis] == bounds.max[axis],
                    "Box endpoint is not a bounds corner."
                );
                Require(
                    edges[i + 1][axis] == bounds.min[axis]
                        || edges[i + 1][axis] == bounds.max[axis],
                    "Box endpoint is not a bounds corner."
                );
                if (edges[i][axis] != edges[i + 1][axis])
                    different++;
            }
            Require(different == 1, "Box edges must connect adjacent corners, not diagonals.");
        }
        Console.WriteLine(
            "PASS: winch overlay occupancy precedence, hidden clone exclusion, coincident grouping, moving supports, mesh edges and bounds fallback. Camera rendering remains an in-game check."
        );
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
