using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.FishermansFlyingSail;

// Guards the boundary between decorative Flying Sail visuals and native rope/cloth state.
internal static class SheetVisualChecks
{
    internal static void Run(Assembly assembly)
    {
        const string prefix = "MoreSailwindSails.Sails.FishermansFlyingSail.";
        const BindingFlags methods =
            BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.DeclaredOnly;
        var patch = assembly.GetType(prefix + "Patches.FishermansFlyingSailSheetVisualPatch", true);
        var marker = assembly.GetType(prefix + "FishermansFlyingSailNativeSheetVisual", true);
        var route = assembly.GetType(prefix + "FishermansFlyingSailSupportLine", true);
        var knots = assembly.GetType(prefix + "FishermansFlyingSailKnots", true);
        var rig = assembly.GetType(prefix + "FishermansFlyingSailRig", true);
        var target = patch.GetCustomAttribute<HarmonyPatch>().info;
        var postfix = patch.GetMethod("Postfix", methods);
        if (
            target.declaringType.Name != "RopeEffect"
            || target.methodName != "LateUpdate"
            || !postfix.IsDefined(typeof(HarmonyPostfix))
            || patch.GetMethod("Prefix", methods) != null
        )
            throw new Exception("Sheet visuals must leave native RopeEffect.LateUpdate running.");
        if (
            !CalledMethods(postfix)
                .OfType<MethodInfo>()
                .Any(m =>
                    m.Name == "GetComponent"
                    && m.IsGenericMethod
                    && m.GetGenericArguments().Single() == marker
                )
            || !Instructions(postfix).Any(i => i.Code.FlowControl == FlowControl.Cond_Branch)
            || !CalledMethods(postfix).Any(m => m.DeclaringType == marker && m.Name == "Suppress")
        )
            throw new Exception(
                "Native visual suppression must be guarded by the owned Flying Sail marker."
            );
        foreach (string name in new[] { "lineRenderer", "clothRope" })
        {
            var field = target.declaringType.GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (
                field == null
                || !postfix
                    .GetParameters()
                    .Any(p => p.Name == "___" + name && p.ParameterType == field.FieldType)
            )
                throw new Exception(
                    "Sheet suppression no longer matches the installed native rope visuals."
                );
        }
        foreach (var type in new[] { patch, marker, route })
        foreach (var method in type.GetMethods(methods))
        {
            foreach (var instruction in Instructions(method))
                if (
                    instruction.Code == OpCodes.Stfld
                    && instruction.Operand is FieldInfo field
                    && field.DeclaringType == target.declaringType
                )
                    throw new Exception("Visual replacement must not modify native rope state.");
            foreach (var called in CalledMethods(method))
                if (
                    called.DeclaringType.FullName is "UnityEngine.Cloth" or "UnityEngine.HingeJoint"
                    || called.Name
                        is "set_sharedMesh"
                            or "set_bones"
                            or "set_bindposes"
                            or "set_rotation"
                            or "set_localRotation"
                            or "set_localScale"
                )
                    throw new Exception(
                        "Sheet visuals must not alter cloth topology, bone orientation or hinge physics."
                    );
        }
        foreach (var type in new[] { marker, route, rig, knots })
            if (!CalledMethods(type.GetMethod("OnDisable", methods)).Any(m => m.Name == "Hide"))
                throw new Exception("Disabling a sail or its visual owner must hide its ropes.");
        var hide = CalledMethods(marker.GetMethod("Hide", methods)).ToArray();
        if (!hide.Any(m => m.Name == "set_enabled") || !hide.Any(m => m.Name == "SetActive"))
            throw new Exception("Both line and optional ClothRope visuals must be suppressed.");
        var lateCalls = CalledMethods(rig.GetMethod("LateUpdate", methods)).ToList();
        if (
            lateCalls.FindIndex(m => m.DeclaringType == route && m.Name == "Draw")
            <= lateCalls.FindIndex(m => m.Name == "UpdateShapeBones")
        )
            throw new Exception("Rope drawing must follow the final sail pose.");
        var routeDraw = CalledMethods(route.GetMethod("Draw", methods)).ToArray();
        if (
            routeDraw.Count(m => m.Name == "DrawDirectSpan") != 3
            || !routeDraw.Any(m => m.DeclaringType == knots && m.Name == "Draw")
            || !routeDraw.Any(m => m.DeclaringType == knots && m.Name == "Hide")
            || !CalledMethods(route.GetMethod("Hide", methods))
                .Any(m => m.DeclaringType == knots && m.Name == "Hide")
        )
            throw new Exception(
                "External sheets must use direct spans and share knot visibility/parking."
            );
        var create = knots.GetMethod("TryCreate", methods);
        var creationCalls = CalledMethods(create).ToList();
        int bake = creationCalls.FindIndex(m => m.Name == "BakeMesh");
        if (
            bake < 0
            || bake >= creationCalls.FindIndex(m => m.Name == "get_vertices")
            || !creationCalls.Any(m =>
                m.DeclaringType.Name == "FishermansFlyingSailKnotGeometry" && m.Name == "Select"
            )
            || !creationCalls.Any(m => m.Name == "set_sharedMaterial")
            || !create
                .GetMethodBody()
                .ExceptionHandlingClauses.Any(c =>
                    c.Flags == ExceptionHandlingClauseOptions.Finally
                )
        )
            throw new Exception(
                "Knot creation must bake a private copy, isolate the knot, reuse material and clean up temporary assets."
            );
        if (
            !Instructions(create)
                .Any(i => i.Operand is FieldInfo field && field.Name == "clothRopeJibSheetPrefab")
        )
            throw new Exception(
                "Knot appearance must come from the installed native jib-sheet prefab."
            );
        var channels = assembly.GetType(prefix + "FishermansFlyingSailKnotChannels", true);
        var creationIL = Instructions(create).ToList();
        int bounds = creationIL.FindIndex(i =>
            i.Operand is MethodBase m && m.Name == "RecalculateBounds"
        );
        int applyUVs = creationIL.FindIndex(i =>
            i.Operand is MethodBase m && m.DeclaringType == knots && m.Name == "ApplyUVs"
        );
        var uvCalls = CalledMethods(knots.GetMethod("ApplyUVs", methods)).ToArray();
        if (
            !creationCalls.Any(m => m.DeclaringType == channels && m.Name == "Validate")
            || !creationCalls.Any(m => m.DeclaringType == channels && m.Name == "Remap")
            || bounds < 0
            || applyUVs <= bounds
            || !creationIL
                .Skip(bounds + 1)
                .Take(applyUVs - bounds - 1)
                .Any(i => i.Code.FlowControl == FlowControl.Cond_Branch)
            || creationCalls.Any(m =>
                m.Name
                    is "set_uv"
                        or "set_tangents"
                        or "RecalculateTangents"
                        or "RecalculateNormals"
            )
            || !uvCalls.Any(m => m.DeclaringType == channels && m.Name == "Remap")
            || !uvCalls.Any(m => m.Name == "set_uv")
            || !uvCalls.Any(m => m.Name == "RecalculateTangents")
        )
            throw new Exception(
                "Knot UV assignment and tangent generation must be conditional on validated UVs; native normals must be preserved."
            );
        var materialCalls = CalledMethods(knots.GetMethod("HasAssignedTextures", methods))
            .ToArray();
        if (
            !materialCalls.Any(m => m.Name == "GetTexturePropertyNames")
            || !materialCalls.Any(m => m.Name == "GetTexture")
            || !creationCalls.Any(m => m.Name == "get_shader")
            || !creationCalls.Any(m => m.DeclaringType == knots && m.Name == "HasAssignedTextures")
        )
            throw new Exception(
                "UV-less knot compatibility must inspect the native shader and all assigned textures."
            );
        int inactive = creationCalls.FindIndex(m => m.Name == "get_activeInHierarchy");
        int instantiate = creationCalls.FindIndex(m => m.Name == "Instantiate");
        if (
            inactive < 0
            || instantiate <= inactive
            || creationCalls.Count(m => m.Name == "DestroyImmediate") != 4
            || knots.GetMethods(methods).SelectMany(CalledMethods).Any(m => m.Name == "SetActive")
        )
            throw new Exception(
                "Knot construction must retain the inactive-parent guard, temporary/failed asset cleanup, and never activate its donor."
            );
        foreach (
            string getter in new[]
            {
                "get_vertexCount",
                "get_isReadable",
                "get_vertices",
                "get_normals",
                "get_uv",
                "get_tangents",
            }
        )
            if (!creationCalls.Any(m => m.Name == getter))
                throw new Exception(
                    "Knot diagnostics must retain source metadata and each baked channel count."
                );
        foreach (var method in knots.GetMethods(methods).Where(m => !m.IsStatic))
        foreach (var called in CalledMethods(method))
            if (
                called.DeclaringType.Name is "Cloth" or "ClothRope" or "RopeEffect" or "HingeJoint"
                || called.Name
                    is "BakeMesh"
                        or "set_sharedMesh"
                        or "set_bones"
                        or "set_bindposes"
                        or "Destroy"
                        or "DestroyImmediate"
            )
                throw new Exception(
                    "Live knot callbacks must only pose/hide independent visuals; instances do not own meshes or rope physics."
                );
        Console.WriteLine(
            "PASS (IL): scoped native sheet suppression, direct external spans, private native-knot baking, conditional UV/tangents, material compatibility, cleanup and knot lifecycle; live cloth and rope physics preserved. Baking/rendering remain runtime checks."
        );
    }
}
