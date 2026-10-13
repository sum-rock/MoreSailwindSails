// Checks optional SE preparation and the guarded prefix structure without running Unity.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.Compatibility;

internal static class ShipyardExpansionTextureChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags methods = BindingFlags.Static | BindingFlags.NonPublic;
        var patch = assembly.GetType(
            name: "MoreSailwindSails.Compatibility.Patches.ShipyardExpansionTexturesPatch",
            throwOnError: true
        );
        var prepare = patch.GetMethod(name: "Prepare", bindingAttr: methods);
        var targetMethod = patch.GetMethod(name: "TargetMethod", bindingAttr: methods);
        var cleanup = patch.GetMethod(name: "Cleanup", bindingAttr: methods);
        if (
            (bool)prepare.Invoke(obj: null, parameters: null)
            || targetMethod.Invoke(obj: null, parameters: null) != null
        )
            throw new Exception("Missing SE must skip the optional patch.");
        foreach (var error in new Exception[] { null, new MissingFieldException("changed SE API") })
            if (cleanup.Invoke(obj: null, parameters: new object[] { error }) != null)
                throw new Exception("SE patch cleanup must suppress failures.");

        var prefix = patch.GetMethod(name: "Prefix", bindingAttr: methods);
        var calls = CalledMethods(method: prefix).OfType<MethodInfo>().ToArray();
        foreach (string rig in new[] { "FishermansFlyingSailRig", "FishermansStaysailRig" })
            if (
                !calls.Any(method =>
                    method.Name == "GetComponent"
                    && method.IsGenericMethod
                    && method.GetGenericArguments()[0].Name == rig
                )
            )
                throw new Exception(
                    "SE restriction must be scoped to both Fisherman rig families."
                );
        if (
            prefix.ReturnType != typeof(void)
            || prefix.GetParameters()[1].Name != "___textureIndex"
            || prefix.GetParameters()[1].ParameterType != typeof(string).MakeByRefType()
        )
            throw new Exception(
                "The SE prefix must allow its method to run and synchronize its selected texture field."
            );

        // The full prefix requires Unity. Inspect the write order and catch boundary instead.
        var instructions = Instructions(method: prefix).ToArray();
        int choices = Array.FindIndex(
            array: instructions,
            match: i =>
                i.Operand is MethodInfo method
                && method.DeclaringType == typeof(FieldInfo)
                && method.Name == "SetValue"
        );
        int selected = Array.FindIndex(
            array: instructions,
            match: i => i.Code == OpCodes.Stind_Ref
        );
        var catchBlock = prefix.GetMethodBody().ExceptionHandlingClauses.Single();
        if (
            choices < 0
            || selected <= choices
            || catchBlock.Flags != ExceptionHandlingClauseOptions.Clause
            || catchBlock.CatchType != typeof(Exception)
            || catchBlock.TryOffset != 0
            || !CalledMethods(method: prefix)
                .Any(method => method.IsConstructor && method.DeclaringType == typeof(List<string>))
            || !calls.Contains(value: cleanup)
        )
            throw new Exception(
                "SE must try a fresh string list before changing selection and catch failures in the prefix."
            );

        // Exercise the loaded-type lookup without loading SE or constructing a Unity object.
        var module = AssemblyBuilder
            .DefineDynamicAssembly(
                name: new AssemblyName("ShipyardExpansionTextureContracts"),
                access: AssemblyBuilderAccess.Run
            )
            .DefineDynamicModule(name: "Contracts");
        var fakeType = module.DefineType(
            name: "ShipyardExpansion.Scripts.SailTextureChanger",
            attr: TypeAttributes.Public,
            parent: prefix.GetParameters()[0].ParameterType
        );
        fakeType.DefineField(
            fieldName: "textureIndex",
            type: typeof(string),
            attributes: FieldAttributes.Public
        );
        var fakeUpdate = fakeType.DefineMethod(
            name: "UpdateMaterial",
            attributes: MethodAttributes.Private,
            returnType: typeof(void),
            parameterTypes: Type.EmptyTypes
        );
        fakeUpdate.GetILGenerator().Emit(opcode: OpCodes.Ret);
        var expectedTarget = fakeType
            .CreateType()
            .GetMethod(
                name: "UpdateMaterial",
                bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic
            );
        for (int call = 0; call < 2; call++)
            if (
                !(bool)prepare.Invoke(obj: null, parameters: null)
                || !Equals(
                    objA: targetMethod.Invoke(obj: null, parameters: null),
                    objB: expectedTarget
                )
            )
                throw new Exception(
                    "Repeated Harmony preparation must resolve the same loaded SE target."
                );

        var startup = assembly
            .GetType(name: "MoreSailwindSails.Plugin", throwOnError: true)
            .GetMethod(name: "Awake", bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic);
        var startupCalls = CalledMethods(method: startup).ToArray();
        if (
            !startupCalls.Any(method =>
                method.DeclaringType == typeof(Harmony) && method.Name == "PatchAll"
            ) || startupCalls.Any(method => method.DeclaringType == patch)
        )
            throw new Exception(
                "The optional SE patch must be discovered by PatchAll without manual installation."
            );
        Console.WriteLine(
            "PASS: optional SE target preparation and cleanup; prefix family scope, fresh List<string>, write order and catch boundary inspected; live type-change recovery requires Unity."
        );
    }
}
