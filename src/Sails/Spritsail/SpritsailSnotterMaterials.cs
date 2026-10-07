using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Couples the native finish donors and their UV policy; owns only explicit fallbacks.
    internal static class SpritsailSnotterMaterials
    {
        internal const int WoodSailIndex = 62;
        internal const int MetalItemIndex = 114;

        // Interior of Sailwind 0.39's dhow_medium_paint dark longitudinal mast trim.
        // Fallback wood is untextured, so this atlas remap is harmless without the donor.
        internal static Vector2 WoodUv(Vector2 uv) =>
            new Vector2(x: 0.350f + uv.x * 0.002f, y: 0.870f + uv.y * 0.030f);

        internal static Material Wood(GameObject donor, Shader shader, out Material owned) =>
            Resolve(
                donor: donor,
                nativeName: "dhow_medium_paint",
                shader: shader,
                color: new Color(r: 0.12f, g: 0.065f, b: 0.035f, a: 1),
                metallic: 0,
                smoothness: 0,
                owned: out owned
            );

        internal static Material Metal(GameObject donor, Shader shader, out Material owned) =>
            Resolve(
                donor: donor,
                nativeName: "metal2",
                shader: shader,
                color: new Color(r: 0.11035956f, g: 0.14488259f, b: 0.1509434f, a: 1),
                metallic: 0.51f,
                smoothness: 0.4f,
                owned: out owned
            );

        private static Material Resolve(
            GameObject donor,
            string nativeName,
            Shader shader,
            Color color,
            float metallic,
            float smoothness,
            out Material owned
        )
        {
            owned = null;
            if (donor)
                foreach (
                    var renderer in donor.GetComponentsInChildren<MeshRenderer>(
                        includeInactive: true
                    )
                )
                foreach (var material in renderer.sharedMaterials)
                    if (material && material.name == nativeName)
                        return material;

            // Construct from the Standard shader, not a copy of the gaff's wood maps.
            owned = new Material(shader: shader)
            {
                name = "Spritsail snotter fallback " + nativeName,
                color = color,
            };
            if (owned.HasProperty(name: "_Metallic"))
                owned.SetFloat(name: "_Metallic", value: metallic);
            if (owned.HasProperty(name: "_Glossiness"))
                owned.SetFloat(name: "_Glossiness", value: smoothness);
            return owned;
        }
    }
}
