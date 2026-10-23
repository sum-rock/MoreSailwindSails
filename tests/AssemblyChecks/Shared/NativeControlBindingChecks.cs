// Verifies that custom sails participate in native mast ordering and only restore routing afterwards.
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace MoreSailwindSails.Tests.AssemblyChecks.Shared;

internal static class NativeControlBindingChecks
{
    internal static void Run(Assembly assembly, string patchName, string feature)
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        var patch = assembly.GetType(patchName, true);
        var target = patch.GetCustomAttribute<HarmonyPatch>().info;
        var postfix = patch.GetMethod("Postfix", flags);
        if (
            target.declaringType.FullName != "Mast"
            || target.methodName != "UpdateControllerAttachments"
            || postfix == null
            || !postfix.IsDefined(typeof(HarmonyPostfix))
            || patch.GetMethod("Prefix", flags) != null
            || patch.GetMethod("Finalizer", flags) != null
        )
            throw new Exception(feature + " must participate in native control binding.");
        if (
            IlReader
                .Instructions(postfix)
                .Any(i =>
                    i.Code == OpCodes.Stfld && i.Operand is FieldInfo field && field.Name == "sails"
                ) || !IlReader.CalledMethods(postfix).Any(method => method.Name == "AttachControls")
        )
            throw new Exception(
                feature + " routing hook mutates sail ownership or fails to refresh custom guides."
            );
        var native = target.declaringType.GetMethod(target.methodName);
        foreach (
            string field in new[] { "mastOrder", "reefWinch", "leftAngleWinch", "rightAngleWinch" }
        )
            if (
                !IlReader.Instructions(native).Any(i => i.Operand is FieldInfo f && f.Name == field)
            )
                throw new Exception("Installed native binding no longer uses " + field);
        if (!IlReader.CalledMethods(native).Any(method => method.Name == "UpdateSailOrder"))
            throw new Exception("Installed native binding no longer refreshes sail order.");
        Console.WriteLine(
            $"PASS (structural): {feature} native mast ordering and arrays, with custom routing postfix; mixed-sail binding remains a game check."
        );
    }
}
