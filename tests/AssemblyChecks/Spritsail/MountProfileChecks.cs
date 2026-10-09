using System;
using System.Linq;
using System.Reflection;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Spritsail;

// Checks disabled diagnostics and ensures the comparison path cannot change sail physics.
internal static class MountProfileChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags flags =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.Instance;
        var profile = assembly.GetType(
            name: "MoreSailwindSails.Sails.Spritsail.SpritsailMountProfile",
            throwOnError: true
        );
        MethodInfo Method(string name) => profile.GetMethod(name: name, bindingAttr: flags);
        Require(
            value: !(bool)Method(name: "get_Bypass").Invoke(obj: null, parameters: null)
                && (long)Method(name: "Begin").Invoke(obj: null, parameters: null) == 0,
            message: "Profiling must start disabled with normal visuals and no timestamp capture."
        );
        Method(name: "Mark").Invoke(obj: null, parameters: new object[] { 0, 0L });
        Method(name: "EnvelopeQuery").Invoke(obj: null, parameters: new object[] { true });
        Method(name: "SurfaceQuery").Invoke(obj: null, parameters: new object[] { false });
        Method(name: "Refit").Invoke(obj: null, parameters: new object[] { 32767, 63 });
        Method(name: "Revision").Invoke(obj: null, parameters: new object[] { 127 });
        Method(name: "Consumer").Invoke(obj: null, parameters: new object[] { 0, true });
        Method(name: "Consumer").Invoke(obj: null, parameters: new object[] { 2, false });
        Method(name: "End").Invoke(obj: null, parameters: new object[] { 0L });
        Require(
            value: (int)profile.GetField(name: "calls", bindingAttr: flags).GetValue(obj: null) == 0
                && (int)profile.GetField(name: "misses", bindingAttr: flags).GetValue(obj: null)
                    == 0
                && (int)profile.GetField(name: "refits", bindingAttr: flags).GetValue(obj: null)
                    == 0,
            message: "Disabled hooks must not collect samples."
        );
        foreach (
            string field in new[]
            {
                "fittedParts",
                "refitReasons",
                "revisionReasons",
                "rebuilds",
                "reuses",
            }
        )
            Require(
                value: (
                    (int[])profile.GetField(name: field, bindingAttr: flags).GetValue(obj: null)
                ).All(n => n == 0),
                message: "Disabled diagnostics must not count parts or reasons."
            );
        Require(
            value: (int)
                profile.GetField(name: "envelopeHits", bindingAttr: flags).GetValue(obj: null) == 0,
            message: "Disabled profiling must not collect envelope hits."
        );
        var mount = assembly.GetType(
            name: "MoreSailwindSails.Sails.Spritsail.SpritsailMount",
            throwOnError: true
        );
        var draw = mount.GetMethod(name: "Draw", bindingAttr: flags);
        Require(
            value: draw.GetMethodBody()
                .ExceptionHandlingClauses.Any(c =>
                    c.Flags == ExceptionHandlingClauseOptions.Finally
                )
                && IlReader
                    .CalledMethods(method: draw)
                    .Any(m => m.DeclaringType == profile && m.Name == "End"),
            message: "Timing scopes must close on early returns and exceptions."
        );
        foreach (var method in profile.GetMethods(bindingAttr: flags | BindingFlags.DeclaredOnly))
            Require(
                value: IlReader
                    .CalledMethods(method: method)
                    .All(m =>
                        m.DeclaringType.Name
                            is not ("Cloth" or "Sail" or "Rigidbody" or "HingeJoint" or "Transform")
                    ),
                message: "Mount profiling and bypass must not mutate rig mechanics."
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
            "PASS: mount profiling defaults off, disabled hooks collect nothing, scopes finalize, and diagnostic modes avoid sail physics; live timing and A/B require the game."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message: message);
    }
}
