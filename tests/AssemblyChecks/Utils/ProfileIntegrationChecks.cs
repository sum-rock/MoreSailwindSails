using System;
using System.Linq;
using System.Reflection;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Utils;

// Checks profiler integration and the boundary between visual bypass and native mechanics.
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
                "Sails.Spritsail.LooseFootedSpritsail.LooseFootedSpritsailRig",
                "Sails.Spritsail.BoomedSpritsail.BoomedSpritsailRig",
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
            var target in new[]
            {
                ("Sails.Spritsail.SpritsailMount", "Draw"),
                ("Sails.Spritsail.SpritsailSnotter", "Pose"),
                ("Sails.Spritsail.LooseFootedSpritsail.LooseFootedSpritsailLines", "Draw"),
                ("Sails.Spritsail.BoomedSpritsail.BoomedSpritsailLines", "Draw"),
            }
        )
        {
            var calls = IlReader.CalledMethods(Method(target.Item1, target.Item2)).ToArray();
            Require(
                value: calls.Any(m => m.Name == "IsBypassed"),
                message: "The visual owner must handle its bypass."
            );
            if (!target.Item1.EndsWith("SpritsailMount"))
                Require(
                    value: calls.Any(m => m.Name == "Hide"),
                    message: "Bypass must hide persistent rope renderers."
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
        Require(
            value: assembly.GetType("MoreSailwindSails.Sails.Spritsail.SpritsailMountProfile")
                == null,
            message: "The obsolete mount-only profiler must be removed."
        );
        var frame = assembly.GetType(
            name: "MoreSailwindSails.Sails.Spritsail.SpritsailMountFrame",
            throwOnError: true
        );
        foreach (var method in frame.GetMethods(bindingAttr: flags | BindingFlags.DeclaredOnly))
            Require(
                value: IlReader
                    .CalledMethods(method: method)
                    .All(m =>
                        m.DeclaringType.Name != "Transform"
                        || m.Name
                            is "get_parent"
                                or "get_localPosition"
                                or "get_localRotation"
                                or "get_localScale"
                                or "IsChildOf"
                    ),
                message: "Stable fitting frames must use the local hierarchy without world positions or transform mutation."
            );

        Console.WriteLine(
            "PASS: all sail rigs instrumented with finalized scopes; visual owners handle bypass, profiler avoids mechanics, and stable mount frames are preserved. Live rendering requires in-game checks."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message: message);
    }
}
