using System;
using System.Linq;
using System.Reflection;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Utils;

// Checks profiler integration and isolation from native mechanics.
internal static class ProfileIntegrationChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags flags =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.Instance;
        Type Type(string name) =>
            assembly.GetType(name: "MoreSailwindSails." + name, throwOnError: true);
        MethodInfo Method(string name, string method) =>
            Type(name).GetMethod(name: method, bindingAttr: flags);
        foreach (
            string rig in new[]
            {
                "Sails.FishermansFlyingSail.FishermansFlyingSailRig",
                "Sails.FishermansStaysail.FishermansStaysailRig",
            }
        )
        {
            var late = Method(rig, "LateUpdate");
            Require(
                value: IlReader
                    .CalledMethods(late)
                    .Any(m => m.DeclaringType.Name == "PerformanceProfile" && m.Name == "Measure")
                    && late.GetMethodBody()
                        .ExceptionHandlingClauses.Any(c =>
                            c.Flags == ExceptionHandlingClauseOptions.Finally
                        ),
                message: "Every rig must measure its work and finalize scopes on early returns."
            );
            Require(
                value: !IlReader.CalledMethods(late).Any(m => m.Name == "IsBypassed"),
                message: "Profiling must not bypass rig mechanics."
            );
        }
        foreach (
            var type in assembly
                .GetTypes()
                .Where(t => t.Namespace == "MoreSailwindSails.Utils.Profiling")
        )
        foreach (var method in type.GetMethods(bindingAttr: flags | BindingFlags.DeclaredOnly))
            Require(
                value: IlReader
                    .CalledMethods(method)
                    .All(m =>
                        m.DeclaringType.Name
                            is not ("Cloth" or "Sail" or "Rigidbody" or "HingeJoint" or "Transform")
                    ),
                message: "The profiling utility must not access or mutate sail mechanics."
            );
        Console.WriteLine(
            "PASS: Fisherman sail rigs instrumented with finalized scopes; profiler avoids mechanics. Live rendering requires in-game checks."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message: message);
    }
}
