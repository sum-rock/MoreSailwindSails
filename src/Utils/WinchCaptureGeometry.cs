using UnityEngine;

namespace MoreSailwindSails.Utils
{
    // Converts capture rays between visual and displaced walking-model frames.
    internal static class WinchCaptureGeometry
    {
        // A normalized ray needs a matching range conversion when roots are scaled.
        internal static Ray TransformRay(Matrix4x4 transform, Ray ray, out float distanceScale)
        {
            var direction = transform.MultiplyVector(ray.direction);
            distanceScale = direction.magnitude;
            return new Ray(transform.MultiplyPoint3x4(ray.origin), direction);
        }
    }
}
