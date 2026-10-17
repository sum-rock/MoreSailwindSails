// Guards baked stay construction, native material reuse and owned mesh cleanup without Unity objects.
using System;
using System.Linq;
using System.Reflection;
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
        var calls = CalledMethods(visual.GetConstructors(flags).Single()).ToArray();
        foreach (string name in new[] { "CreateRope", "CreateCollar", "FindObjectsOfTypeAll" })
            if (!calls.Any(m => m.Name == name))
                throw new Exception("Baked stay construction lost " + name);
        var materialPredicate = visual
            .GetNestedTypes(flags)
            .SelectMany(t => t.GetMethods(flags))
            .Any(m =>
                m.GetMethodBody() != null && Instructions(m).Any(i => Equals(i.Operand, "metal2"))
            );
        if (!materialPredicate)
            throw new Exception("Stay collars must use the installed lantern's metal2 material.");
        calls = CalledMethods(visual.GetMethod("CreateMesh", flags)).ToArray();
        foreach (string channel in new[] { "vertices", "normals", "uv", "triangles" })
            if (!calls.Any(m => m.Name == "set_" + channel))
                throw new Exception("Baked stay lost its " + channel + " channel.");
        if (calls.Any(m => m.Name == "RecalculateNormals"))
            throw new Exception("Stay construction must retain authored normals.");
        calls = CalledMethods(visual.GetMethod("CreatePart", flags)).ToArray();
        if (
            !calls.Any(m => m.Name == "set_sharedMaterial")
            || calls.Any(m => m.Name == "set_material")
        )
            throw new Exception(
                "Stay rendering must reuse native materials without private copies."
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
        var meshes = stay.GetField("ownedMeshes", flags);
        var cleanup = stay.GetMethod("Destroy", flags);
        if (
            !Instructions(cleanup).Any(i => Equals(i.Operand, meshes))
            || !CalledMethods(cleanup)
                .Any(m => m.DeclaringType?.FullName == "UnityEngine.Object" && m.Name == "Destroy")
        )
            throw new Exception("Stay cleanup lost ownership of baked meshes.");
        var registry = assembly.GetType(prefix + "FishermansStayRegistry", throwOnError: true);
        foreach (string path in new[] { "Register", "OnDestroy" })
            if (
                !CalledMethods(registry.GetMethod(path, flags))
                    .Any(m => m.DeclaringType == stay && m.Name == "Destroy")
            )
                throw new Exception("Baked stay cleanup is missing from rollback or teardown.");
        Console.WriteLine(
            "PASS: collider-free baked stay construction, lantern metal reuse, authored attachment placement, retained walking frame and owned cleanup (structural checks)."
        );
    }
}
