using System;
using System.Linq;
using System.Reflection;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Spritsail;

// Guards native airflow convention and one shared, non-simulated obstruction state.
internal static class ObstructionChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags all =
            BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.Static
            | BindingFlags.Instance;
        const string family = "MoreSailwindSails.Sails.Spritsail.";
        MethodInfo Method(string type, string name) =>
            assembly
                .GetType(name: family + type, throwOnError: true)
                .GetMethod(name: name, bindingAttr: all);
        var native = Assembly.Load("Assembly-CSharp").GetType("Sail");
        var wind = IlReader
            .Instructions(method: native.GetMethod("GetApparentWind", all))
            .ToArray();
        int current = Array.FindIndex(
            wind,
            i => i.Operand is FieldInfo f && f.Name == "currentWind"
        );
        int velocity = Array.FindIndex(
            wind,
            i => i.Operand is MethodInfo m && m.Name == "get_velocity"
        );
        int subtract = Array.FindIndex(
            wind,
            i => i.Operand is MethodInfo m && m.Name == "op_Subtraction"
        );
        Require(
            current >= 0 && velocity > current && subtract > velocity,
            "Native apparent wind must remain wind velocity minus boat velocity."
        );
        var force = IlReader
            .CalledMethods(method: Method("SpritsailCategory", "ScalePropulsion"))
            .ToArray();
        Require(
            force.Any(m => m.Name == "IsSpritsail")
                && force.Count(m => m.Name == "ObstructionMultiplier") == 1,
            "Apply obstruction once within the existing category-scoped propulsion path."
        );
        var penalty = IlReader
            .CalledMethods(method: Method("SpritsailCategory", "ObstructionMultiplier"))
            .ToArray();
        var shape = IlReader
            .CalledMethods(
                method: Method("LooseFootedSpritsail.LooseFootedSpritsailRig", "UpdateShapeBones")
            )
            .ToArray();
        Require(
            penalty.Any(m =>
                m.DeclaringType.FullName == family + "SpritsailObstruction"
                && m.Name == "get_Amount"
            )
                && shape.Any(m =>
                    m.DeclaringType.FullName == family + "SpritsailObstruction"
                    && m.Name == "get_Amount"
                ),
            "Force and shape must read the same obstruction state."
        );
        var runtime = IlReader
            .CalledMethods(
                method: Method("LooseFootedSpritsail.LooseFootedSpritsailRig", "LateUpdate")
            )
            .ToArray();
        Require(
            Array.FindIndex(runtime, m => m.Name == "UpdateState")
                < Array.FindIndex(runtime, m => m.Name == "UpdateShapeBones")
                && runtime.Any(m => m.Name == "UpdateState"),
            "Update tack state before shape and sheet fitting."
        );
        foreach (
            var calls in new[]
            {
                shape,
                IlReader
                    .CalledMethods(method: Method("SpritsailObstruction", "UpdateState"))
                    .ToArray(),
            }
        )
            Require(
                !calls.Any(m =>
                    m.Name
                        is "set_sharedMesh"
                            or "set_bindposes"
                            or "set_coefficients"
                            or "ClearTransformMotion"
                            or "AddComponent"
                ),
                "Obstruction must not change live Cloth topology or solver state."
            );
        Console.WriteLine(
            "PASS: installed airflow convention and shared spritsail force/visual obstruction wiring."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }
}
