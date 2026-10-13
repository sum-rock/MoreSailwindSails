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
        Shared.ControlRestorationChecks.Run(
            assembly,
            family + "Patches.FishermansStaysailControlsPatch",
            "Staysail family (Mk.A/Mk.B/Mk.C)"
        );
        if (!CalledMethods(Patch("Travel", "Postfix")).Any(m => m.Name == "ConstrainHinge"))
            throw new Exception("Mk.A travel is not constrained after native sway.");
        if (
            !CalledMethods(Method("FishermansStaysailReefing", "Sample"))
                .Any(m =>
                    m.Name == "SampleAnimation"
                    && m.DeclaringType.FullName == "UnityEngine.AnimationClip"
                )
        )
            throw new Exception("Mk.A is not sampling the actual native reef animation.");
        if (
            Type("FishermansStaysailReefing").GetProperty("Spread", all) != null
            || Type("FishermansStaysailReefing").GetField("FoldBone", all) != null
        )
            throw new Exception("Unused reef spread sampling returned.");
        var shaping = CalledMethods(Method("FishermansStaysailRig", "UpdateShapeBones")).ToArray();
        foreach (string getter in new[] { "get_Active", "get_ForePoint", "get_ForeAxis" })
            if (shaping.Count(m => m.Name == getter) != 1)
                throw new Exception("Resolve the luff frame once per bone update: " + getter);
        if (shaping.Any(m => m.Name == "LuffPoint"))
            throw new Exception("Bone updates must project with the captured mast frame.");
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
        var draw = CalledMethods(Method("FishermansStaysailReefing", "DrawBundle")).ToArray();
        if (
            !draw.Any(m => m.Name == "set_enabled")
            || !draw.Any(m => m.Name == "set_sharedMaterial")
        )
            throw new Exception("Native resting bundle visibility/material not maintained.");
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
            if (
                method.Name == "UpdateShapeBones"
                && calls.Any(m =>
                    m.DeclaringType.FullName == "UnityEngine.Cloth"
                    || m.Name is "set_localScale" or "set_localRotation"
                )
            )
                throw new Exception("Staysail shaping must only move existing bones.");
        }
        if (
            !CalledMethods(Method("FishermansStaysailRig", "InitializeCut"))
                .Any(m => m.DeclaringType.Name == "FishermansStaysailShape" && m.Name == "Create")
        )
            throw new Exception("The family rig must request the cut from its mark.");
        var lateUpdate = CalledMethods(Method("FishermansStaysailRig", "LateUpdate")).ToArray();
        if (
            !lateUpdate.Any(m =>
                m.DeclaringType.Name == "FishermansStaysailEdgeFit" && m.Name == "Fit"
            )
        )
            throw new Exception("The family rig must retain active edge fitting.");
        if (
            !lateUpdate.Any(m =>
                m.DeclaringType.Name == "FishermansStaysailFixedHead" && m.Name == "Update"
            )
            || !lateUpdate.Any(m =>
                m.DeclaringType.Name == "FishermansStaysailFixedHead" && m.Name == "Position"
            )
            || Type("FishermansStaysailShape").GetProperty("FixedUpperHeadAngle", all) != null
            || Type("FishermansStaysailShape").GetProperty("UpperCornerTrim", all) != null
            || Type("FishermansStaysailFrameGeometry").GetMethod("UpperHead", all) != null
            || assembly.GetType(family + "FishermansStaysailUpperTrim") != null
            || (float)
                Type("FishermansStaysailFixedHead")
                    .GetField("DeployedAngle", all)
                    .GetRawConstantValue() != 14f
        )
            throw new Exception(
                "Mk.A must select the fixed 14-degree policy without its old upper trim."
            );
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
            "PASS: Mk.A prefab identity, stay membership, fit guards, animation sampling, native bundle, control finalizer and fixed-mesh lifecycle."
        );
    }
}
