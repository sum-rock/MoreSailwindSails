using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Spritsail;

// Guards the opt-in visual revision integration while mechanical/Cloth updates remain live.
internal static class VisualCacheChecks
{
    private const BindingFlags All =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    internal static void Run(Assembly assembly)
    {
        Type Type(string name) =>
            assembly.GetType(name: "MoreSailwindSails." + name, throwOnError: true);
        MethodInfo Method(string name, string method) =>
            Type(name: name).GetMethod(name: method, bindingAttr: All);
        foreach (
            var target in new[]
            {
                ("Sails.Spritsail.SpritsailMount", "Draw", "SpritsailMountGeometry", "Fit"),
                ("Sails.Spritsail.SpritsailSpar", "Pose", "Quaternion", "LookRotation"),
                (
                    "Sails.Spritsail.SpritsailSnotter",
                    "Pose",
                    "SpritsailSpritGeometry",
                    "RadiusAtPivot"
                ),
            }
        )
        {
            var code = IlReader
                .Instructions(method: Method(name: target.Item1, method: target.Item2))
                .ToArray();
            int needs = Array.FindIndex(
                array: code,
                match: i =>
                    i.Operand is MethodInfo m
                    && m.DeclaringType.Name == "SailVisualCache"
                    && m.Name == "Needs"
            );
            int work = Array.FindIndex(
                array: code,
                match: i =>
                    i.Operand is MethodInfo m
                    && m.DeclaringType.Name == target.Item3
                    && m.Name == target.Item4
            );
            Require(
                value: needs >= 0
                    && work > needs
                    && code.Skip(needs + 1)
                        .Take(work - needs - 1)
                        .Any(i => i.Code.FlowControl == FlowControl.Cond_Branch),
                message: "Expensive visual work must follow a revision cache branch: "
                    + target.Item1
            );
            Require(
                value: IlReader
                    .CalledMethods(method: Method(name: target.Item1, method: target.Item2))
                    .Any(m => m.DeclaringType.Name == "SailVisualCache" && m.Name == "Commit"),
                message: "Visual consumers must acknowledge their own successful updates."
            );
        }
        foreach (string family in new[] { "LooseFootedSpritsail", "BoomedSpritsail" })
        {
            var calls = IlReader
                .CalledMethods(
                    method: Method(
                        name: "Sails.Spritsail." + family + "." + family + "Rig",
                        method: "LateUpdate"
                    )
                )
                .ToArray();
            Require(
                value: calls.Count(m =>
                    m.DeclaringType.Name == "SpritsailVisualTriggers" && m.Name == "Update"
                ) == 1
                    && calls.Any(m => m.Name == "UpdateShapeBones")
                    && calls.Any(m => m.Name == "RefreshAerodynamics"),
                message: "Each rig samples visual state once and retains live Cloth/aerodynamic posing."
            );
        }
        var trigger = IlReader
            .Instructions(
                method: Method(name: "Sails.Spritsail.SpritsailVisualTriggers", method: "Update")
            )
            .ToArray();
        Require(
            value: trigger.Any(i => i.Operand is FieldInfo f && f.Name == "currentLength")
                && trigger.Any(i => i.Operand is FieldInfo f && f.Name == "currentUnroll")
                && !trigger.Any(i =>
                    i.Operand is FieldInfo f && f.Name is "apparentWind" or "currentAngle"
                )
                && !trigger.Any(i =>
                    i.Operand is MethodInfo m && m.Name is "get_position" or "get_lossyScale"
                ),
            message: "Visual revisions must use paid-out controls and local fitting values, excluding wind, sway angle and world positions."
        );
        var snotter = IlReader.CalledMethods(
            method: Method(name: "Sails.Spritsail.SpritsailSnotter", method: "Pose")
        );
        Require(
            value: snotter.Count(m =>
                m.DeclaringType.Name == "RoutedRope" && m.Name == "DrawCached"
            ) == 2,
            message: "Snotter purchase and collar must both use cached tube drawing."
        );
        var cached = IlReader
            .CalledMethods(method: Method(name: "Visuals.RoutedRope", method: "DrawCached"))
            .ToArray();
        Require(
            value: cached.Any(m => m.Name == "DrawMesh")
                && cached.Any(m => m.Name == "SetVisible")
                && !cached.Any(m =>
                    m.DeclaringType.Name == "RoutedRopeGeometry" && m.Name == "Pose"
                ),
            message: "Cached rope drawing must reuse the mesh; rebuilds retain the existing tube/material fallback."
        );
        Console.WriteLine(
            "PASS: shared visual revision guards mount/sprit/snotter, both rigs retain live mechanics, triggers exclude environment/sway, and snotter routes use opt-in cached drawing; Unity rendering remains untested."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message: message);
    }
}
