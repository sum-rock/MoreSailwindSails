// Checks native recoloring and material sharing without loading Shipyard Expansion.
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
            if (
                !calls.Any(m => m.DeclaringType.Name == "Sail" && m.Name == "ChangeSailColor")
                || !calls.Any(m => m.Name == "get_sharedMaterial")
                || calls.Count(m => m.Name == "set_sharedMaterial") != 2
                || calls.Any(m => m.Name == "SetTexture")
            )
                throw new Exception(
                    "Appearance must inherit textures and share the native recolored material with both reefed references."
                );
        }
        Console.WriteLine(
            "PASS: native recoloring and reefed material sharing; live texture changes require in-game validation."
        );
    }
}
