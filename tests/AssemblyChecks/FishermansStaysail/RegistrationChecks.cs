using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.FishermansStaysail;

internal static class RegistrationChecks
{
    internal static void Run(Assembly assembly)
    {
        const string family = "MoreSailwindSails.Sails.FishermansStaysail.";
        const BindingFlags all =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.Instance;
        Type Type(string name) => assembly.GetType(family + name, true);
        var prefab = Type("FishermansStaysailPrefab");
        var shapeBase = Type("FishermansStaysailShape");
        var register = prefab.GetMethod("Register", all);
        var parameters = register.GetParameters();
        if (
            parameters.Length != 4
            || parameters[0].ParameterType.Name != "PrefabsDirectory"
            || parameters[1].ParameterType.FullName != "UnityEngine.GameObject"
            || parameters[2].ParameterType != typeof(int)
            || parameters[3].ParameterType != typeof(string)
            || (int)prefab.GetField("SourceIndex", all).GetRawConstantValue() != 110
            || (float)prefab.GetField("TemplateHeadSlope", all).GetRawConstantValue() != 20f
        )
            throw new Exception(
                "Staysail registration must own the donor and template slope, accepting only the mark's identity and shape."
            );

        foreach (string mark in new[] { "MkA", "MkB", "MkC" })
        {
            var definition = Type(mark + ".FishermansStaysail" + mark);
            var shape = Type(mark + ".FishermansStaysail" + mark + "Shape");
            var baked = Type(mark + ".FishermansStaysail" + mark + "BakedMesh");
            var registrationCalls = CalledMethods(definition.GetMethod("Register", all)).ToArray();
            var factoryCall = registrationCalls.Single(m =>
                m.DeclaringType == prefab && m.Name == "Register"
            );
            if (
                !factoryCall.IsGenericMethod
                || factoryCall.GetGenericArguments().Single() != shape
                || definition.GetField("SourceIndex", all) != null
            )
                throw new Exception(
                    mark
                        + " must register through its own shape without a separate factory or donor."
                );

            var cutCalls = CalledMethods(shape.GetMethod("Create", all)).ToArray();
            if (
                cutCalls.Length != 2
                || cutCalls[0].DeclaringType != baked
                || cutCalls[0].Name != "Create"
                || cutCalls[1].DeclaringType != Type("FishermansStaysailMeshFitting")
                || cutCalls[1].Name != "Fit"
                || assembly.GetType(family + mark + ".FishermansStaysail" + mark + "Geometry")
                    != null
            )
                throw new Exception(
                    mark + " shape must fit its bake directly, without a geometry wrapper."
                );

            var prefix = shape.GetProperty("ObjectPrefix", all).GetMethod;
            if (
                !Instructions(prefix)
                    .Any(i =>
                        i.Code == OpCodes.Ldstr && Equals(i.Operand, "FishermansStaysail" + mark)
                    )
            )
                throw new Exception(mark + " owned-asset prefix changed.");

            var construction = Instructions(register.MakeGenericMethod(shape))
                .Where(i => i.Code != OpCodes.Nop)
                .ToArray();
            int Call(Type type, string name) =>
                Array.FindIndex(
                    construction,
                    i => i.Operand is MethodBase m && m.DeclaringType == type && m.Name == name
                );
            int inactive = Array.FindIndex(
                construction,
                i =>
                    i.Operand is MethodBase m
                    && m.DeclaringType.FullName == "UnityEngine.GameObject"
                    && m.Name == "SetActive"
            );
            int clone = Array.FindIndex(
                construction,
                i =>
                    i.Operand is MethodBase m
                    && m.DeclaringType.FullName == "UnityEngine.Object"
                    && m.Name == "Instantiate"
            );
            int attach = Array.FindIndex(
                construction,
                i =>
                    i.Operand is MethodBase m
                    && m.Name == "AddComponent"
                    && m.IsGenericMethod
                    && m.GetGenericArguments().Single() == shape
            );
            int create = Call(shapeBase, "Create");
            int configure = Call(Type("FishermansStaysailRig"), "Configure");
            int appearance = Call(Type("FishermansStaysailAppearance"), "Configure");
            if (
                inactive < 1
                || construction[inactive - 1].Code != OpCodes.Ldc_I4_0
                || clone <= inactive
                || attach <= clone
                || create <= attach
                || Call(shapeBase, "get_ObjectPrefix") <= attach
                || configure <= create
                || appearance <= configure
            )
                throw new Exception(
                    mark
                        + " template must attach its shape under an inactive parent before creating geometry and configuring the rig/appearance."
                );
        }

        var initialize = CalledMethods(
                Type("FishermansStaysailRig").GetMethod("InitializeCut", all)
            )
            .ToArray();
        if (
            !initialize.Any(m => m.DeclaringType == shapeBase && m.Name == "Create")
            || !initialize.Any(m => m.DeclaringType == shapeBase && m.Name == "get_ObjectPrefix")
        )
            throw new Exception(
                "Installed cuts and asset names must use the same shape definition as templates."
            );
        Console.WriteLine(
            "PASS (structural): all three staysail registrations select their own shape/cut/prefix, share donor 110 and retain inactive template construction; installed cuts use the same shape interface. Unity registration is not executed."
        );
    }
}
