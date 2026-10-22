// Collects only a winch's visual geometry, optionally placing it at an authored fallback.
using System.Collections.Generic;
using MoreSailwindSails.Controls.Placement;
using UnityEngine;

namespace MoreSailwindSails.Utils
{
    // Draws models without creating game objects, bindings or placement reservations.
    internal sealed class WinchModelPreview
    {
        private readonly GPButtonRopeWinch source;
        private readonly WinchSeat seat;
        private readonly List<WinchPreviewMesh> meshes = new List<WinchPreviewMesh>();

        internal bool Available => source && meshes.Exists(mesh => mesh.Available);
        internal bool Visible => meshes.Exists(mesh => mesh.Visible);

        internal WinchModelPreview(GPButtonRopeWinch source, WinchSeat seat = null)
        {
            this.source = source;
            this.seat = seat;
            if (!source)
                return;
            foreach (
                var renderer in source.GetComponentsInChildren<Renderer>(includeInactive: true)
            )
            {
                if (!BelongsTo(source: source, node: renderer.transform))
                    continue;
                Mesh mesh = null;
                bool ownsMesh = false;
                try
                {
                    if (renderer is SkinnedMeshRenderer skin && skin.sharedMesh)
                    {
                        mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
                        ownsMesh = true;
                        skin.BakeMesh(mesh: mesh);
                    }
                    else if (renderer is MeshRenderer)
                        mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    if (mesh)
                        meshes.Add(
                            new WinchPreviewMesh(
                                renderer: renderer,
                                mesh: mesh,
                                ownsMesh: ownsMesh,
                                control: source.transform
                            )
                        );
                }
                catch (UnityException)
                {
                    if (ownsMesh && mesh)
                        Object.Destroy(obj: mesh);
                    // An unavailable visual must not disable the remaining diagnostic locations.
                }
            }
        }

        private static bool BelongsTo(GPButtonRopeWinch source, Transform node)
        {
            for (; node; node = node.parent)
            {
                if (node == source.transform)
                    return true;
                if (node.GetComponent<GPButtonRopeWinch>() || node.GetComponent<RopeController>())
                    return false;
            }
            return false;
        }

        internal Vector3 Position(Transform boat) =>
            seat == null
                ? boat.InverseTransformPoint(position: source.transform.position)
                : seat.Position;

        internal void Draw(Camera camera, Transform boat)
        {
            if (!Available)
                return;
            Matrix4x4? mount =
                seat == null
                    ? (Matrix4x4?)null
                    : Matrix4x4.TRS(
                        pos: boat.TransformPoint(position: seat.Position),
                        q: boat.rotation * seat.Rotation,
                        s: Vector3.one
                    );
            foreach (var mesh in meshes)
                mesh.Draw(camera: camera, mount: mount, scale: source.transform.lossyScale);
        }

        internal void Dispose()
        {
            foreach (var mesh in meshes)
                mesh.Dispose();
            meshes.Clear();
        }
    }
}
