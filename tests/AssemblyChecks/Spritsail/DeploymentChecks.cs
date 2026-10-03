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
        var visual = IlReader
            .CalledMethods(method: Method(type: "SpritsailSnotter", name: "Pose"))
            .ToArray();
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
                .Any(m => m.Name == "Destroy"),
            message: "Pocket mesh must be disposed with its owner."
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
