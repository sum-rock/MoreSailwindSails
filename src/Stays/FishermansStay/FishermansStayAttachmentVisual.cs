using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay
{
    // Fits the rope separately from both mast collars in one visual or walking representation.
    internal sealed class FishermansStayAttachmentVisual
    {
        private readonly Transform body;
        private readonly FishermansStayCollar fore,
            aft;
        private readonly float min,
            max;
        private readonly Vector3 bodyCenter;

        internal FishermansStayAttachmentVisual(
            Transform geometry,
            bool combined,
            Transform collarTemplate,
            List<Mesh> owned
        )
        {
            body = geometry;
            var filter = geometry.GetComponent<MeshFilter>();
            var collider = geometry.GetComponent<MeshCollider>();
            var source =
                filter ? filter.sharedMesh
                : collider ? collider.sharedMesh
                : null;
            if (!source || (combined && !source.isReadable))
                throw new InvalidOperationException("Stay donor has no usable root rope mesh.");
            var regions = combined
                ? FishermansStayMeshRegions.Partition(
                    vertices: source.vertices,
                    triangles: source.triangles
                )
                : Enumerable.Repeat(FishermansStayMeshRegions.Rope, source.vertexCount).ToArray();
            if (combined)
            {
                fore = MakeCollar(
                    source: source,
                    template: geometry,
                    regions: regions,
                    region: FishermansStayMeshRegions.Fore,
                    toGeometry: Matrix4x4.identity,
                    owned: owned
                );
                aft = MakeCollar(
                    source: source,
                    template: geometry,
                    regions: regions,
                    region: FishermansStayMeshRegions.Aft,
                    toGeometry: Matrix4x4.identity,
                    owned: owned
                );
            }
            else
            {
                var coils = collarTemplate
                    .GetComponentsInChildren<MeshFilter>(includeInactive: true)
                    .Where(f =>
                        f.sharedMesh
                        && f.sharedMesh.name.StartsWith("coiled_rope_", StringComparison.Ordinal)
                        && !f.GetComponentInParent<Sail>()
                        && !f.GetComponentInParent<GPButtonRopeWinch>()
                    )
                    .OrderBy(f =>
                        collarTemplate
                            .InverseTransformPoint(
                                f.transform.TransformPoint(f.sharedMesh.bounds.center)
                            )
                            .z
                    )
                    .ToArray();
                if (coils.Length < 1 || coils.Length > 2)
                    throw new InvalidOperationException(
                        "Expected one or two separate native stay collars."
                    );
                FishermansStayCollar Copy(MeshFilter coil, int region) =>
                    MakeCollar(
                        source: coil.sharedMesh,
                        template: coil.transform,
                        regions: Enumerable.Repeat(region, coil.sharedMesh.vertexCount).ToArray(),
                        region: region,
                        toGeometry: collarTemplate.worldToLocalMatrix
                            * coil.transform.localToWorldMatrix,
                        owned: owned
                    );
                fore = Copy(coil: coils[0], region: FishermansStayMeshRegions.Fore);
                // SE's one-collar donors reuse their native coil for the missing aft attachment.
                aft = Copy(coil: coils[coils.Length - 1], region: FishermansStayMeshRegions.Aft);
                foreach (
                    var coil in geometry
                        .GetComponentsInChildren<MeshFilter>(includeInactive: true)
                        .Where(f =>
                            f.sharedMesh
                            && f.sharedMesh.name.StartsWith(
                                "coiled_rope_",
                                StringComparison.Ordinal
                            )
                        )
                )
                    coil.gameObject.SetActive(false);
            }
            var rope = source;
            // SE's static rope meshes can be non-readable. They already contain only the body;
            // transform their measured bounds without reading or cloning their vertex buffers.
            if (combined)
            {
                rope = FishermansStayMeshCopy.Create(
                    source: source,
                    regions: regions,
                    region: FishermansStayMeshRegions.Rope,
                    toGeometry: Matrix4x4.identity,
                    collar: false,
                    owned: owned,
                    frame: out _
                );
                if (filter)
                    filter.sharedMesh = rope;
                if (collider)
                    collider.sharedMesh = rope;
            }
            min = rope.bounds.min.z;
            max = rope.bounds.max.z;
            bodyCenter = new Vector3(rope.bounds.center.x, rope.bounds.center.y, 0);
        }

        private FishermansStayCollar MakeCollar(
            Mesh source,
            Transform template,
            int[] regions,
            int region,
            Matrix4x4 toGeometry,
            List<Mesh> owned
        )
        {
            var mesh = FishermansStayMeshCopy.Create(
                source: source,
                regions: regions,
                region: region,
                toGeometry: toGeometry,
                collar: true,
                owned: owned,
                frame: out var frame
            );
            var node = new GameObject(
                "FishermansStay "
                    + (region == FishermansStayMeshRegions.Fore ? "forward" : "aft")
                    + " collar"
            );
            node.transform.SetParent(parent: body.parent, worldPositionStays: false);
            node.layer = body.gameObject.layer;
            var renderer = template.GetComponent<MeshRenderer>();
            if (renderer)
            {
                node.AddComponent<MeshFilter>().sharedMesh = mesh;
                var copy = node.AddComponent<MeshRenderer>();
                copy.sharedMaterials = renderer.sharedMaterials;
                copy.shadowCastingMode = renderer.shadowCastingMode;
                copy.receiveShadows = renderer.receiveShadows;
            }
            var collider = template.GetComponent<MeshCollider>();
            if (collider)
            {
                var copy = node.AddComponent<MeshCollider>();
                copy.sharedMesh = mesh;
                copy.convex = collider.convex;
                copy.sharedMaterial = collider.sharedMaterial;
                copy.isTrigger = collider.isTrigger;
                copy.enabled = collider.enabled;
            }
            return new FishermansStayCollar(node: node.transform, mesh: mesh, frame: frame);
        }

        internal void Place(
            Matrix4x4 foreToOwner,
            Matrix4x4 aftToOwner,
            FishermansStaySparSurface foreSurface,
            FishermansStaySparSurface aftSurface,
            Vector3 forePoint,
            Vector3 aftPoint
        )
        {
            fore.Place(mastToOwner: foreToOwner, surface: foreSurface, attachment: forePoint);
            aft.Place(mastToOwner: aftToOwner, surface: aftSurface, attachment: aftPoint);
            var start = fore.Contact(toward: aft.Center);
            var end = aft.Contact(toward: fore.Center);
            float span = FishermansStayGeometry.Span(aft: end, fore: start);
            var direction = (end - start) / span;
            var rotation = Quaternion.LookRotation(
                forward: direction,
                upwards: FishermansStayGeometry.FrameUp(donorUp: Vector3.up, forward: direction)
            );
            FishermansStayGeometry.FitAxis(
                min: min,
                max: max,
                span: span,
                scale: out float scale,
                offset: out float offset
            );
            body.localRotation = rotation;
            body.localScale = new Vector3(1, 1, scale);
            body.localPosition = end + rotation * (new Vector3(0, 0, offset) - bodyCenter);
        }

        internal static FishermansStaySparSurface Surface(Mast mast, BoatRigDefinition profile)
        {
            var filter = mast.GetComponent<MeshFilter>();
            var capsule = mast.GetComponent<CapsuleCollider>();
            if (!filter || !filter.sharedMesh || !capsule || capsule.direction != 2)
                throw new InvalidOperationException(
                    "Supporting mast has no axial spar surface: " + mast.orderIndex
                );
            if (!filter.sharedMesh.isReadable)
            {
                if (
                    profile == Sanbuq.Definition
                    && mast.orderIndex == 80
                    && filter.sharedMesh.name == "mizzen_topmast_sanbuq"
                )
                    return FishermansStaySparSurface.Taper(
                        bottom: Sanbuq.StayTopmastBottom,
                        top: Sanbuq.StayTopmastTop,
                        bottomRadius: Sanbuq.StayTopmastBottomRadius,
                        topRadius: Sanbuq.StayTopmastTopRadius,
                        axis: capsule.center
                    );
                throw new InvalidOperationException(
                    "Supporting mast has no readable or authored spar surface: " + mast.orderIndex
                );
            }
            return new FishermansStaySparSurface(
                vertices: filter.sharedMesh.vertices,
                triangles: filter.sharedMesh.triangles,
                axis: capsule.center
            );
        }
    }
}
