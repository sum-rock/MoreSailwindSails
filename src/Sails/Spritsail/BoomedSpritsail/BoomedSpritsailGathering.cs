using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail
{
    // Folds the interior toward the mast while the complete foot follows the lifting boom.
    internal static class BoomedSpritsailGathering
    {
        internal static Vector3 Point(
            Vector3[] corners,
            SpritsailDeploymentPose pose,
            float amount,
            float u,
            float v,
            float camber = 1,
            float normalScale = 1,
            float obstruction = 0
        )
        {
            var point = Vector3.Lerp(
                a: Vector3.Lerp(a: pose.Throat, b: pose.Tack, t: v),
                b: Vector3.Lerp(a: pose.Peak, b: pose.Clew, t: v),
                t: u
            );
            float width = corners[3].z - corners[2].z;
            point.y +=
                BoomedSpritsailGeometry.RestCamber(
                    width: width,
                    u: u,
                    v: v,
                    headReach: (corners[1].z - corners[0].z) / width
                )
                * normalScale
                * camber
                * amount
                * SpritsailObstructionGeometry.Camber(
                    point: point,
                    heel: pose.Heel,
                    tip: pose.Tip,
                    width: width,
                    obstruction: obstruction
                );
            // Folds vanish on all four edges: fixed luff/foot and slack head/leech
            // cannot gain length from the decorative partial-reef pleats.
            float interior = (float)(Math.Sin(u * Math.PI) * Math.Sin(v * Math.PI));
            point.y +=
                width
                * normalScale
                * (1 - amount)
                * 0.025f
                * interior
                * (float)Math.Sin(u * Math.PI * 8 + v * Math.PI * 4);
            point.z += width * (1 - amount) * 0.02f * interior;
            return point;
        }
    }
}
