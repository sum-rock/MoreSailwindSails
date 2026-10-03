using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail
{
    // Poses the existing skin into folds, retaining the upper luff and bunching its lower quarter.
    internal static class LooseFootedSpritsailGathering
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
            const float socketRow = 1 - SpritsailDeployment.SocketLuffFraction;
            var fore =
                v <= socketRow
                    ? Vector3.Lerp(a: pose.Throat, b: pose.Heel, t: v / socketRow)
                    : Vector3.Lerp(
                        a: pose.Heel,
                        b: pose.Tack,
                        t: (v - socketRow) / (1 - socketRow)
                    );
            var aft = Vector3.Lerp(a: pose.Peak, b: pose.Clew, t: v);
            var point = Vector3.Lerp(a: fore, b: aft, t: u);
            float width = corners[3].z - corners[2].z;
            point.y +=
                LooseFootedSpritsailGeometry.RestCamber(
                    width: width,
                    u: u,
                    v: v,
                    headReach: (corners[1].z - corners[0].z) / width
                )
                * normalScale
                * camber
                * SpritsailObstructionGeometry.Camber(
                    point: point,
                    heel: pose.Heel,
                    tip: pose.Tip,
                    width: width,
                    obstruction: obstruction
                )
                * amount;
            float lower = Math.Max(0, (v - socketRow) / (1 - socketRow));
            float lowerFold = (float)Math.Sin(lower * Math.PI * 4) * 0.025f;
            float upperFold =
                (float)(Math.Sin(u * Math.PI) * Math.Sin(u * Math.PI * 8 + v * Math.PI * 4))
                * 0.025f;
            // Outward folds keep the collapsed panel visible and off the mast.
            point.y += width * (1 - amount) * (lowerFold + upperFold);
            point.z +=
                width
                * (1 - amount)
                * 0.02f
                * (float)(Math.Sin(u * Math.PI) + Math.Sin(lower * Math.PI));
            return point;
        }
    }
}
