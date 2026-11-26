// Guards fixed sensor ownership and force integration without starting Unity.
using System;
using System.Linq;
using System.Reflection;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.FishermansStaysail;

internal static class WindCenterChecks
{
    internal static void Run(Assembly assembly)
    {
        const string family = "MoreSailwindSails.Sails.FishermansStaysail.";
        const BindingFlags all =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static;
        var rig = assembly.GetType(family + "FishermansStaysailRig", throwOnError: true);
        MethodInfo Method(string name) => rig.GetMethod(name, all);
        if (
            Method("RefreshAerodynamics") != null
            || assembly.GetType(family + "Patches.FishermansStaysailWindFramePatch") != null
        )
            throw new Exception(
                "Staysails must not reconstruct their sensor from animated corners."
            );
        foreach (string name in new[] { "windCenter", "windRotation" })
        {
            var field = rig.GetField(name, all);
            if (
                field == null
                || !field.CustomAttributes.Any(a => a.AttributeType.Name == "SerializeField")
            )
                throw new Exception("Fitted sensor data must survive prefab cloning: " + name);
            if (!Instructions(Method("InitializeCut")).Any(i => Equals(i.Operand, field)))
                throw new Exception(
                    "Installed staysails must use the fitted authored sensor: " + name
                );
        }
        foreach (string name in new[] { "Configure", "RefreshFrame" })
            if (!CalledMethods(Method(name)).Contains(Method("UpdateWindCenter")))
                throw new Exception("Sensor must follow initial setup and refitting: " + name);
        foreach (string name in new[] { "PoseAnimation", "LateUpdate" })
            if (CalledMethods(Method(name)).Contains(Method("UpdateWindCenter")))
                throw new Exception("Cloth animation must not reposition the fixed sensor.");
        var update = CalledMethods(Method("UpdateWindCenter")).ToArray();
        if (
            !update.Any(m => m.Name == "set_localPosition")
            || !update.Any(m => m.Name == "set_localRotation")
            || update.Any(m => m.Name == "SetPositionAndRotation")
        )
            throw new Exception("Fixed windcenter must inherit the sail frame's live rotation.");
        var force = assembly
            .GetType(family + "Patches.FishermansStaysailForceDirectionPatch", throwOnError: true)
            .GetMethod("Prefix", all);
        var forceCalls = CalledMethods(force).ToArray();
        if (
            !forceCalls.Any(m => m.Name == "get_up")
            || !forceCalls.Any(m => m.Name == "ForceDirection")
            || forceCalls.Any(m => m.DeclaringType == rig)
        )
            throw new Exception(
                "Force direction must use the fixed sensor normal without posing the rig."
            );
        Console.WriteLine(
            "PASS: staysail fixed windcenter lifecycle, serialized fitting data and force-direction integration."
        );
    }
}
