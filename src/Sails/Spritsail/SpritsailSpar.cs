using System;
using System.Linq;
using MoreSailwindSails.Utils.Profiling;
using MoreSailwindSails.Visuals;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Renders a procedural rigid sprit using the installed small gaff's timber material.
    internal sealed class SpritsailSpar : MonoBehaviour
    {
        private readonly SailVisualCache cache = new SailVisualCache();
        public MeshRenderer Renderer;
        public SpritsailSnotter Snotter;
        public SpritsailFurledVisual Furled;

        internal static SpritsailSpar Create(
            Transform parent,
            Mesh mesh,
            PrefabsDirectory directory
        )
        {
            var donor = directory.sails.Length > 15 ? directory.sails[15] : null;
            var sail = donor ? donor.GetComponent<Sail>() : null;
            var timber =
                sail && sail.category == SailCategory.gaff && sail.prefabIndex == 15
                    ? donor
                        .GetComponentsInChildren<MeshRenderer>(includeInactive: true)
                        .FirstOrDefault(predicate: r =>
                            r.name == "boom_gaff_top" && r.sharedMaterial
                        )
                    : null;
            if (!timber)
                throw new InvalidOperationException(
                    message: "Expected gaff 15's boom_gaff_top timber material."
                );
            var root = new GameObject(name: "Spritsail rigid sprit");
            root.transform.SetParent(parent: parent, worldPositionStays: false);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var spar = root.AddComponent<SpritsailSpar>();
            spar.Renderer = root.AddComponent<MeshRenderer>();
            spar.Renderer.sharedMaterial = timber.sharedMaterial;
            spar.Renderer.enabled = false;
            spar.Snotter = SpritsailSnotter.Create(
                parent: parent,
                timber: timber.sharedMaterial,
                directory: directory
            );
            spar.Furled = SpritsailFurledVisual.Create(parent: parent, directory: directory);
            return spar;
        }

        internal static Mesh CreateMesh()
        {
            SpritsailSpritGeometry.Spar(
                vertices: out var vertices,
                uv: out var uv,
                triangles: out var triangles
            );
            var mesh = new Mesh
            {
                name = "Spritsail blunt sprit",
                vertices = vertices,
                uv = uv,
                triangles = triangles,
            };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        internal void Pose(Vector3 heel, Vector3 tip, float radius, long revision)
        {
            using (PerformanceProfile.Measure(target: ProfileTarget.Sprit))
            {
                bool rebuild = cache.Needs(revision: revision);
                PerformanceProfile.Consumer(target: ProfileTarget.Sprit, rebuild: rebuild);
                if (!rebuild)
                    return;
                radius *= SpritsailSpritGeometry.ThicknessMultiplier;
                transform.SetPositionAndRotation(
                    position: heel,
                    rotation: Quaternion.LookRotation(forward: tip - heel)
                );
                var parentScale = transform.parent.lossyScale;
                transform.localScale = new Vector3(
                    radius / parentScale.x,
                    radius / parentScale.y,
                    (tip - heel).magnitude / parentScale.z
                );
                cache.Commit(revision: revision);
            }
        }

        private void OnDisable() => cache.Invalidate();

        internal void SetVisible(bool visible)
        {
            Renderer.enabled = visible;
            if (!visible)
            {
                Snotter.Hide();
                Furled.Hide();
            }
        }
    }
}
