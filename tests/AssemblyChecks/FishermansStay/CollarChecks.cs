using System;
using System.Linq;
using System.Reflection;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.FishermansStay;

// Guards native channel retention and owned mesh cleanup without instantiating Unity objects.
internal static class CollarChecks
{
    internal static void Run(Assembly assembly)
    {
        const string prefix = "MoreSailwindSails.Stays.FishermansStay.";
        const BindingFlags flags =
            BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.Instance
            | BindingFlags.Static;
        var create = assembly
            .GetType(prefix + "FishermansStayMeshCopy", throwOnError: true)
            .GetMethod("Create", flags);
        var calls = CalledMethods(create).ToArray();
        if (
            !calls.Any(m =>
                m.DeclaringType?.FullName == "UnityEngine.Object" && m.Name == "Instantiate"
            )
            || !calls.Any(m => m.Name == "SetTriangles")
            || calls.Any(m =>
                m.Name == "Clear"
                || m.Name.StartsWith("set_uv", StringComparison.Ordinal)
                || m.Name == "set_colors"
                || m.Name == "set_colors32"
                || m.Name == "RecalculateNormals"
            )
        )
            throw new Exception(
                "Collar extraction must clone native channels, retain seam indices and preserve material submeshes."
            );
        var stay = assembly.GetType(prefix + "FishermansStay", throwOnError: true);
        var meshes = stay.GetField("ownedMeshes", flags);
        var cleanup = stay.GetMethod("Destroy", flags);
        if (
            !Instructions(cleanup).Any(i => Equals(i.Operand, meshes))
            || !CalledMethods(cleanup)
                .Any(m => m.DeclaringType?.FullName == "UnityEngine.Object" && m.Name == "Destroy")
        )
            throw new Exception("Stay cleanup lost ownership of generated collar meshes.");
        var registry = assembly.GetType(prefix + "FishermansStayRegistry", throwOnError: true);
        foreach (string path in new[] { "Register", "OnDestroy" })
            if (
                !CalledMethods(registry.GetMethod(path, flags))
                    .Any(m => m.DeclaringType == stay && m.Name == "Destroy")
            )
                throw new Exception(
                    "Stay mesh cleanup is missing from registration rollback or teardown."
                );
        Console.WriteLine(
            "PASS: native collar mesh cloning/channel retention and owned cleanup wiring for rollback and teardown (structural checks)."
        );
    }
}
