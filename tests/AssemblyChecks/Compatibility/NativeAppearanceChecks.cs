// Checks native recoloring, private textures and material sharing for multi-renderer sails.
using System;
using System.Linq;
using System.Reflection;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.Compatibility;

internal static class NativeAppearanceChecks
{
    internal static void Run(Assembly assembly)
    {
        foreach (string family in new[] { "FishermansFlyingSail", "FishermansStaysail" })
        {
            var appearance = assembly.GetType(
                $"MoreSailwindSails.Sails.{family}.{family}Appearance",
                true
            );
            var calls = CalledMethods(
                    appearance.GetMethod("Configure", BindingFlags.Static | BindingFlags.NonPublic)
                )
                .ToArray();
            int sharedReferences = family == "FishermansFlyingSail" ? 2 : 0;
            if (
                !calls.Any(m => m.DeclaringType.Name == "Sail" && m.Name == "ChangeSailColor")
                || !calls.Any(m => m.Name == "get_sharedMaterial")
                || calls.Count(m => m.Name == "set_sharedMaterial") != sharedReferences
                || calls.Any(m => m.Name == "SetTexture")
            )
                throw new Exception(
                    family
                        + " must retain native recoloring and share materials only with its additional renderers."
                );
            int textureWrite = Array.FindIndex(calls, m => m.Name == "set_mainTexture");
            int recolor = Array.FindIndex(calls, m => m.Name == "ChangeSailColor");
            if (
                textureWrite <= recolor
                || !calls.Any(m =>
                    m.DeclaringType.Name == "Texture2D" && m.Name == "get_whiteTexture"
                )
                || calls
                    .OfType<MethodInfo>()
                    .Any(m =>
                        m.Name == "GetComponent"
                        && m.IsGenericMethod
                        && m.GetGenericArguments()[0].Name.EndsWith("Shape")
                    )
            )
                throw new Exception(
                    "Plain sail texture must be initialized after native material isolation."
                );
            // Verify the installed native behavior relied on for donor isolation.
            var nativeRecolor = calls.First(m => m.Name == "ChangeSailColor");
            if (
                !CalledMethods(nativeRecolor)
                    .Any(m => m.DeclaringType.Name == "Renderer" && m.Name == "get_material")
            )
                throw new Exception("Native recoloring no longer obtains a private material.");
        }
        Console.WriteLine(
            "PASS: native material isolation, plain sail initialization and reefed material sharing; live appearance remains an in-game check."
        );
    }
}
