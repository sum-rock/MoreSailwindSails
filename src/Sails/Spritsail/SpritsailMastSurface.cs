using MoreSailwindSails.BoatRigs;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Caches read-only mast vertices so a fitted socket seats on timber rather than its collision capsule.
    internal sealed class SpritsailMastSurface
    {
        private readonly MeshFilter filter;
        private readonly Vector3[] vertices;
        private readonly int[] triangles;
        private bool warned;
        private readonly bool authoredSanbuqTopmast;
        private readonly SpritsailSurfaceSample[] samples;

        internal SpritsailMastSurface(Mast mast, int sampleCount = 1)
        {
            samples = new SpritsailSurfaceSample[sampleCount];
            filter = mast.GetComponent<MeshFilter>();
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
            if (filter)
            {
                var origin = filter.transform.InverseTransformPoint(position: center);
                var ray = filter.transform.InverseTransformVector(vector: direction);
                if (
                    samples[sampleIndex]
                        .TryGet(origin: origin, direction: ray, radius: out var cachedRadius)
                )
                {
                    SpritsailMountProfile.SurfaceQuery(hit: true);
                    return cachedRadius;
                }
                SpritsailMountProfile.SurfaceQuery(hit: false);
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
