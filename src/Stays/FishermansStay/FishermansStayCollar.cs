using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay
{
    // Poses one owned native collar around its physical mast and exposes an actual rope contact point.
    internal sealed class FishermansStayCollar
    {
        private readonly Transform node;
        private readonly Vector3[] points;
        private readonly FishermansStayCollarFrame frame;
        private FishermansStaySparSurface seatedSurface;
        private Vector3 seatedAttachment,
            seatedCenter;
        private float radialScale;
        internal Vector3 Center => node.localPosition;

        internal FishermansStayCollar(Transform node, Mesh mesh, FishermansStayCollarFrame frame)
        {
            this.node = node;
            this.frame = frame;
            var vertices = mesh.vertices;
            points = mesh.triangles.Distinct().Select(i => vertices[i]).ToArray();
        }

        internal void Place(
            Matrix4x4 mastToOwner,
            FishermansStaySparSurface surface,
            Vector3 attachment
        )
        {
            // The authored attachment and mesh surface are static for this variant. Boat motion
            // only changes its frame, so shipyard refreshes do not need to rescan the mesh.
            if (seatedSurface != surface || !seatedAttachment.Equals(attachment))
            {
                seatedCenter = surface.Seat(
                    attachment: attachment,
                    halfHeight: frame.HalfHeight,
                    radius: out float radius
                );
                radialScale = radius / frame.InnerRadius;
                seatedAttachment = attachment;
                seatedSurface = surface;
            }
            node.localPosition = mastToOwner.MultiplyPoint3x4(seatedCenter);
            node.localRotation = mastToOwner.rotation;
            node.localScale = Vector3.Scale(
                mastToOwner.lossyScale,
                new Vector3(radialScale, radialScale, 1)
            );
        }

        internal Vector3 Contact(Vector3 toward) =>
            FishermansStayCollarFrame.Contact(
                points: points,
                localToOwner: Matrix4x4.TRS(
                    pos: node.localPosition,
                    q: node.localRotation,
                    s: node.localScale
                ),
                toward: toward
            );
    }
}
