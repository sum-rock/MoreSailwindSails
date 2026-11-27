using System;
using System.Linq;
using System.Reflection;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Utils;

// Keeps the optional recorder independent from MSS and prevents gameplay writes or patching.
internal static class BenchmarkIntegrationChecks
{
    internal static void Run(Assembly assembly)
    {
        Require(
            !assembly
                .GetReferencedAssemblies()
                .Any(a => a.Name is "MoreSailwindSails" or "0Harmony"),
            "Recorder must load without MSS or Harmony"
        );
        foreach (var type in assembly.GetTypes())
        foreach (
            var method in type.GetMethods(
                BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance
                    | BindingFlags.Static
                    | BindingFlags.DeclaredOnly
            )
        )
        foreach (var called in IlReader.CalledMethods(method))
        {
            string owner = called.DeclaringType?.Name;
            Require(
                owner is not ("Sail" or "Cloth" or "Rigidbody" or "HingeJoint" or "Transform"),
                "Recorder must not invoke sail or physics mechanics"
            );
            if (owner is "QualitySettings" or "Screen" or "Application" or "Time")
                Require(
                    !called.Name.StartsWith("set_", StringComparison.Ordinal),
                    "Recorder must not change settings"
                );
        }
        var plugin = assembly.GetType("SailwindBenchmark.BenchmarkPlugin", throwOnError: true);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var loop = plugin.GetMethod(name: "LateUpdate", bindingAttr: flags);
        var reasons = IlReader
            .Instructions(method: loop)
            .Select(i => i.Operand)
            .OfType<string>()
            .ToArray();
        Require(
            reasons.Contains("user-cancelled")
                && !reasons.Any(r =>
                    r
                        is "focus-pause-or-loading"
                            or "environment-changed"
                            or "configuration-changed"
                ),
            "Frame loop only permits explicit user cancellation"
        );
        Require(
            !IlReader.CalledMethods(loop).Any(c => c.Name is "get_Ready" or "EnvironmentMatches"),
            "Gameplay readiness must not gate continued recording"
        );
        Console.WriteLine(
            "PASS: benchmark assembly independent of MSS/Harmony and does not invoke gameplay mechanics or change settings."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }
}
