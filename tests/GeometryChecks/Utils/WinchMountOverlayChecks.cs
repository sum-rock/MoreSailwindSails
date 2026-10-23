using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Utils;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Utils;

// Exercises diagnostic grouping and wire geometry without executing Unity rendering.
internal static class WinchMountOverlayChecks
{
    internal static void Run()
    {
        CheckModelPlacement();
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
            "PASS: winch overlay model scale/placement, duplicate suppression, occupancy precedence, hidden clone exclusion, coincident grouping, moving supports, mesh edges and bounds fallback. Camera rendering remains an in-game check."
        );
    }

    private static void CheckModelPlacement()
    {
        // Mount rotated 90 degrees about Y and translated; explicit matrices avoid native TRS.
        var mount = new Matrix4x4
        {
            m02 = 1f,
            m03 = 10f,
            m11 = 1f,
            m13 = 20f,
            m20 = -1f,
            m23 = 30f,
            m33 = 1f,
        };
        var child = Matrix4x4.identity;
        child.m03 = 1f;
        child.m13 = 2f;
        child.m23 = 3f;
        var placed = WinchMountOverlayGeometry.PreviewMatrix(
            mount: mount,
            scale: new Vector3(2f, 3f, 4f),
            meshToControl: child
        );
        Require(
            (
                placed.MultiplyPoint3x4(new Vector3(2f, 1f, -1f)) - new Vector3(18f, 29f, 24f)
            ).sqrMagnitude < 0.000001f,
            "Fallback model must retain child offsets, native nonuniform scale and mounting rotation."
        );
        var root = WinchMountOverlayGeometry.PreviewMatrix(
            mount: mount,
            scale: new Vector3(2f, 3f, 4f),
            meshToControl: Matrix4x4.identity
        );
        Require(
            (root.MultiplyPoint3x4(Vector3.zero) - new Vector3(10f, 20f, 30f)).sqrMagnitude
                < 0.000001f,
            "Donor scale must not scale the fallback's world position."
        );
        mount.m03 += 7f;
        var moved = WinchMountOverlayGeometry.PreviewMatrix(
            mount: mount,
            scale: new Vector3(2f, 3f, 4f),
            meshToControl: child
        );
        Require(
            (
                moved.MultiplyPoint3x4(Vector3.zero)
                - placed.MultiplyPoint3x4(Vector3.zero)
                - new Vector3(7f, 0f, 0f)
            ).sqrMagnitude < 0.000001f,
            "Preview must follow boat movement without scaling its displacement."
        );
        var visible = new List<Vector3> { new Vector3(1f, 2f, 3f) };
        Require(
            WinchMountOverlayGeometry.ContainsPosition(
                positions: visible,
                position: new Vector3(1.0005f, 2f, 3f)
            ),
            "A visible real winch or earlier preview must suppress a coincident model."
        );
        Require(
            !WinchMountOverlayGeometry.ContainsPosition(
                positions: visible,
                position: new Vector3(1.002f, 2f, 3f)
            ),
            "Distinct preview positions must not be suppressed."
        );
        visible.Clear();
        Require(
            !WinchMountOverlayGeometry.ContainsPosition(
                positions: visible,
                position: new Vector3(1f, 2f, 3f)
            ),
            "Hiding a real winch must allow its preview on the next frame."
        );
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
