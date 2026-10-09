using System;
using System.Linq;
using System.Reflection;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Spritsail;

// Guards the shared upper-purchase route and owned pocket mesh without executing Unity physics.
internal static class DeploymentChecks
{
    internal static void Run(Assembly assembly)
    {
        const string family = "MoreSailwindSails.Sails.Spritsail.";
        const BindingFlags all =
            BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.Instance
            | BindingFlags.Static;
        MethodInfo Method(string type, string name) =>
            assembly
                .GetType(name: family + type, throwOnError: true)
                .GetMethod(name: name, bindingAttr: all);
        foreach (string type in new[] { "LooseFootedSpritsail", "BoomedSpritsail" })
        {
            var aft = Method(type: type + "." + type + "Rigging", name: "AftDirection");
            Require(
                value: !IlReader.CalledMethods(method: aft).Any(m => m.Name == "LuffSailFrame")
                    && IlReader
                        .CalledMethods(
                            method: Method(
                                type: type + "." + type + "Rig",
                                name: "RefreshMastFrame"
                            )
                        )
                        .Count(m => m.Name == "LuffSailFrame") == 1,
                message: "Mast alignment must resolve the stateful luff frame only once."
            );
            var resolve = Method(type: type + "." + type + "Rigging", name: "TryResolve");
            var calls = IlReader.CalledMethods(method: resolve).ToArray();
            Require(
                value: calls.Any(m =>
                    m.DeclaringType.Name == "SpritsailMastAlignment" && m.Name == "IsUsable"
                ) && !calls.Any(m => m.Name == "IsUpright"),
                message: "Both spritsail types must accept finite raked physical masts."
            );
            Require(
                value: !calls.Any(m => m.DeclaringType.Namespace == "MoreSailwindSails.BoatRigs")
                    && calls.Any(m =>
                        m.DeclaringType.Name == "SpritsailNativeBinding" && m.Name == "TryGuides"
                    ),
                message: "Both spritsail types must resolve native mast attachments without boat profiles."
            );
        }
        var looseConfigure = IlReader
            .CalledMethods(
                method: Method(
                    type: "LooseFootedSpritsail.LooseFootedSpritsailRig",
                    name: "Configure"
                )
            )
            .ToArray();
        var looseCollision = IlReader
            .Instructions(
                method: Method(
                    type: "LooseFootedSpritsail.LooseFootedSpritsailRig",
                    name: "ConfigureCollision"
                )
            )
            .ToArray();
        Require(
            value: looseConfigure.Any(m => m.Name == "RefreshSparCollision")
                && looseCollision.Any(i =>
                    i.Operand is string name && name == "LooseFootedSpritsail deployed sprit"
                ),
            message: "The loose-footed deployed sprit collider must be constructed and posed before native Awake."
        );
        Require(
            value: !IlReader
                .CalledMethods(
                    method: Method(
                        type: "LooseFootedSpritsail.LooseFootedSpritsailRig",
                        name: "RefreshSparCollision"
                    )
                )
                .Any(m =>
                    m.Name == "AddComponent"
                    || (m is ConstructorInfo && m.DeclaringType.Name == "GameObject")
                ),
            message: "Loose-footed collision refresh must only reuse initialized collider children."
        );
        foreach (string type in new[] { "LooseFootedSpritsail", "BoomedSpritsail" })
        {
            var rig = type + "." + type + "Rig";
            var collision = IlReader
                .CalledMethods(method: Method(type: rig, name: "RefreshSparCollision"))
                .ToArray();
            Require(
                value: collision.Count(m => m.Name == "Evaluate") == 1
                    && !collision.Any(m => m.DeclaringType.Name.EndsWith("Gathering")),
                message: "Spar collision must use one deployed pose without gathered-fabric envelopes."
            );
            Require(
                value: IlReader
                    .CalledMethods(method: Method(type: rig, name: "DeploymentBounds"))
                    .Any(m => m.DeclaringType.Name.EndsWith("Gathering"))
                    && IlReader
                        .CalledMethods(method: Method(type: rig, name: "RefreshMastFrame"))
                        .Any(m => m.Name == "DeploymentBounds"),
                message: "Fitting must retain gathered-fabric rendering bounds independently of collision."
            );
            var draw = IlReader
                .CalledMethods(method: Method(type: type + "." + type + "Lines", name: "Draw"))
                .ToArray();
            Require(
                value: draw.Any(m => m.DeclaringType.Name == "SpritsailMount" && m.Name == "Draw")
                    && !draw.Any(m => m.DeclaringType.Name == "SpritsailRopeCollar"),
                message: "Both rigs must draw the authored mount instead of the old luff coils."
            );
        }
        foreach (string name in new[] { "Sail", "LuffBones" })
        {
            var field = Method(type: "SpritsailMount", name: "Draw")
                .DeclaringType.GetField(
                    name: name,
                    bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic
                );
            Require(
                value: field != null
                    && field.IsPrivate
                    && field.CustomAttributes.Any(a => a.AttributeType.Name == "SerializeField"),
                message: "Mount references must remain serialized for cloned sail templates."
            );
        }
        var mountDraw = IlReader
            .CalledMethods(method: Method(type: "SpritsailMount", name: "Draw"))
            .ToArray();
        Require(
            value: mountDraw.Any(m =>
                m.DeclaringType.Name == "SpritsailMastSurface" && m is ConstructorInfo
            )
                && mountDraw.Any(m =>
                    m.Name == "RadiusLocal" && m.GetParameters().Any(p => p.Name == "sampleIndex")
                )
                && mountDraw.Any(m =>
                    m.DeclaringType.Name == "SpritsailMountGeometry" && m.Name == "Fit"
                )
                && mountDraw.Any(m => m.Name == "DrawMesh")
                && !mountDraw.Any(m => m.DeclaringType.Name == "Cloth"),
            message: "Authored mounts must fit sampled timber and draw independently of live Cloth."
        );
        Require(
            value: IlReader
                .CalledMethods(method: Method(type: "SpritsailMount", name: "OnDestroy"))
                .Any(m => m.Name == "Destroy"),
            message: "Live mount meshes need owned cleanup."
        );
        using (var mount = assembly.GetManifestResourceStream(name: "MoreSailwindSails.SailMount"))
            Require(
                value: mount != null && mount.Length == 645640,
                message: "The DLL must contain the baked mount."
            );
        var runtime = IlReader
            .CalledMethods(
                method: Method(
                    type: "LooseFootedSpritsail.LooseFootedSpritsailRig",
                    name: "LateUpdate"
                )
            )
            .ToArray();
        var flex = IlReader
            .CalledMethods(
                method: Method(
                    type: "LooseFootedSpritsail.LooseFootedSpritsailRig",
                    name: "ApplySheetFlex"
                )
            )
            .ToArray();
        Require(
            value: runtime.Any(m => m.Name == "ApplySheetFlex")
                && flex.Any(m =>
                    m.DeclaringType.FullName
                        == family + "LooseFootedSpritsail.LooseFootedSpritsailFlex"
                    && m.Name == "Fit"
                )
                && flex.Any(m => m.Name == "set_localPosition"),
            message: "Mk.A must drive its existing bones through the shared bounded flex fit."
        );
        Require(
            value: !flex.Any(m =>
                m.Name
                    is "set_sharedMesh"
                        or "set_bindposes"
                        or "ClearTransformMotion"
                        or "set_coefficients"
                        or "AddComponent"
            ),
            message: "Sheet flex must not rebuild or reset live Cloth."
        );
        using (var socket = assembly.GetManifestResourceStream(name: "MoreSailwindSails.Snotter"))
            Require(
                value: socket != null && socket.Length > 12,
                message: "The plugin DLL must carry the authored snotter mesh without loose runtime assets."
            );
        var visual = IlReader
            .CalledMethods(method: Method(type: "SpritsailSnotter", name: "Pose"))
            .ToArray();
        var upload = IlReader
            .CalledMethods(method: Method(type: "SpritsailSnotterMesh", name: "UploadFit"))
            .ToArray();
        var drawFitting = IlReader
            .CalledMethods(method: Method(type: "SpritsailSnotterMesh", name: "Draw"))
            .ToArray();
        Require(
            value: upload.Any(m => m.Name == "set_normals")
                && !upload.Any(m => m.Name == "RecalculateNormals")
                && IlReader
                    .CalledMethods(method: Method(type: "SpritsailSnotterMesh", name: "Refit"))
                    .Any(m => m.Name == "RecalculateTangents")
                && drawFitting.Any(m => m.Name == "DrawMesh")
                && !visual
                    .Concat(drawFitting)
                    .Any(m =>
                        m.Name
                            is "set_vertices"
                                or "set_normals"
                                or "RecalculateTangents"
                                or "RecalculateBounds"
                    ),
            message: "Rigid snotter drawing must reuse fitted surfaces; authored normals and tangents update only on refit."
        );
        foreach (var calls in new[] { runtime, visual })
            Require(
                value: calls.Any(m =>
                    m.DeclaringType.FullName == family + "SpritsailDeployment"
                    && m.Name == "PurchasePoint"
                ),
                message: "Native endpoint and visible purchase must share the upper-sprit attachment calculation."
            );
        Require(
            value: runtime.Any(m => m.Name == "UpdateHalyard")
                && runtime.Any(m => m.Name == "ExposedArea"),
            message: "Reefing must bind its native endpoint and use the unpleated area."
        );
        Require(
            value: !visual.Any(m =>
                m.DeclaringType.Name == "Cloth" || m.DeclaringType.Name == "Rigidbody"
            ),
            message: "Pocket visual must not mutate Cloth or sail physics."
        );
        Require(
            value: IlReader
                .CalledMethods(method: Method(type: "SpritsailSnotter", name: "OnDestroy"))
                .Count(m => m.DeclaringType.Name == "SpritsailSnotterMesh" && m.Name == "Dispose")
                == 2
                && IlReader
                    .CalledMethods(method: Method(type: "SpritsailSnotterMesh", name: "Dispose"))
                    .Any(m => m.Name == "Destroy"),
            message: "Both rigid fitting meshes must be disposed with their instance owner."
        );
        Require(
            value: assembly
                .GetType(
                    name: family + "LooseFootedSpritsail.LooseFootedSpritsailRig",
                    throwOnError: true
                )
                .GetField(name: "SpritHoistAttachment", bindingAttr: all)
                .FieldType.Name == "Transform",
            message: "Upper purchase must retain an independent endpoint leaf."
        );
        var furledCreate = IlReader
            .CalledMethods(method: Method(type: "SpritsailFurledVisual", name: "Create"))
            .ToArray();
        var furledPose = IlReader
            .CalledMethods(method: Method(type: "SpritsailFurledVisual", name: "Pose"))
            .ToArray();
        Require(
            value: furledCreate.Any(m => m.Name == "get_sharedMesh")
                && furledCreate.Any(m => m.Name == "get_sharedMaterials")
                && !furledCreate.Any(m =>
                    m.Name is "set_vertices" or "set_triangles" or "set_color"
                ),
            message: "Native furled mesh and rope materials must remain shared read-only assets."
        );
        Require(
            value: !furledPose.Any(m =>
                m.Name is "set_sharedMesh" or "set_vertices" or "set_bindposes"
            ) && furledPose.Any(m => m.Name == "set_sharedMaterials"),
            message: "Furled recoloring must replace the cloth material slot without altering native geometry."
        );
        Require(
            value: runtime.Any(m =>
                m.DeclaringType.FullName == family + "SpritsailFurledVisual" && m.Name == "Pose"
            ),
            message: "The struck native renderer must be updated alongside reefing."
        );
        Console.WriteLine(
            "PASS (structural): shared upper-sprit purchase routing, projected-area integration and owned pocket cleanup without Cloth/rigidbody mutation."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new Exception(message: message);
    }
}
