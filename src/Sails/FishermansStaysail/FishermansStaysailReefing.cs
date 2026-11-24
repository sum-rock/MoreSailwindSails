// Draws the native furled bundle on the physical stay over the fitted sail span.
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    internal sealed class FishermansStaysailReefing : MonoBehaviour
    {
        public Transform BundleRoot;
        public MeshRenderer Bundle;

        internal static FishermansStaysailReefing Create(Sail sail, ReefEffectAnimUniversal donor)
        {
            string prefix = sail.GetComponent<FishermansStaysailShape>().ObjectPrefix;
            if (!donor.furledSail || !donor.furledSail.GetComponent<MeshFilter>())
                throw new InvalidOperationException("Expected the native brig jib furled mesh.");
            var result = sail.gameObject.AddComponent<FishermansStaysailReefing>();
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

        internal void DrawBundle(
            Vector3 foreHead,
            Vector3 aftHead,
            Vector3 mastAxis,
            float scale,
            Material material,
            bool visible
        )
        {
            Bundle.sharedMaterial = material;
            Bundle.enabled = visible;
            if (!visible)
                return;
            var head = aftHead - foreHead;
            BundleRoot.SetPositionAndRotation(
                (foreHead + aftHead) * 0.5f,
                Quaternion.LookRotation(head, Vector3.Cross(head, mastAxis))
            );
            var mesh = Bundle.GetComponent<MeshFilter>().sharedMesh;
            float nativeLength = mesh.bounds.size.z * Bundle.transform.localScale.z;
            // Fit length independently of thickness, including steeper stays.
            BundleRoot.localScale = new Vector3(scale, scale, head.magnitude / nativeLength);
        }
    }
}
