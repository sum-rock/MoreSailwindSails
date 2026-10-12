using System;
using System.Collections.Generic;
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
        foreach (string cut in new[] { "A", "B", "C" })
        {
            var create = CalledMethods(
                    Method($"Mk{cut}.FishermansStaysailMk{cut}Geometry", "Create")
                )
                .ToArray();
            if (
                !create.Any(m =>
                    m.DeclaringType.Name == $"FishermansStaysailMk{cut}BakedMesh"
                    && m.Name == "Create"
                )
                || !create.Any(m =>
                    m.DeclaringType.Name == "FishermansStaysailMeshFitting" && m.Name == "Fit"
                )
            )
                throw new Exception($"Mk.{cut} must consume its bake through the shared fitter.");
        }
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
        foreach (string patch in new[] { "InstallError", "InstallGuard", "Fit" })
            if (
                !CalledMethods(Patch(patch))
                    .Any(m =>
                        m.Name == "InstallError"
                        && m.DeclaringType.Name == "FishermansStaysailRigging"
                    )
            )
                throw new Exception("Missing physical mast fit guard: " + patch);
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
            || !lateUpdate.Any(m => m.Name == "get_FixedUpperHeadAngle")
            || Type("MkA.FishermansStaysailMkAShape")
                .GetProperty("FixedUpperHeadAngle", all)
                .DeclaringType != Type("MkA.FishermansStaysailMkAShape")
            || Type("FishermansStaysailShape").GetProperty("FixedUpperHeadAngle", all).PropertyType
                != typeof(float)
            || !Type("FishermansStaysailShape")
                .GetProperty("FixedUpperHeadAngle", all)
                .GetMethod.IsAbstract
            || Type("FishermansStaysailShape").GetProperty("UpperCornerTrim", all) != null
            || Type("FishermansStaysailFrameGeometry").GetMethod("UpperHead", all) != null
            || assembly.GetType(family + "FishermansStaysailUpperTrim") != null
            || (float)
                Type("MkA.FishermansStaysailMkAGeometry")
                    .GetField("FixedUpperHeadAngle", all)
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
        var prefix = Patch("OrderText");
        if (
            !prefix
                .GetCustomAttribute<HarmonyBefore>()
                .info.before.Contains("com.nandbrew.nandfixes")
        )
            throw new Exception("Mk.A order guard must precede NANDFixes.");
        var lines = new List<string> { "existing" };
        object[] args = { "192: (ERROR): Fisherman's Staysail Mk.A (150%) -> (no sail)", lines };
        if (
            (bool)prefix.Invoke(null, args)
            || (string)args[0] != ""
            || lines[0] != "existing"
            || lines.Any(l => l.Length > 45)
        )
            throw new Exception("Mk.A order text is unsafe.");
        args[0] = "Fisherman's Stay (a long physical stay part description)";
        if (!(bool)prefix.Invoke(null, args))
            throw new Exception("Mk.A text guard changed a stay-part order.");
        Console.WriteLine(
            "PASS: Mk.A prefab identity, stay membership, fit guards, animation sampling, native bundle, control finalizer and fixed-mesh lifecycle."
        );
    }
}
