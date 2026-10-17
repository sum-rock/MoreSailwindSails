// Guards registry mesh sharing, native materials and cleanup without running Unity objects.
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.FishermansStay;

internal static class CollarChecks
{
    internal static void Run(Assembly assembly)
    {
        const string prefix = "MoreSailwindSails.Stays.FishermansStay.";
        const BindingFlags flags =
            BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.Instance
            | BindingFlags.Static;
        var visual = assembly.GetType(prefix + "FishermansStayVisual", throwOnError: true);
        var registry = assembly.GetType(prefix + "FishermansStayRegistry", throwOnError: true);
        var calls = CalledMethods(visual.GetConstructors(flags).Single()).ToArray();
        if (!calls.Any(m => m.Name == "FindObjectsOfTypeAll"))
            throw new Exception("Stay visuals lost native material discovery.");
        var materialPredicate = visual
            .GetNestedTypes(flags)
            .SelectMany(t => t.GetMethods(flags))
            .Any(m =>
                m.GetMethodBody() != null && Instructions(m).Any(i => Equals(i.Operand, "metal2"))
            );
        if (!materialPredicate)
            throw new Exception("Stay collars must use the installed lantern's metal2 material.");
        calls = CalledMethods(registry.GetMethod("PopulateMesh", flags)).ToArray();
        foreach (string channel in new[] { "vertices", "normals", "uv", "triangles" })
            if (!calls.Any(m => m.Name == "set_" + channel))
                throw new Exception("Baked stay lost its " + channel + " channel.");
        if (calls.Any(m => m.Name == "RecalculateNormals"))
            throw new Exception("Stay construction must retain authored normals.");
        foreach (string method in new[] { "RecalculateBounds", "RecalculateTangents" })
            if (!calls.Any(m => m.Name == method))
                throw new Exception("Shared stay mesh lost " + method);
        calls = CalledMethods(visual.GetMethod("CreatePart", flags)).ToArray();
        if (
            !calls.Any(m => m.Name == "set_sharedMaterial")
            || !calls.Any(m => m.Name == "set_sharedMesh")
            || calls.Any(m => m.Name is "set_material" or "get_mesh" or "set_mesh")
        )
            throw new Exception(
                "Stay rendering must borrow shared meshes and native materials without private copies."
            );
        var stay = assembly.GetType(prefix + "FishermansStay", throwOnError: true);
        foreach (var method in visual.GetMethods(flags).Concat(stay.GetMethods(flags)))
            if (
                method.GetMethodBody() != null
                && CalledMethods(method)
                    .Any(m =>
                        m.Name == "AddComponent"
                        && m.IsGenericMethod
                        && m.GetGenericArguments()
                            .Any(t => t.Name.EndsWith("Collider", StringComparison.Ordinal))
                    )
            )
                throw new Exception("Decorative stays must not create colliders.");
        if (
            !Instructions(stay.GetMethod("Create", flags))
                .Any(i => i.Operand is FieldInfo f && f.Name == "walkColMast")
        )
            throw new Exception("Native sail fitting still requires the stay's walking frame.");
        var place = stay.GetMethod("Place", flags);
        if (
            !Instructions(place).Any(i => i.Operand is FieldInfo f && f.Name == "boatModel")
            || !CalledMethods(place).Any(m => m.DeclaringType == visual && m.Name == "Place")
        )
            throw new Exception(
                "The sail checker's walking frame must mirror the boat model frame."
            );
        var collar = visual.GetMethod("PlaceCollar", flags);
        if (
            !CalledMethods(collar).Any(m => m.Name == "MultiplyPoint3x4")
            || CalledMethods(collar)
                .Any(m => m.Name is "get_vertices" or "get_triangles" or "get_radius")
        )
            throw new Exception(
                "Collars must use authored seating measurements, not capsule radii or mesh surface scans."
            );
        var visualPlace = visual.GetMethod("Place", flags);
        foreach (
            string field in new[]
            {
                "ForeCollarRadius",
                "AftCollarRadius",
                "ForeCollarOffset",
                "AftCollarOffset",
            }
        )
            if (!Instructions(visualPlace).Any(i => i.Operand is FieldInfo f && f.Name == field))
                throw new Exception("Collar placement lost its authored " + field);
        foreach (
            string removed in new[]
            {
                "FishermansStayAttachmentVisual",
                "FishermansStayCollarFrame",
                "FishermansStayMeshCopy",
                "FishermansStayMeshRegions",
                "FishermansStaySparSurface",
            }
        )
            if (assembly.GetType(prefix + removed) != null)
                throw new Exception("Legacy donor geometry remains: " + removed);
        CheckMeshOwnership(registry: registry, stay: stay, visual: visual, flags: flags);
        foreach (string path in new[] { "Register", "OnDestroy" })
        {
            var method = registry.GetMethod(path, flags);
            if (!CalledMethods(method).Any(m => m.DeclaringType == stay && m.Name == "Destroy"))
                throw new Exception("Stay object cleanup is missing from rollback or teardown.");
            var instructions = Instructions(method).ToArray();
            int cleanup = Array.FindIndex(
                instructions,
                i =>
                    i.Operand is MethodInfo m
                    && m.DeclaringType == registry
                    && m.Name == "DestroyMeshes"
            );
            if (
                cleanup < 0
                || !method
                    .GetMethodBody()
                    .ExceptionHandlingClauses.Any(c =>
                        c.Flags == ExceptionHandlingClauseOptions.Finally
                    )
                || instructions.Skip(cleanup + 1).First(i => i.Code != OpCodes.Nop).Code
                    != OpCodes.Endfinally
            )
                throw new Exception("Shared mesh cleanup must finish a finally handler in " + path);
        }
        Console.WriteLine(
            "PASS (structural): registry-owned mesh pair, lazy construction, partial-build ownership, finalized cleanup, shared visuals, native materials and retained placement/walking frames. Unity lifecycle is not executed."
        );
    }

    private static void CheckMeshOwnership(
        Type registry,
        Type stay,
        Type visual,
        BindingFlags flags
    )
    {
        var ensure = registry.GetMethod("EnsureMeshes", flags);
        var instructions = Instructions(ensure).ToArray();
        int firstReturn = Array.FindIndex(instructions, i => i.Code == OpCodes.Ret);
        int firstMesh = Array.FindIndex(
            instructions,
            i =>
                i.Code == OpCodes.Newobj
                && i.Operand is ConstructorInfo c
                && c.DeclaringType.FullName == "UnityEngine.Mesh"
        );
        if (
            firstReturn < 0
            || firstReturn >= firstMesh
            || CalledMethods(ensure)
                .Count(m => m is ConstructorInfo && m.DeclaringType.FullName == "UnityEngine.Mesh")
                != 2
        )
            throw new Exception(
                "Stay registry must reuse exactly one rope/collar pair after initialization."
            );
        var destroy = Instructions(registry.GetMethod("DestroyMeshes", flags)).ToArray();
        foreach (
            var (fieldName, factory) in new[]
            {
                ("ropeMesh", "CreateRope"),
                ("collarMesh", "CreateCollar"),
            }
        )
        {
            var field = registry.GetField(fieldName, flags);
            int store = Array.FindIndex(
                instructions,
                i => i.Code == OpCodes.Stfld && Equals(i.Operand, field)
            );
            int createData = Array.FindIndex(
                instructions,
                i => i.Operand is MethodInfo m && m.Name == factory
            );
            if (
                field == null
                || field.IsStatic
                || field.FieldType.FullName != "UnityEngine.Mesh"
                || !instructions.Take(firstReturn).Any(i => Equals(i.Operand, field))
                || store < firstMesh
                || createData <= store
            )
                throw new Exception(
                    "Registry must own " + fieldName + " before its channels can throw."
                );
            int clear = Array.FindIndex(
                destroy,
                i => i.Code == OpCodes.Stfld && Equals(i.Operand, field)
            );
            if (
                clear < 1
                || destroy[clear - 1].Code != OpCodes.Ldnull
                || !destroy
                    .Take(clear)
                    .Any(i => i.Code == OpCodes.Ldfld && Equals(i.Operand, field))
            )
                throw new Exception("Shared mesh cleanup must release and clear " + fieldName);
        }
        if (
            CalledMethods(registry.GetMethod("DestroyMeshes", flags))
                .Count(m => m.DeclaringType.FullName == "UnityEngine.Object" && m.Name == "Destroy")
            != 2
        )
            throw new Exception("Registry cleanup must destroy both owned meshes.");
        var registrationCalls = CalledMethods(registry.GetMethod("Register", flags)).ToArray();
        int ensureCall = Array.FindIndex(registrationCalls, m => m == ensure);
        int createStay = Array.FindIndex(
            registrationCalls,
            m => m.DeclaringType == stay && m.Name == "Create"
        );
        if (ensureCall < 0 || createStay <= ensureCall)
            throw new Exception("Registry must prepare shared meshes before constructing stays.");
        if (
            stay.GetField("ownedMeshes", flags) != null
            || stay.GetMethod("Create", flags)
                .GetParameters()
                .Count(p => p.ParameterType.FullName == "UnityEngine.Mesh") != 2
            || visual
                .GetConstructors(flags)
                .Single()
                .GetParameters()
                .Count(p => p.ParameterType.FullName == "UnityEngine.Mesh") != 2
        )
            throw new Exception(
                "Stay and visual construction must borrow the registry's mesh pair."
            );
        foreach (var type in new[] { stay, visual })
        foreach (
            var method in type.GetMethods(flags)
                .Cast<MethodBase>()
                .Concat(type.GetConstructors(flags))
        )
            if (
                CalledMethods(method)
                    .Any(m =>
                        m.DeclaringType.FullName == "UnityEngine.Mesh"
                        || m.Name is "CreateRope" or "CreateCollar" or "DestroyMeshes" or "get_mesh"
                    )
            )
                throw new Exception(
                    "Stay instances must not allocate, mutate or release meshes: " + method.Name
                );
    }
}
