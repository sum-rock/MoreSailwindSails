using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Spritsail;

// Checks native binding contracts, exception restoration and separation from Fisherman's allocation.
internal static class NativeBindingChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags all =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.Instance;
        const string family = "MoreSailwindSails.Sails.Spritsail.";
        var patch = assembly.GetType(
            name: family + "Patches.SpritsailNativeControlsPatch",
            throwOnError: true
        );
        var prefix = patch.GetMethod(name: "Prefix", bindingAttr: all);
        var finalizer = patch.GetMethod(name: "Finalizer", bindingAttr: all);
        Require(
            condition: prefix.GetCustomAttribute<HarmonyPriority>().info.priority == Priority.Last
                && finalizer.GetCustomAttribute<HarmonyPriority>().info.priority == Priority.First,
            message: "Capture native slots after Fisherman filtering and restore controller references before its finalizers."
        );
        Require(
            condition: !IlReader
                .Instructions(method: prefix)
                .Any(i =>
                    i.Code == OpCodes.Stfld && i.Operand is FieldInfo field && field.Name == "sails"
                ),
            message: "The spritsail guard must not remove sails or shift native slots."
        );
        Require(
            condition: finalizer.IsDefined(attributeType: typeof(HarmonyFinalizer))
                && IlReader.CalledMethods(method: finalizer).Any(m => m.Name == "Restore"),
            message: "Restore suppressed references on success and exception paths."
        );
        var state = assembly.GetType(
            name: family + "SpritsailControlBindingState",
            throwOnError: true
        );
        var restore = state.GetMethod(name: "Restore", bindingAttr: all);
        foreach (
            string name in new[]
            {
                "reefController",
                "angleControllerMid",
                "angleControllerLeft",
                "angleControllerRight",
                "midRopeAttachment",
                "mastReefAttachment",
                "mastReefAttExtension",
            }
        )
            Require(
                condition: IlReader
                    .Instructions(method: restore)
                    .Any(i =>
                        i.Code == OpCodes.Stfld
                        && i.Operand is FieldInfo field
                        && field.Name == name
                    ),
                message: "Missing native-reference restoration: " + name
            );
        Require(
            condition: !IlReader
                .CalledMethods(method: restore)
                .Any(m => m.Name is "Destroy" or "DestroyImmediate"),
            message: "A failed binding must retain controller objects and their input state."
        );
        var native = Assembly
            .Load(assemblyString: "Assembly-CSharp")
            .GetType(name: "Mast", throwOnError: true);
        var binding = native.GetMethod(name: "UpdateControllerAttachments", bindingAttr: all);
        foreach (
            string name in new[]
            {
                "mastOrder",
                "squareSail",
                "reefWinch",
                "midAngleWinch",
                "leftAngleWinch",
                "rightAngleWinch",
                "mastReefAtt",
                "mastReefAttExtension",
            }
        )
            Require(
                condition: IlReader
                    .Instructions(method: binding)
                    .Any(i => i.Operand is FieldInfo field && field.Name == name),
                message: "Installed native binding contract changed: " + name
            );
        foreach (
            var type in assembly
                .GetTypes()
                .Where(t =>
                    t.FullName.StartsWith(value: family, comparisonType: StringComparison.Ordinal)
                )
        )
        foreach (var method in type.GetMethods(bindingAttr: all | BindingFlags.DeclaredOnly))
            Require(
                condition: !IlReader
                    .CalledMethods(method: method)
                    .Any(m =>
                        m.DeclaringType?.Namespace
                            is "MoreSailwindSails.Controls"
                                or "MoreSailwindSails.BoatRigs"
                    ),
                message: "Spritsails must not depend on boat profiles or Fisherman's control allocation: "
                    + type.Name
            );
        Console.WriteLine(
            "PASS (structural): native spritsail binding, slot capture ordering, no list filtering, reference restoration, retained controllers and profile independence. Unity binding/exception recovery not executed."
        );
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message: message);
    }
}
