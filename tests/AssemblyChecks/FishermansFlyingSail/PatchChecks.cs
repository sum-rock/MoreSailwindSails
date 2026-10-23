using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.FishermansFlyingSail;

internal static class PatchChecks
{
    internal static void Run(Assembly assembly)
    {
        const string family = "MoreSailwindSails.Sails.FishermansFlyingSail.";
        const BindingFlags methods = BindingFlags.Static | BindingFlags.NonPublic;
        var geometry = assembly.GetType(family + "FishermansFlyingSailGeometry", true);
        if (
            !CalledMethods(geometry.GetMethod("Create", methods))
                .Any(m =>
                    m.DeclaringType.Name == "FishermansFlyingSailBakedMesh" && m.Name == "Create"
                )
            || geometry.GetMethod("RestPoint", methods) != null
            || !CalledMethods(
                    assembly
                        .GetType(family + "FishermansFlyingSailPrefab", true)
                        .GetMethod("Register", methods)
                )
                .Any(m => m.DeclaringType == geometry && m.Name == "Create")
        )
            throw new Exception(
                "Flying Sail registration must construct its rest mesh from the bake."
            );
        SheetVisualChecks.Run(assembly);
        var travelPatch = assembly.GetType(
            "MoreSailwindSails.Sails.FishermansFlyingSail.Patches.FishermansFlyingSailTravelPatch"
        );
        var travelTarget = travelPatch.GetCustomAttribute<HarmonyPatch>().info;
        var travelPrefix = travelPatch.GetMethod(
            "Prefix",
            BindingFlags.Static | BindingFlags.NonPublic
        );
        var travelPostfix = travelPatch.GetMethod(
            "Postfix",
            BindingFlags.Static | BindingFlags.NonPublic
        );
        if (
            travelTarget.declaringType.Name != "JibAngleMaster"
            || travelTarget.methodName != "Update"
            || !travelPrefix.IsDefined(typeof(HarmonyPrefix))
            || !travelPostfix.IsDefined(typeof(HarmonyPostfix))
        )
            throw new Exception(
                "Travel protection must surround the native sheet and sway update."
            );
        var prefixCalls = CalledMethods(travelPrefix).ToArray();
        var postfixCalls = CalledMethods(travelPostfix).ToArray();
        if (
            !prefixCalls.Any(m =>
                m.Name == "GetComponent"
                && m.IsGenericMethod
                && m.GetGenericArguments().Single().FullName
                    == "MoreSailwindSails.Sails.FishermansFlyingSail.FishermansFlyingSailRig"
            )
            || !prefixCalls.Any(m =>
                m.DeclaringType.FullName
                    == "MoreSailwindSails.Sails.FishermansFlyingSail.FishermansFlyingSailTravel"
                && m.Name == "Clamp"
            )
            || !postfixCalls.Any(m =>
                m.DeclaringType.FullName
                    == "MoreSailwindSails.Sails.FishermansFlyingSail.FishermansFlyingSailTravel"
                && m.Name == "ConstrainHinge"
            )
            || !postfixCalls.Any(m =>
                m.DeclaringType.FullName == "UnityEngine.HingeJoint" && m.Name == "set_limits"
            )
        )
            throw new Exception("Missing fisherman scoping or live hinge travel enforcement.");
        foreach (var called in prefixCalls.Concat(postfixCalls))
            if (
                called.DeclaringType.FullName is "UnityEngine.Cloth" or "UnityEngine.Transform"
                || called.Name == "ResetHingeRestingRot"
            )
                throw new Exception(
                    "Travel protection must not reset cloth or snap the sail transform."
                );
        Console.WriteLine(
            "PASS: fisherman travel wraps native sheet/sway Update and constrains the hinge without resetting cloth or transforms."
        );

        // Cloth mesh assignment belongs to inactive prefab construction. Replacing
        // a live Cloth renderer's mesh caused the 0.7.11 detach/reset regression even
        // though the two meshes passed all pure geometry checks.
        var rigType = assembly.GetType(
            "MoreSailwindSails.Sails.FishermansFlyingSail.FishermansFlyingSailRig"
        );
        foreach (
            var method in rigType.GetMethods(
                BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.DeclaredOnly
            )
        )
        {
            foreach (var called in CalledMethods(method))
                if (
                    (
                        called.Name == "set_sharedMesh"
                        && called.DeclaringType.FullName == "UnityEngine.SkinnedMeshRenderer"
                    )
                    || (
                        called.Name == "set_uv"
                        && called.DeclaringType.FullName == "UnityEngine.Mesh"
                    )
                )
                    throw new Exception(
                        $"Live cloth mesh replacement in {method.Name}; only prefab construction may assign it."
                    );
        }
        Console.WriteLine("PASS: installed sail callbacks retain their initialized cloth mesh.");

        var shapeUpdate = rigType.GetMethod(
            "UpdateShapeBones",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        if (shapeUpdate == null)
            throw new Exception("Missing bone-driven billow update.");
        foreach (var called in CalledMethods(shapeUpdate))
            if (
                called.DeclaringType.FullName == "UnityEngine.Cloth"
                || called.Name
                    is "set_sharedMesh"
                        or "set_bones"
                        or "set_bindposes"
                        or "set_localScale"
                        or "set_localRotation"
                        or "set_enabled"
            )
                throw new Exception(
                    "Billow must move existing bones without resetting or replacing cloth or reflecting transforms."
                );
        Console.WriteLine(
            "PASS: billow update only moves existing shaping transforms; cloth lifecycle and transform scales remain untouched."
        );

        var tiesType = assembly.GetType(
            "MoreSailwindSails.Sails.FishermansFlyingSail.FishermansFlyingSailLuffTies"
        );
        foreach (
            var method in tiesType.GetMethods(
                BindingFlags.Instance
                    | BindingFlags.NonPublic
                    | BindingFlags.Public
                    | BindingFlags.DeclaredOnly
            )
        )
        foreach (var called in CalledMethods(method))
            if (
                called.DeclaringType.FullName is "UnityEngine.Cloth" or "UnityEngine.HingeJoint"
                || called.Name
                    is "set_sharedMesh"
                        or "set_localRotation"
                        or "set_localScale"
                        or "set_rotation"
                        or "set_bones"
                        or "set_bindposes"
            )
                throw new Exception(
                    "Visual luff ties must not alter cloth, bone rotation, scaling or sheet physics."
                );
        Console.WriteLine(
            "PASS (IL): fixed luff tie callbacks only draw/hide endpoints; no cloth, hinge or skin-orientation mutation. Unity rope rendering remains a runtime check."
        );

        Shared.NativeControlBindingChecks.Run(
            assembly,
            "MoreSailwindSails.Sails.FishermansFlyingSail.Patches.FishermansFlyingSailControlsPatch",
            "Flying Sail"
        );
    }
}
