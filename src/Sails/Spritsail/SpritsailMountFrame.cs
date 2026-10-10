using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Builds metre-sized fitting transforms without subtracting large world-space positions.
    internal static class SpritsailMountFrame
    {
        internal static bool TryBuild(
            Transform cloth,
            Transform mast,
            Transform surface,
            out Matrix4x4 clothToFrame,
            out Matrix4x4 mastToFrame,
            out Matrix4x4 frameToSurface,
            out Quaternion mastRotation
        )
        {
            var common = mast;
            while (common && !cloth.IsChildOf(parent: common))
                common = common.parent;
            clothToFrame = mastToFrame = frameToSurface = Matrix4x4.identity;
            mastRotation = Quaternion.identity;
            if (!common || (surface && !surface.IsChildOf(parent: common)))
                return false;
            var clothToCommon = Relative(
                child: cloth,
                ancestor: common,
                rotation: out var clothRotation
            );
            var mastToCommon = Relative(
                child: mast,
                ancestor: common,
                rotation: out var relativeMastRotation
            );
            var inverseRotation = Quaternion.Inverse(rotation: clothRotation);
            // Uniform ancestor scales cancel all shared rotation exactly, avoiding numeric churn.
            var stretch = CommonStretch(common: common);
            var rigid = RigidFromCommon(
                clothOrigin: clothToCommon.MultiplyPoint3x4(point: Vector3.zero),
                linear: Matrix4x4.Rotate(q: inverseRotation) * stretch
            );
            clothToFrame = rigid * clothToCommon;
            mastToFrame = rigid * mastToCommon;
            mastRotation = inverseRotation * relativeMastRotation;
            var surfaceToCommon = Relative(
                child: surface ? surface : mast,
                ancestor: common,
                rotation: out _
            );
            frameToSurface = (rigid * surfaceToCommon).inverse;
            return true;
        }

        internal static Matrix4x4 RigidFromCommon(Vector3 clothOrigin, Matrix4x4 linear)
        {
            var offset = -linear.MultiplyVector(vector: clothOrigin);
            linear.m03 = offset.x;
            linear.m13 = offset.y;
            linear.m23 = offset.z;
            return linear;
        }

        private static Matrix4x4 CommonStretch(Transform common)
        {
            bool uniform = true;
            float scale = 1;
            for (var node = common; node; node = node.parent)
            {
                var local = node.localScale;
                uniform &= local.x == local.y && local.y == local.z;
                scale *= local.x;
            }
            if (uniform)
                return new Matrix4x4
                {
                    m00 = scale,
                    m11 = scale,
                    m22 = scale,
                    m33 = 1,
                };
            // Retain the full stretch/shear when an ancestor has nonuniform scale.
            var linear = Matrix4x4.identity;
            var rotation = Quaternion.identity;
            for (var node = common; node; node = node.parent)
            {
                linear =
                    Matrix4x4.TRS(pos: Vector3.zero, q: node.localRotation, s: node.localScale)
                    * linear;
                rotation = node.localRotation * rotation;
            }
            return Matrix4x4.Rotate(q: Quaternion.Inverse(rotation: rotation)) * linear;
        }

        private static Matrix4x4 Relative(
            Transform child,
            Transform ancestor,
            out Quaternion rotation
        )
        {
            var matrix = Matrix4x4.identity;
            rotation = Quaternion.identity;
            for (var node = child; node != ancestor; node = node.parent)
            {
                matrix =
                    Matrix4x4.TRS(
                        pos: node.localPosition,
                        q: node.localRotation,
                        s: node.localScale
                    ) * matrix;
                rotation = node.localRotation * rotation;
            }
            return matrix;
        }
    }
}
