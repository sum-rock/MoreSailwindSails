using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.Visuals;

// Guards native setting/material integration, visual ownership and family route coverage.
internal static class RoutedRopeChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags flags =
            BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.DeclaredOnly;
        var rope = assembly.GetType(
            name: "MoreSailwindSails.Visuals.RoutedRope",
            throwOnError: true
        );
        var display = rope.GetMethod(name: "SetVisible", bindingAttr: flags);
        var ensure = rope.GetMethod(name: "EnsureTube", bindingAttr: flags);
        var line = rope.GetField(name: "Line", bindingAttr: flags);
        Require(
            value: line.IsPrivate
                && line.CustomAttributes.Any(a => a.AttributeType.Name == "SerializeField"),
            message: "The route renderer must remain private but serialized for cloned sail templates."
        );
        Require(
            value: Instructions(display)
                .Any(i =>
                    i.Operand is FieldInfo field
                    && field.DeclaringType.Name == "Settings"
                    && field.Name == "clothRopes"
                ),
            message: "Routed ropes must follow the installed native 3D rope setting."
        );
        Require(
            value: Instructions(ensure)
                .Any(i => i.Operand is FieldInfo field && field.Name == "clothRopePrefab")
                && CalledMethods(ensure).Any(m => m.Name == "get_sharedMaterial"),
            message: "Rope mesh must use the installed native 3D rope material."
        );
        Require(
            value: CalledMethods(rope.GetMethod(name: "Attach", bindingAttr: flags))
                .All(m => m.Name != "EnsureTube")
                && CalledMethods(display).Any(m => m.Name == "EnsureTube"),
            message: "Meshes must be created per live instance, not on templates."
        );
        Require(
            value: CalledMethods(rope.GetMethod(name: "OnDisable", bindingAttr: flags))
                .Any(m => m.Name == "SetVisible")
                && CalledMethods(rope.GetMethod(name: "OnDestroy", bindingAttr: flags))
                    .Any(m => m.Name == "Destroy"),
            message: "Disabling must hide both visuals; destruction must release the owned mesh."
        );
        Require(
            value: CalledMethods(display).Any(m => m.Name == "get_worldToLocalMatrix")
                && CalledMethods(display).Any(m => m.Name == "get_transpose")
                && CalledMethods(display).Any(m => m.Name == "RecalculateBounds"),
            message: "World-space rope diameter/normals/bounds must survive scaled parent transforms."
        );
        var upload = Instructions(method: display).ToArray();
        int Call(string name) =>
            Array.FindIndex(array: upload, match: i => i.Operand is MethodInfo m && m.Name == name);
        Require(
            value: Call(name: "Pose") >= 0
                && Call(name: "get_vertexCount") > Call(name: "Pose")
                && Call(name: "Clear") > Call(name: "get_vertexCount")
                && Call(name: "set_triangles") > Call(name: "Clear"),
            message: "After a failed pose, retry topology must follow the uploaded mesh count, not already-resized buffers; clear/upload only after successful posing."
        );
        foreach (var method in rope.GetMethods(bindingAttr: flags))
        {
            Require(
                value: !Instructions(method)
                    .Any(i =>
                        (i.Code == OpCodes.Stfld || i.Code == OpCodes.Stsfld)
                        && i.Operand is FieldInfo field
                        && field.DeclaringType.Assembly.GetName().Name == "Assembly-CSharp"
                    ),
                message: "Rendering must not write native control/settings state."
            );
            Require(
                value: CalledMethods(method)
                    .All(m =>
                        m.DeclaringType.Name is not ("Cloth" or "RopeEffect" or "HingeJoint")
                        && m.Name is not ("set_bindposes" or "set_bones")
                    ),
                message: "Rendering must not change sail Cloth or control physics."
            );
        }
        foreach (
            string name in new[]
            {
                "FishermansFlyingSail.FishermansFlyingSailSupportLine",
                "Spritsail.LooseFootedSpritsail.LooseFootedSpritsailLines",
                "Spritsail.BoomedSpritsail.BoomedSpritsailLines",
                "Spritsail.SpritsailSnotter",
                "Spritsail.SpritsailRopeCollar",
            }
        )
        {
            var owner = assembly.GetType(
                name: "MoreSailwindSails.Sails." + name,
                throwOnError: true
            );
            Require(
                value: owner
                    .GetMethods(bindingAttr: flags)
                    .SelectMany(CalledMethods)
                    .Any(m => m.DeclaringType == rope && m.Name == "Attach"),
                message: "Custom rope owner missed 3D rendering: " + name
            );
            Require(
                value: CalledMethods(owner.GetMethod(name: "Hide", bindingAttr: flags))
                    .Any(m => m.DeclaringType == rope && m.Name == "SetVisible"),
                message: "Custom rope owner missed mesh hiding: " + name
            );
        }
        Console.WriteLine(
            "PASS (IL): native rope setting/material, custom route coverage, instance mesh ownership, hiding/cleanup and native physics isolation; live switching and rendering remain in-game checks."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new Exception(message: message);
    }
}
