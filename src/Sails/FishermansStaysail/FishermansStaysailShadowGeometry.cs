// Supplies the same coarse shadow samples and collider envelope for templates and fitted cuts.
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    internal static class FishermansStaysailShadowGeometry
    {
        internal static Vector3[] Samples(Vector3[] vertices)
        {
            // Native shadow checks cast a ray per vertex. Keep nine neutral-plane samples.
            var samples = new Vector3[9];
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++)
            {
                samples[row * 3 + col] = vertices[
                    row
                        * (FishermansStaysailGeometry.Rows / 2)
                        * (FishermansStaysailGeometry.Columns + 1)
                        + col * (FishermansStaysailGeometry.Columns / 2)
                ];
                samples[row * 3 + col].y = 0;
            }
            return samples;
        }

        internal static Bounds ColliderBounds(Bounds clothBounds) =>
            new Bounds(
                center: new Vector3(clothBounds.center.x, 0, clothBounds.center.z),
                size: new Vector3(
                    clothBounds.size.x,
                    clothBounds.size.y * 2 + 0.1f,
                    clothBounds.size.z
                )
            );
    }
}
