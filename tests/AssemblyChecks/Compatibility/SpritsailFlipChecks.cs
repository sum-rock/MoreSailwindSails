// Verifies the optional flip integration and reuse of SE's persisted state without Unity objects.
using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.Compatibility;

internal static class SpritsailFlipChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var type = assembly.GetType(
            name: "MoreSailwindSails.Compatibility.Patches.ShipyardExpansionSpritsailFlipPatch",
            throwOnError: true
        );
        type.GetMethod(name: "Install", bindingAttr: flags)
            .Invoke(obj: null, parameters: new object[] { new Harmony("mss.tests.optional.flip") });
        if (type.GetField(name: "flippedSetter", bindingAttr: flags).GetValue(obj: null) != null)
            throw new Exception("Absent SE must leave flip integration inactive.");
        var flip = type.GetMethod(name: "Flip", bindingAttr: flags);
        var calls = CalledMethods(method: flip).ToArray();
        if (
            flip.ReturnType != typeof(bool)
            || !calls.Any(method => method.Name == "Invoke")
            || !calls.Any(method => method.Name == "SetFlipped")
            || !calls
                .OfType<MethodInfo>()
                .Any(method =>
                    method.Name == "GetComponent"
                    && method.IsGenericMethod
                    && method.GetGenericArguments()[0].Name == "LooseFootedSpritsailRig"
                )
        )
            throw new Exception(
                "The flip override must scope itself to spritsails and synchronize SE and rig state."
            );
    }
}
