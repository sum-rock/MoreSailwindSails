using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.FishermansStaysail.MkA;

internal static class PatchChecks
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
        MethodInfo Method(string type, string name) => Type(type).GetMethod(name, all);
        MethodInfo Patch(string name, string method = "Prefix") =>
            Method("Patches.FishermansStaysail" + name + "Patch", method);
        if (
            (int)
                Type("MkA.FishermansStaysailMkA").GetField("PrefabIndex", all).GetRawConstantValue()
                != 401
            || (string)
                Type("MkA.FishermansStaysailMkA").GetField("DisplayName", all).GetRawConstantValue()
                != "Fisherman's Staysail Mk.A"
        )
            throw new Exception("Mk.A save identity changed.");
        var registration = Patch("Registration", "Postfix");
        if (Method("FishermansStaysailGeometry", "Create") != null)
            throw new Exception("Procedural staysail mesh creation must be removed.");
        if (
            !CalledMethods(Method("FishermansStaysailRig", "InitializeCut"))
                .Any(m => m.Name == "set_uv")
        )
            throw new Exception("Fitted staysail UVs must reach the initialized mesh.");
        if (
            !registration
                .GetCustomAttribute<HarmonyAfter>()
                .info.after.Contains("com.nandbrew.shipyardexpansion")
            || !registration
                .GetCustomAttribute<HarmonyBefore>()
                .info.before.Contains("NatoriusG.AllSailsAllShipyards")
        )
            throw new Exception("Mk.A registration must follow SE and precede All Sails.");
        var resolve = CalledMethods(Method("FishermansStaysailRigging", "TryResolve")).ToArray();
        if (
            !resolve.Any(m =>
                m.DeclaringType.Name == "FishermansStayRegistry" && m.Name == "TryFind"
            )
        )
            throw new Exception("Mk.A must resolve exact registered stays.");
        if (
            resolve.Any(m => m.DeclaringType.Namespace == "MoreSailwindSails.BoatConfiguration")
            || !resolve.Any(m =>
                m.DeclaringType.Name == "FishermansStay" && m.Name == "get_Available"
            )
            || !CalledMethods(Method("FishermansStaysailRigging+MountPair", "get_Active"))
                .Any(m => m.DeclaringType.Name == "FishermansStay" && m.Name == "get_Available")
        )
            throw new Exception(
                "Staysail binding must use stay availability without ancestry lookup."
            );
        var dependsOn = Method("FishermansStaysailRigging", "DependsOn");
        var dependencyFields = Instructions(dependsOn)
            .Select(i => i.Operand)
            .OfType<FieldInfo>()
            .ToArray();
        if (
            !dependencyFields.Any(f =>
                f.DeclaringType.Name == "FishermansStayReferences" && f.Name == "Required"
            )
            || !dependencyFields.Any(f =>
                f.DeclaringType.Name == "FishermansStay" && f.Name == "Option"
            )
        )
            throw new Exception(
                "Staysail dependencies must retain the occupied stay and its explicit prerequisites."
            );
        // Sh'ba's topmast options point childMast at their lower masts. Treating
        // those references as dependencies forces unselected topmasts on during
        // SetOptionEnabled(false), and incorrectly blocks their removal.
        if (
            dependencyFields.Any(f =>
                f.DeclaringType.Name == "BoatPartOption" && f.Name == "childMast"
            )
        )
            throw new Exception(
                "Staysail support guards must not infer prerequisites from childMast references."
            );
        foreach (string patch in new[] { "SupportRemoval", "SupportPreview" })
        {
            var patchType = Type("Patches.FishermansStaysail" + patch + "Patch");
            var methods = patchType
                .GetMethods(all | BindingFlags.DeclaredOnly)
                .Concat(
                    patchType
                        .GetNestedTypes(all)
                        .SelectMany(t => t.GetMethods(all | BindingFlags.DeclaredOnly))
                );
            if (!methods.SelectMany(CalledMethods).Any(m => m == dependsOn))
                throw new Exception("Staysail support guard lost dependency checking: " + patch);
        }
        foreach (string patch in new[] { "InstallError", "Fit" })
            if (
                !CalledMethods(Patch(patch))
                    .Any(m =>
                        m.Name == "InstallError"
                        && m.DeclaringType.Name == "FishermansStaysailRigging"
                    )
            )
                throw new Exception("Missing physical mast fit guard: " + patch);
        // Native installation must still reject invalid custom fits through our height hook.
        var fitTarget = Patch("Fit").DeclaringType.GetCustomAttribute<HarmonyPatch>().info;
        var nativeFit = fitTarget.declaringType.GetMethod(fitTarget.methodName, all);
        var nativeInstall = fitTarget.declaringType.GetMethod("InstallSail", all);
        if (!CalledMethods(nativeInstall).Any(m => m == nativeFit))
            throw new Exception("Native installation bypasses the custom staysail fit check.");
        if (assembly.GetType(family + "Patches.FishermansStaysailInstallGuardPatch") != null)
            throw new Exception("The redundant staysail installation guard returned.");
        Shared.NativeControlBindingChecks.Run(
            assembly,
            family + "Patches.FishermansStaysailControlsPatch",
            "Staysail family (Mk.A/Mk.B/Mk.C)"
        );
        if (!CalledMethods(Patch("Travel", "Postfix")).Any(m => m.Name == "ConstrainHinge"))
            throw new Exception("Mk.A travel is not constrained after native sway.");
        if (
            Method("FishermansStaysailReefing", "Sample") != null
            || Method("FishermansStaysailRig", "UpdateShapeBones") != null
        )
            throw new Exception("Procedural staysail animation returned.");
        var newSail = Patch("NewSail", "Postfix");
        if (
            !CalledMethods(newSail)
                .Any(m => m.DeclaringType.Name == "Sail" && m.Name == "LoadScale")
            || !newSail
                .GetCustomAttribute<HarmonyAfter>()
                .info.after.Contains("com.nandbrew.shipyardexpansion")
        )
            throw new Exception(
                "New Mk.A size must use native scaling after shipyard initialization."
            );
        foreach (
            var type in assembly
                .GetTypes()
                .Where(t => t.Namespace?.StartsWith(family.TrimEnd('.')) == true)
        )
        foreach (var method in type.GetMethods(all | BindingFlags.DeclaredOnly))
            if (
                method != newSail
                && CalledMethods(method)
                    .Any(m => m.DeclaringType.Name == "Sail" && m.Name == "LoadScale")
            )
                throw new Exception(
                    "Default size must not override existing sail sizes: " + type.Name
                );
        if (
            Method("FishermansStaysailGeometry", "RenderState") != null
            || Type("FishermansStaysailRig").GetField("ReefedRenderer", all) != null
        )
            throw new Exception("Staysails must retain a single animated panel renderer.");
        foreach (string method in new[] { "PoseAnimation", "LateUpdate" })
            if (
                !CalledMethods(Method("FishermansStaysailRig", method))
                    .Any(m =>
                        m.Name == "IsFurled"
                        && m.DeclaringType == Type("FishermansStaysailGeometry")
                    )
            )
                throw new Exception(
                    "Sheet attachment and visibility must share the fully-furled boundary."
                );
        var poseCalls = CalledMethods(Method("FishermansStaysailRig", "PoseAnimation")).ToArray();
        if (
            !poseCalls.Any(m => m.Name == "FurledAftPoint")
            || !poseCalls.Any(m => m.Name == "get_ForePoint")
            || !poseCalls.Any(m => m.Name == "get_AftPoint")
            || poseCalls.Count(m => m.Name == "set_position") != 2
            || !CalledMethods(Method("FishermansStaysailRig", "LateUpdate"))
                .Any(m => m.Name == "get_ForePoint")
        )
            throw new Exception(
                "Furled bundle and both rope leaves must follow the physical stay."
            );
        var bundleCalls = CalledMethods(Method("FishermansStaysailReefing", "DrawBundle"))
            .ToArray();
        if (
            !bundleCalls.Any(m => m.Name == "set_enabled")
            || !bundleCalls.Any(m => m.Name == "set_sharedMaterial")
            || !CalledMethods(Method("FishermansStaysailRig", "LateUpdate"))
                .Any(m =>
                    m.DeclaringType == Type("FishermansStaysailReefing") && m.Name == "DrawBundle"
                )
        )
            throw new Exception(
                "The fitted native bundle must track visibility and the current sail material."
            );
        if (
            Type("FishermansStaysailTravel").GetField("MaximumAngle", all) != null
            || Method("FishermansStaysailTravel", "Clamp") != null
        )
            throw new Exception("Staysail trim must retain fitted limits without a fixed cap.");
        foreach (
            var method in Type("FishermansStaysailRig").GetMethods(all | BindingFlags.DeclaredOnly)
        )
        {
            var calls = CalledMethods(method).ToArray();
            if (
                method.Name != "Configure"
                && method.Name != "InitializeCut"
                && calls.Any(m =>
                    m.Name is "set_sharedMesh" or "set_bindposes" or "set_bones" or "set_uv"
                )
            )
                throw new Exception("Live staysail mesh replacement in " + method.Name);
        }
        if (
            !CalledMethods(Method("FishermansStaysailRig", "InitializeCut"))
                .Any(m => m.DeclaringType.Name == "FishermansStaysailShape" && m.Name == "Create")
        )
            throw new Exception("The family rig must request the cut from its mark.");
        var lateUpdate = CalledMethods(Method("FishermansStaysailRig", "LateUpdate")).ToArray();
        if (!lateUpdate.Any(m => m.Name == "PoseAnimation"))
            throw new Exception("All staysails must use authored poses.");
        var flutterCalls = CalledMethods(Method("FishermansStaysailRig", "UpdateFlutter"))
            .ToArray();
        if (
            !poseCalls.Any(m => m.Name == "UpdateFlutter")
            || !poseCalls.Any(m =>
                m.Name == "VisualVertex" && m.DeclaringType == Type("FishermansStaysailFlutter")
            )
            || !flutterCalls.Any(m => m.Name == "GetCapturedWindFraction")
            || !flutterCalls.Any(m => m.Name == "GetCurrentShadowMult")
            || flutterCalls.Any(m => m.Name == "GetCapturedForceFraction")
        )
            throw new Exception(
                "Staysail visual flutter must use native wind capture and shadowing."
            );
        var instructions = Instructions(Method("FishermansStaysailRig", "LateUpdate")).ToArray();
        int clothSetter = Array.FindIndex(
            instructions,
            i =>
                i.Operand is MethodInfo m
                && m.DeclaringType.Name == "Cloth"
                && m.Name == "set_enabled"
        );
        if (
            clothSetter < 1
            || instructions[clothSetter - 1].Code != System.Reflection.Emit.OpCodes.Ldc_I4_0
        )
            throw new Exception("Authored staysails must keep the native Cloth solver disabled.");
        foreach (string cut in new[] { "A", "B", "C" })
            if (
                !CalledMethods(
                        Method(
                            "Mk" + cut + ".FishermansStaysailMk" + cut + "Shape",
                            "CreateAnimation"
                        )
                    )
                    .Any(m =>
                        m.DeclaringType.Name == "FishermansStaysailMk" + cut + "BakedMesh"
                        && m.Name == "CreateAnimation"
                    )
            )
                throw new Exception("Missing authored animation for Mk." + cut);
        if (
            assembly.GetType(family + "FishermansStaysailSupportLine") != null
            || Type("FishermansStaysailRig").GetField("HalyardAttachment", all)?.FieldType.FullName
                != "UnityEngine.Transform"
            || Method("FishermansStaysailRigging", "UpdateHalyard")
                .GetParameters()
                .Single()
                .ParameterType.FullName != "UnityEngine.Transform"
        )
            throw new Exception(
                "Mk.A must use one real aft halyard leaf, not decorative upper sheets."
            );
        var halyard = CalledMethods(Method("FishermansStaysailRigging", "UpdateHalyard")).ToArray();
        if (
            halyard.Any(m => m.Name == "get_ForePoint")
            || !halyard.Any(m => m.Name == "MastAxis")
            || !halyard.Any(m => m.Name == "AttachControls")
        )
            throw new Exception(
                "Aft halyard route must refresh its controls and use the aft mast axis."
            );
        Console.WriteLine(
            "PASS: Mk.A prefab identity, stay membership, fit guards, full-range authored playback, native control binding and fixed-mesh lifecycle."
        );
    }
}
