using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Utils.Profiling;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Caches read-only mast vertices so a fitted socket seats on timber rather than its collision capsule.
    internal sealed class SpritsailMastSurface
    {
        private readonly MeshFilter filter;
        private readonly Mesh sourceMesh;
        internal Transform Frame => filter ? filter.transform : null;
        internal bool MeshUnchanged => !filter || filter.sharedMesh == sourceMesh;
        private readonly Vector3[] vertices;
        private readonly int[] triangles;
        private bool warned;
        private readonly bool authoredSanbuqTopmast;
        private readonly SpritsailSurfaceSample[] samples;

        internal SpritsailMastSurface(Mast mast, int sampleCount = 1)
        {
            samples = new SpritsailSurfaceSample[sampleCount];
            filter = mast.GetComponent<MeshFilter>();
            sourceMesh = filter ? filter.sharedMesh : null;
            authoredSanbuqTopmast =
                mast.orderIndex == 80
                && filter
                && filter.sharedMesh
                && filter.sharedMesh.name == "mizzen_topmast_sanbuq";
            if (filter && filter.sharedMesh && filter.sharedMesh.isReadable)
            {
                vertices = filter.sharedMesh.vertices;
                triangles = filter.sharedMesh.triangles;
            }
        }

        internal float Radius(
            Vector3 center,
            Vector3 direction,
            float fallback,
            int sampleIndex = 0
        )
        {
            return RadiusLocal(
                origin: filter ? filter.transform.InverseTransformPoint(position: center) : center,
                ray: filter
                    ? filter.transform.InverseTransformVector(vector: direction)
                    : direction,
                fallback: fallback,
                sampleIndex: sampleIndex
            );
        }

        internal float RadiusLocal(Vector3 origin, Vector3 ray, float fallback, int sampleIndex = 0)
        {
            if (filter)
            {
                if (
                    samples[sampleIndex]
                        .TryGet(origin: origin, direction: ray, radius: out var cachedRadius)
                )
                {
                    PerformanceProfile.SurfaceQuery(hit: true);
                    return cachedRadius;
                }
                PerformanceProfile.SurfaceQuery(hit: false);
                float radius = 0;
                bool found =
                    vertices != null
                    && SpritsailMastGeometry.TryDistance(
                        vertices: vertices,
                        triangles: triangles,
                        origin: origin,
                        direction: ray,
                        distance: out radius
                    );
                if (
                    !found
                    && authoredSanbuqTopmast
                    && origin.z >= Sanbuq.StayTopmastBottom
                    && origin.z <= Sanbuq.StayTopmastTop
                )
                {
                    float t =
                        (origin.z - Sanbuq.StayTopmastBottom)
                        / (Sanbuq.StayTopmastTop - Sanbuq.StayTopmastBottom);
                    radius =
                        Mathf.Lerp(
                            a: Sanbuq.StayTopmastBottomRadius,
                            b: Sanbuq.StayTopmastTopRadius,
                            t: t
                        ) / Mathf.Sqrt(f: ray.x * ray.x + ray.y * ray.y);
                    found = SpritsailDeployment.Finite(value: radius);
                }
                if (found)
                {
                    samples[sampleIndex].Store(origin: origin, direction: ray, radius: radius);
                    return radius;
                }
            }
            if (!warned)
            {
                Plugin.Log.LogWarning(
                    data: "Spritsail mast surface unavailable; using capsule radius for socket seating."
                );
                warned = true;
            }
            return fallback;
        }
    }
}
