// Renders winch wireframes, fallback markers and optional model previews for the selected boat.
using System;
using System.Collections.Generic;
using MoreSailwindSails.Controls;
using MoreSailwindSails.Controls.Configuration;
using MoreSailwindSails.Controls.Discovery;
using MoreSailwindSails.Controls.Placement;
using MoreSailwindSails.Controls.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoreSailwindSails.Utils
{
    // Draws optional winch diagnostics using owned camera resources, never native renderers.
    internal sealed class WinchMountOverlay : MonoBehaviour
    {
        private static readonly Color FallbackColor = new Color(r: 0.7f, g: 0.3f, b: 1f, a: 1f);
        private readonly Dictionary<Mesh, Vector3[]> meshEdges = new Dictionary<Mesh, Vector3[]>();
        private readonly List<WinchMountTrace> traces = new List<WinchMountTrace>();
        private readonly List<Vector3[]> fallbackBoxes = new List<Vector3[]>();
        private readonly List<WinchModelPreview> models = new List<WinchModelPreview>();
        private readonly List<WinchModelPreview> fallbackModels = new List<WinchModelPreview>();
        private readonly List<Vector3> drawnModels = new List<Vector3>();
        private readonly HashSet<int> visibleModelGroups = new HashSet<int>();
        private bool showModels;
        private int unavailableFallbacks;
        internal string PreviewShortcut { get; set; }
        private BoatRefs boat;
        private NativeWinchInventory inventory;
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

        internal void ToggleModels()
        {
            if (!boat || !BoatSurfacePicker.CanInspect)
                return;
            showModels = !showModels;
            if (showModels)
                Refresh();
            else
                ClearModels();
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
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            material.SetInt(name: "_SrcBlend", value: (int)BlendMode.SrcAlpha);
            material.SetInt(name: "_DstBlend", value: (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt(name: "_Cull", value: (int)CullMode.Off);
            material.SetInt(name: "_ZWrite", value: 0);
            material.SetInt(name: "_ZTest", value: (int)CompareFunction.Always);
            boat = surface.Boat;
            // This separate read-only inventory includes owned controls for display only.
            // It never participates in placement or reserves any seats.
            inventory = new NativeWinchInventory(boat: boat, owned: c => false);
            CacheFallbackBoxes();
            Refresh();
            Notify(
                message: "Winch mount overlay: "
                    + boat.name
                    + ". The position-capture shortcut remains available."
            );
        }

        private void CacheFallbackBoxes()
        {
            fallbackBoxes.Clear();
            var profile = inventory.Profile;
            if (profile == null)
                return;
            // Authored positions remain visible regardless of allocation or support availability.
            foreach (var definition in profile.SheetFallbacks)
            {
                var pair = definition.Fallback;
                if (pair?.Port != null && pair.Port.Valid)
                    AddFallbackBox(position: pair.Port.Position(offset: pair.Offset));
                if (pair?.Starboard != null && pair.Starboard.Valid)
                    AddFallbackBox(position: pair.Starboard.Position(offset: pair.Offset));
            }
            foreach (var definition in profile.HalyardFallbacks)
                AddFallbackBox(position: definition.Fallback.Position);
        }

        private void AddFallbackBox(Vector3 position) =>
            fallbackBoxes.Add(
                WinchMountOverlayGeometry.BoundsEdges(
                    bounds: new Bounds(
                        center: position,
                        size: new Vector3(x: 0.3f, y: 0.3f, z: 0.3f)
                    )
                )
            );

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
            if (showModels && Camera.main)
            {
                GroupLocations();
                DrawModels(camera: Camera.main);
            }
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
            if (showModels)
                CacheModels();
            nextDiscovery = Time.unscaledTime + 1f;
        }

        private void CacheModels()
        {
            ClearModels();
            foreach (var trace in traces)
                models.Add(new WinchModelPreview(source: trace.Control));
            var profile = inventory.Profile;
            if (profile == null)
                return;
            foreach (var definition in profile.SheetFallbacks)
            {
                var pair = definition.Fallback;
                if (pair == null)
                    continue;
                var source = Template(
                    mast: pair.TemplateMast,
                    role: pair.TemplateRole,
                    index: pair.TemplateIndex
                );
                AddSheetModel(source: source, pair: pair, seat: pair.Port);
                AddSheetModel(source: source, pair: pair, seat: pair.Starboard);
            }
            foreach (var definition in profile.HalyardFallbacks)
            {
                var source = Template(
                    mast: definition.Mast,
                    role: WinchRole.Reef,
                    index: definition.Fallback.TemplateIndex
                );
                AddFallbackModel(
                    source: source,
                    position: definition.Fallback.Position,
                    rotation: source
                        ? definition.Fallback.Rotation(
                            templateRotation: inventory.Rotation(c: source)
                        )
                        : Quaternion.identity
                );
            }
        }

        private GPButtonRopeWinch Template(int mast, WinchRole role, int index)
        {
            var sources = NativeWinchInventory.Sources(mast: inventory.Mast(id: mast), role: role);
            if (sources == null || index < 0 || index >= sources.Length)
                return null;
            var source = sources[index];
            return
                NativeWinchInventory.Usable(c: source) && !source.GetComponent<OwnedWinchMarker>()
                ? source
                : null;
        }

        private void AddSheetModel(
            GPButtonRopeWinch source,
            SheetFallbackPair pair,
            SheetFallbackSeat seat
        )
        {
            if (seat == null || !seat.Valid)
                return;
            AddFallbackModel(
                source: source,
                position: seat.Position(offset: pair.Offset),
                rotation: source
                    ? seat.Rotation(
                        sourceNormal: pair.SourceNormal,
                        templateRotation: inventory.Rotation(c: source)
                    )
                    : Quaternion.identity
            );
        }

        private void AddFallbackModel(
            GPButtonRopeWinch source,
            Vector3 position,
            Quaternion rotation
        )
        {
            var model = new WinchModelPreview(
                source: source,
                seat: new WinchSeat(
                    identity: position,
                    aliases: null,
                    position: position,
                    rotation: rotation
                )
            );
            if (model.Available)
                fallbackModels.Add(model);
            else
            {
                unavailableFallbacks++;
                model.Dispose();
            }
        }

        private void DrawModels(Camera camera)
        {
            drawnModels.Clear();
            visibleModelGroups.Clear();
            for (int i = 0; i < models.Count; i++)
                if (models[i].Available && models[i].Visible)
                {
                    drawnModels.Add(positions[i]);
                    visibleModelGroups.Add(groups[i]);
                }
            // Authored previews take precedence over unused native aliases at the same position.
            foreach (var model in fallbackModels)
                DrawModel(model: model, camera: camera);
            for (int i = 0; i < models.Count; i++)
                if (groups[i] == i && !visibleModelGroups.Contains(i))
                    DrawModel(model: models[i], camera: camera);
        }

        private void DrawModel(WinchModelPreview model, Camera camera)
        {
            if (!model.Available)
                return;
            var position = model.Position(boat: boat.transform);
            if (
                WinchMountOverlayGeometry.ContainsPosition(
                    positions: drawnModels,
                    position: position
                )
            )
                return;
            model.Draw(camera: camera, boat: boat.transform);
            drawnModels.Add(position);
        }

        private void ClearModels()
        {
            foreach (var model in models)
                model.Dispose();
            foreach (var model in fallbackModels)
                model.Dispose();
            models.Clear();
            fallbackModels.Clear();
            drawnModels.Clear();
            visibleModelGroups.Clear();
            unavailableFallbacks = 0;
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
            GroupLocations();
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
                    GL.Color(c: FallbackColor);
                    var boatTransform = boat.transform.localToWorldMatrix;
                    foreach (var edges in fallbackBoxes)
                    foreach (var point in edges)
                        GL.Vertex(v: boatTransform.MultiplyPoint3x4(point));
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

        private void GroupLocations()
        {
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
                    position: new Rect(16f, 16f, 360f, 224f),
                    text: "Winch mounts: " + boat.name
                );
                Legend(
                    y: 42f,
                    color: ColorFor(state: WinchMountStatus.Unused),
                    text: "Green: unused on fitted supports"
                );
                Legend(
                    y: 64f,
                    color: ColorFor(state: WinchMountStatus.Occupied),
                    text: "Red: occupied (including hidden bound ropes)"
                );
                Legend(
                    y: 86f,
                    color: ColorFor(state: WinchMountStatus.Unfitted),
                    text: "Cyan: unused on unfitted supports"
                );
                Legend(y: 108f, color: FallbackColor, text: "Purple: defined fallback placement");
                GUI.color = Color.white;
                GUI.Label(
                    position: new Rect(26f, 132f, 340f, 24f),
                    text: locationCount
                        + " winch locations; "
                        + fallbackBoxes.Count
                        + " fallback boxes."
                );
                GUI.Label(
                    position: new Rect(26f, 154f, 340f, 26f),
                    text: "Green does not guarantee allocation."
                );
                GUI.Label(
                    position: new Rect(26f, 178f, 340f, 24f),
                    text: "Models: " + (showModels ? "on" : "off") + " (" + PreviewShortcut + ")"
                );
                if (showModels)
                    GUI.Label(
                        position: new Rect(26f, 202f, 340f, 24f),
                        text: unavailableFallbacks + " fallback models unavailable."
                    );
            }
            finally
            {
                GUI.color = color;
            }
        }

        private static void Legend(float y, Color color, string text)
        {
            GUI.color = color;
            GUI.Label(position: new Rect(26f, y, 340f, 24f), text: text);
        }

        private static Color ColorFor(WinchMountStatus state) =>
            state == WinchMountStatus.Occupied ? Color.red
            : state == WinchMountStatus.Unused ? Color.green
            : Color.cyan;

        private void Clear()
        {
            ClearModels();
            showModels = false;
            boat = null;
            inventory = null;
            traces.Clear();
            fallbackBoxes.Clear();
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
