// Samples native rope motion for reef lift and draws the native furled bundle.
using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    internal sealed class FishermansStaysailReefing : MonoBehaviour
    {
        public GameObject Driver;
        public AnimationClip Clip;
        public Transform Rope;
        public Vector3 OpenRope,
            ClosedRope;
        public Transform BundleRoot;
        public MeshRenderer Bundle;
        private float lastUnroll = float.NaN;
        internal float Lift { get; private set; } = 1;

        internal static FishermansStaysailReefing Create(Sail sail, ReefEffectAnimUniversal donor)
        {
            string prefix = sail.GetComponent<FishermansStaysailShape>().ObjectPrefix;
            var animator = donor.overrideAnimator;
            var clip = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c =>
                c.name == donor.clipName
            );
            if (!clip || !donor.furledSail || !donor.furledSail.GetComponent<MeshFilter>())
                throw new InvalidOperationException(
                    "Expected the native brig jib reef clip and furled mesh."
                );
            var result = sail.gameObject.AddComponent<FishermansStaysailReefing>();
            result.Clip = clip;
            var inactive = new GameObject(prefix + " native reef driver");
            inactive.SetActive(false);
            inactive.transform.SetParent(sail.transform, false);
            result.Driver = Object.Instantiate(animator.gameObject, inactive.transform, false);
            result.Driver.name = prefix + " sampled native rig";
            // Keep the original animation paths but no live duplicate behaviour,
            // renderers, physics or wind callbacks inside the sampling hierarchy.
            foreach (var script in result.Driver.GetComponentsInChildren<MonoBehaviour>(true))
                Object.DestroyImmediate(script);
            foreach (var cloth in result.Driver.GetComponentsInChildren<Cloth>(true))
                Object.DestroyImmediate(cloth);
            foreach (var collider in result.Driver.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            foreach (var renderer in result.Driver.GetComponentsInChildren<Renderer>(true))
                Object.DestroyImmediate(renderer);
            foreach (var animation in result.Driver.GetComponentsInChildren<Animator>(true))
                animation.enabled = false;
            result.Rope = result.Driver.transform.Find("SAIL_jib/rope_att_jib_angle");
            if (!result.Rope)
                throw new InvalidOperationException("Native brig jib animation paths changed.");
            clip.SampleAnimation(result.Driver, 0);
            result.OpenRope = result.Rope.localPosition;
            clip.SampleAnimation(result.Driver, clip.length);
            result.ClosedRope = result.Rope.localPosition;
            // Fail at construction rather than leaving a half-working invisible sail.
            FishermansStaysailReefingGeometry.Progress(
                result.OpenRope,
                result.ClosedRope,
                result.OpenRope
            );
            clip.SampleAnimation(result.Driver, 0);

            result.BundleRoot = new GameObject(prefix + " resting sail").transform;
            result.BundleRoot.SetParent(sail.transform, false);
            var bundle = Object.Instantiate(donor.furledSail.gameObject, result.BundleRoot, false);
            bundle.name = prefix + " native furled sail";
            // The donor's model rotates its long Z axis for a diagonal stay.
            // Align its long Z axis along the head when drawing the bundle.
            bundle.transform.localRotation = Quaternion.identity;
            var mesh = bundle.GetComponent<MeshFilter>().sharedMesh;
            bundle.transform.localPosition = -Vector3.Scale(
                mesh.bounds.center,
                bundle.transform.localScale
            );
            result.Bundle = bundle.GetComponent<MeshRenderer>();
            result.Bundle.enabled = false;
            return result;
        }

        internal void Sample(float unroll)
        {
            if (unroll == lastUnroll)
                return;
            Clip.SampleAnimation(Driver, (1 - Mathf.Clamp01(unroll)) * Clip.length);
            // The native rope attachment supplies vertical deployment progress.
            Lift = FishermansStaysailReefingGeometry.Progress(
                Rope.localPosition,
                ClosedRope,
                OpenRope
            );
            lastUnroll = unroll;
        }

        internal Vector3 Pose(Vector3 point, Vector3 foreHead, Vector3 aftHead) =>
            FishermansStaysailReefingGeometry.Pose(
                point: point,
                foreHead: foreHead,
                aftHead: aftHead,
                lift: Lift
            );

        internal void DrawBundle(
            Vector3 foreHead,
            Vector3 aftHead,
            Vector3 mastAxis,
            float scale,
            Material material,
            bool visible
        )
        {
            var head = aftHead - foreHead;
            BundleRoot.SetPositionAndRotation(
                (foreHead + aftHead) * 0.5f,
                Quaternion.LookRotation(head, Vector3.Cross(head, mastAxis))
            );
            var mesh = Bundle.GetComponent<MeshFilter>().sharedMesh;
            float nativeLength = mesh.bounds.size.z * Bundle.transform.localScale.z;
            // Fit length independently of thickness, including steeper stays.
            BundleRoot.localScale = new Vector3(scale, scale, head.magnitude / nativeLength);
            Bundle.sharedMaterial = material;
            Bundle.enabled = visible;
        }
    }
}
