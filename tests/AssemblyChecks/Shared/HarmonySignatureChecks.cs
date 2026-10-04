using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace MoreSailwindSails.Tests.AssemblyChecks.Shared;

internal static class HarmonySignatureChecks
{
    internal static void Run(Assembly assembly)
    {
        int count = 0;
        foreach (var type in assembly.GetTypes())
        {
            var attribute = type.GetCustomAttributes<HarmonyPatch>().SingleOrDefault();
            if (attribute == null)
                continue;
            var info = attribute.info;
            const BindingFlags all =
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic;
            var targets =
                info.declaringType == null
                    ? (IEnumerable<MethodBase>)
                        type.GetMethod("TargetMethods", all).Invoke(null, null)
                    : new[]
                    {
                        info.methodType == MethodType.Constructor
                            ? (MethodBase)
                                info.declaringType.GetConstructor(all, null, Type.EmptyTypes, null)
                            : info.declaringType.GetMethod(
                                info.methodName,
                                all | BindingFlags.DeclaredOnly
                            ) ?? info.declaringType.GetMethod(info.methodName, all),
                    };
            foreach (var target in targets)
            {
                if (target == null)
                    throw new Exception("Missing Harmony target: " + type.Name);
                foreach (
                    var patch in type.GetMethods(
                        BindingFlags.Static
                            | BindingFlags.NonPublic
                            | BindingFlags.Public
                            | BindingFlags.DeclaredOnly
                    )
                )
                {
                    if (
                        !patch.IsDefined(typeof(HarmonyPrefix))
                        && !patch.IsDefined(typeof(HarmonyPostfix))
                        && !patch.IsDefined(typeof(HarmonyFinalizer))
                    )
                        continue;
                    foreach (var parameter in patch.GetParameters())
                    {
                        Type actual = null;
                        if (parameter.Name.StartsWith("___"))
                            actual = target
                                .DeclaringType.GetField(parameter.Name.Substring(3), all)
                                ?.FieldType;
                        else if (parameter.Name == "__instance")
                            actual = target.DeclaringType;
                        else if (parameter.Name == "__result")
                            actual = (target as MethodInfo)?.ReturnType;
                        else if (parameter.Name == "__exception")
                            actual = typeof(Exception);
                        else if (parameter.Name == "__state")
                            actual = type.GetMethods(all)
                                .Single(m => m.IsDefined(typeof(HarmonyPrefix)))
                                .GetParameters()
                                .Single(p => p.Name == "__state")
                                .ParameterType;
                        else
                            actual = target
                                .GetParameters()
                                .SingleOrDefault(p => p.Name == parameter.Name)
                                ?.ParameterType;
                        var expected = parameter.ParameterType;
                        if (expected.IsByRef)
                            expected = expected.GetElementType();
                        if (actual != null && actual.IsByRef)
                            actual = actual.GetElementType();
                        if (
                            actual == null
                            || (
                                actual != expected
                                && !(
                                    parameter.Name == "__instance"
                                    && expected.IsAssignableFrom(actual)
                                )
                            )
                        )
                            throw new Exception(
                                $"Invalid injection: {type.Name}.{patch.Name}({parameter.Name})."
                            );
                    }
                }
                count++;
            }
        }
        if (count != 114)
            throw new Exception($"Expected all 114 patch targets, found {count}.");

        Console.WriteLine($"PASS: {count} Harmony targets and injected argument types.");
    }
}
