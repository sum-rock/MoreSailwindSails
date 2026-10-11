using System;
using System.Linq;
using System.Reflection;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Spritsail;

// Guards template cloning and the ordering required by zero-displacement bone-write shortcuts.
internal static class FlexChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags flags =
            BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.Public
            | BindingFlags.NonPublic;
        var rig = assembly.GetType(
            "MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.LooseFootedSpritsailRig",
            throwOnError: true
        );
        var weights = rig.GetField("flexWeights", flags);
        Require(
            value: weights != null
                && weights.IsPrivate
                && weights.FieldType == typeof(float[])
                && weights.CustomAttributes.Any(a =>
                    a.AttributeType.FullName == "UnityEngine.SerializeField"
                ),
            message: "Authored flex weights must survive Unity template cloning."
        );
        var setup = IlReader.CalledMethods(rig.GetMethod("Configure", flags)).ToArray();
        Require(
            value: setup.Any(m =>
                m.Name == "Weights" && m.DeclaringType.Name == "LooseFootedSpritsailFlex"
            ),
            message: "Template construction must initialize authored flex weights."
        );
        var flex = IlReader.CalledMethods(rig.GetMethod("ApplySheetFlex", flags)).ToArray();
        Require(
            value: !flex.Any(m => m.Name is "Weight" or "Weights")
                && flex.Any(m => m.Name == "Smooth")
                && flex.Any(m => m.Name == "Fit"),
            message: "Live flex retains smoothing/fitting without recomputing invariant weights."
        );
        var late = IlReader.CalledMethods(rig.GetMethod("LateUpdate", flags)).ToArray();
        Require(
            value: Array.FindIndex(late, m => m.Name == "UpdateShapeBones")
                < Array.FindIndex(late, m => m.Name == "ApplySheetFlex")
                && Array.FindIndex(late, m => m.Name == "ApplySheetFlex")
                    < Array.FindIndex(late, m => m.Name == "RefreshAerodynamics"),
            message: "Each frame must reset the base pose before adding flex and updating its aerodynamic frame."
        );
        Console.WriteLine(
            "PASS: flex weights are serialized and built with the template; live smoothing and base-pose/flex/aerodynamics ordering remain intact. Unity cloning requires in-game validation."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }
}
