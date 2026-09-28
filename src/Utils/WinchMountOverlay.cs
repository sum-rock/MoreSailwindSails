using System;
using System.Collections.Generic;
using MoreSailwindSails.Controls;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoreSailwindSails.Utils
{
    // Draws optional winch diagnostics using owned camera resources, never native renderers.
    internal sealed class WinchMountOverlay : MonoBehaviour
    {
        private readonly Dictionary<Mesh, Vector3[]> meshEdges = new Dictionary<Mesh, Vector3[]>();
        private readonly List<WinchMountTrace> traces = new List<WinchMountTrace>();
        private BoatRefs boat;
        private NativeWinchSeats inventory;
        private Material material;
        private float nextDiscovery;
        private Vector3[] positions = Array.Empty<Vector3>();
        private WinchMountStatus[] states = Array.Empty<WinchMountStatus>();
        private int[] groups = Array.Empty<int>();
        private int[] best = Array.Empty<int>();
        private int locationCount;

        private void OnEnable() => Camera.onPostRender += Draw;

        private void OnDisable()
        {
            Camera.onPostRender -= Draw;
            Clear();
        }

        internal void Toggle()
        {
            if (boat)
            {
                Clear();
                Notify(message: "Winch mount overlay hidden.");
                return;
            }
            if (!BoatSurfacePicker.CanInspect)
            {
                Notify(message: "Close menus and aim at a boat surface during normal play.");
                return;
            }
            if (!BoatSurfacePicker.TryPick(out var surface, out string error))
            {
                Notify(message: error);
                return;
            }
            Clear();
            var shader = Shader.Find(name: "Hidden/Internal-Colored");
            if (!shader || !shader.isSupported)
            {
                Notify(message: "Winch mount overlay shader unavailable; overlay remains off.");
                return;
            }
            material = new Material(shader: shader) { hideFlags = HideFlags.HideAndDontSave };
            material.SetInt(name: "_SrcBlend", value: (int)BlendMode.SrcAlpha);
            material.SetInt(name: "_DstBlend", value: (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt(name: "_Cull", value: (int)CullMode.Off);
            material.SetInt(name: "_ZWrite", value: 0);
            material.SetInt(name: "_ZTest", value: (int)CompareFunction.Always);
            boat = surface.Boat;
            // This separate read-only inventory includes owned controls for display only.
            // It never participates in placement or reserves any seats.
            inventory = new NativeWinchSeats(boat: boat, owned: c => false);
            Refresh();
            Notify(
                message: "Winch mount overlay: "
                    + boat.name
                    + ". The position-capture shortcut remains available."
            );
        }

        private void LateUpdate()
        {
            if (!boat || GameState.currentlyLoading || !GameState.playing)
            {
                if (inventory != null)
                    Clear();
                return;
            }
            if (!BoatSurfacePicker.CanInspect)
                return;
            if (Time.unscaledTime >= nextDiscovery)
                Refresh();
        }

        private void Refresh()
        {
            inventory.Refresh();
            traces.Clear();
            var used = new HashSet<Mesh>();
            foreach (var control in inventory.Controls)
            {
                if (!control)
                    continue;
                var shapes = new List<WinchMountShape>();
                foreach (
                    var filter in control.GetComponentsInChildren<MeshFilter>(includeInactive: true)
                )
                {
                    if (FindControl(source: filter.transform) != control)
                        continue;
                    var mesh = filter.sharedMesh;
                    if (!mesh)
                        continue;
                    used.Add(mesh);
                    if (!meshEdges.TryGetValue(mesh, out var edges))
                    {
                        edges = ReadEdges(mesh: mesh);
                        meshEdges.Add(key: mesh, value: edges);
                    }
                    shapes.Add(new WinchMountShape(transform: filter.transform, edges: edges));
                }
                foreach (
                    var skin in control.GetComponentsInChildren<SkinnedMeshRenderer>(
                        includeInactive: true
                    )
                )
                {
                    if (FindControl(source: skin.transform) == control)
                        shapes.Add(
                            new WinchMountShape(
                                transform: skin.transform,
                                edges: WinchMountOverlayGeometry.BoundsEdges(
                                    bounds: skin.localBounds
                                )
                            )
                        );
                }
                traces.Add(new WinchMountTrace(control: control, shapes: shapes.ToArray()));
            }
            var stale = new List<Mesh>();
            foreach (var mesh in meshEdges.Keys)
                if (!used.Contains(mesh))
                    stale.Add(mesh);
            foreach (var mesh in stale)
                meshEdges.Remove(key: mesh);
            positions = new Vector3[traces.Count];
            states = new WinchMountStatus[traces.Count];
            groups = new int[traces.Count];
            best = new int[traces.Count];
            nextDiscovery = Time.unscaledTime + 1f;
        }

        // Installed Unity lacks GetComponentInParent(includeInactive); walk inactive parents explicitly.
        private static GPButtonRopeWinch FindControl(Transform source)
        {
            for (var node = source; node; node = node.parent)
            {
                var control = node.GetComponent<GPButtonRopeWinch>();
                if (control)
                    return control;
            }
            return null;
        }

        private static Vector3[] ReadEdges(Mesh mesh)
        {
            if (mesh.isReadable)
            {
                try
                {
                    var edges = WinchMountOverlayGeometry.MeshEdges(
                        vertices: mesh.vertices,
                        triangles: mesh.triangles
                    );
                    if (edges.Length > 0)
                        return edges;
                }
                catch (UnityException)
                {
                    // Some installed/modded meshes reject CPU access despite their flag.
                }
            }
            return WinchMountOverlayGeometry.BoundsEdges(bounds: mesh.bounds);
        }

        private void Draw(Camera camera)
        {
            if (
                !enabled
                || !boat
                || !material
                || !BoatSurfacePicker.CanInspect
                || camera != Camera.main
            )
                return;
            for (int i = 0; i < traces.Count; i++)
            {
                var trace = traces[i];
                states[i] = trace.Status(inventory: inventory);
                positions[i] = trace.Control ? inventory.Position(c: trace.Control) : Vector3.zero;
            }
            WinchMountOverlayGeometry.Group(
                positions: positions,
                states: states,
                groups: groups,
                best: best
            );
            locationCount = 0;
            if (!material.SetPass(pass: 0))
            {
                Clear();
                Notify(message: "Winch mount overlay material could not render; overlay disabled.");
                return;
            }
            GL.PushMatrix();
            try
            {
                GL.LoadProjectionMatrix(mat: camera.projectionMatrix);
                GL.modelview = camera.worldToCameraMatrix;
                GL.Begin(mode: GL.LINES);
                try
                {
                    for (int i = 0; i < traces.Count; i++)
                    {
                        if (groups[i] != i)
                            continue;
                        locationCount++;
                        GL.Color(c: ColorFor(state: states[i]));
                        foreach (var shape in traces[i].Shapes)
                        {
                            if (!shape.Transform)
                                continue;
                            var transform = shape.Transform.localToWorldMatrix;
                            foreach (var point in shape.Edges)
                                GL.Vertex(v: transform.MultiplyPoint3x4(point));
                        }
                    }
                }
                finally
                {
                    GL.End();
                }
            }
            finally
            {
                GL.PopMatrix();
            }
        }

        private void OnGUI()
        {
            if (!boat || !material || !BoatSurfacePicker.CanInspect || !Camera.main)
                return;
            var color = GUI.color;
            try
            {
                GUI.color = Color.white;
                GUI.Box(
                    position: new Rect(16f, 16f, 360f, 132f),
                    text: "Winch mounts: " + boat.name
                );
                Legend(
                    y: 42f,
                    state: WinchMountStatus.Unused,
                    text: "Green: unused on fitted supports"
                );
                Legend(
                    y: 64f,
                    state: WinchMountStatus.Occupied,
                    text: "Amber: occupied (including hidden bound ropes)"
                );
                Legend(
                    y: 86f,
                    state: WinchMountStatus.Unfitted,
                    text: "Cyan: unused on unfitted supports"
                );
                GUI.color = Color.white;
                GUI.Label(
                    position: new Rect(26f, 110f, 340f, 26f),
                    text: locationCount + " locations; green does not guarantee allocation."
                );
            }
            finally
            {
                GUI.color = color;
            }
        }

        private static void Legend(float y, WinchMountStatus state, string text)
        {
            GUI.color = ColorFor(state: state);
            GUI.Label(position: new Rect(26f, y, 340f, 24f), text: text);
        }

        private static Color ColorFor(WinchMountStatus state) =>
            state == WinchMountStatus.Occupied ? new Color(1f, 0.65f, 0f, 1f)
            : state == WinchMountStatus.Unused ? Color.green
            : Color.cyan;

        private void Clear()
        {
            boat = null;
            inventory = null;
            traces.Clear();
            meshEdges.Clear();
            positions = Array.Empty<Vector3>();
            states = Array.Empty<WinchMountStatus>();
            groups = Array.Empty<int>();
            best = Array.Empty<int>();
            locationCount = 0;
            if (material)
                Destroy(obj: material);
            material = null;
        }

        private static void Notify(string message)
        {
            Plugin.Log.LogInfo(data: "Winch mount overlay: " + message);
            if (NotificationUi.instance)
                NotificationUi.instance.ShowNotification(message);
        }
    }
}
