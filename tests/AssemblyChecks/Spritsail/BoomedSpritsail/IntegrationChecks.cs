using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Spritsail.BoomedSpritsail;

// Guards installed single-sheet contracts, ownership and isolated runtime integration without constructing Unity objects.
internal static class IntegrationChecks
{
    private const BindingFlags All =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private const string Family = "MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.";

    internal static void Run(Assembly assembly)
    {
        Type Own(string name) => assembly.GetType(name: Family + name, throwOnError: true);
        MethodInfo Method(string type, string name) =>
            Own(name: type).GetMethod(name: name, bindingAttr: All);
        var builder = Own(name: "BoomedSpritsail");
        Require(
            value: (int)
                builder.GetField(name: "SourceIndex", bindingAttr: All).GetRawConstantValue() == 15,
            message: "Use the inspected native full-gaff donor for single-sheet controls."
        );
        var register = Method(type: "BoomedSpritsail", name: "Register");
        Require(
            value: IlReader.CalledMethods(method: register).Count(m => m.Name == "RegisterMark")
                == 2,
            message: "Register both marks independently through the same boomed builder."
        );
        foreach (
            var mark in new[]
            {
                (Name: "MkA", Id: 406, Label: "Boomed Spritsail Mk.A"),
                (Name: "MkB", Id: 407, Label: "Boomed Spritsail Mk.B"),
            }
        )
        {
            var type = Own(name: mark.Name + ".BoomedSpritsail" + mark.Name);
            Require(
                value: (int)
                    type.GetField(name: "PrefabIndex", bindingAttr: All).GetRawConstantValue()
                    == mark.Id,
                message: "Boomed IDs must be stable and separate from IDs 400–405."
            );
            Require(
                value: (string)
                    Method(type: "BoomedSpritsail", name: "DisplayName")
                        .Invoke(obj: null, parameters: new object[] { mark.Id }) == mark.Label,
                message: "HUD names must distinguish both boomed marks."
            );
        }
        var construction = IlReader
            .CalledMethods(method: Method(type: "BoomedSpritsail", name: "RegisterMark"))
            .ToArray();
        foreach (
            string required in new[]
            {
                "ValidateCategory",
                "Register",
                "Unregister",
                "Resize",
                "Destroy",
                "SetActive",
                "SetSailArea",
            }
        )
            Require(
                value: construction.Any(m => m.Name == required),
                message: "Registration/rollback must retain " + required + "."
            );
        var registrationPatch = Method(
            type: "Patches.BoomedSpritsailRegistrationPatch",
            name: "Postfix"
        );
        Require(
            value: registrationPatch
                .GetCustomAttribute<HarmonyAfter>()
                .info.after.Contains("com.nandbrew.shipyardexpansion")
                && registrationPatch
                    .GetCustomAttribute<HarmonyBefore>()
                    .info.before.Contains("NatoriusG.AllSailsAllShipyards"),
            message: "Register after SE construction and before AllSails caches the catalog."
        );

        var native = Assembly.Load(assemblyString: "Assembly-CSharp");
        var mast = native.GetType(name: "Mast", throwOnError: true);
        var nativeBinding = IlReader
            .Instructions(
                method: mast.GetMethod(name: "UpdateControllerAttachments", bindingAttr: All)
            )
            .ToArray();
        foreach (
            string field in new[]
            {
                "angleControllerMid",
                "reefController",
                "midAngleWinch",
                "reefWinch",
                "mastOrder",
                "midRopeAttachment",
            }
        )
            Require(
                value: nativeBinding.Any(i => i.Operand is FieldInfo f && f.Name == field),
                message: "Installed native gaff binding contract changed: " + field + "."
            );
        Require(
            value: !nativeBinding.Any(i => i.Operand is FieldInfo f && f.Name == "category"),
            message: "Native single-sheet binding must remain usable by category 6."
        );
        var controller = native.GetType(name: "RopeControllerSailAngle", throwOnError: true);
        var limits = IlReader
            .Instructions(method: controller.GetMethod(name: "UpdateLimits", bindingAttr: All))
            .ToArray();
        foreach (string field in new[] { "limitBoth", "currentLength", "minAngle", "maxAngle" })
            Require(
                value: limits.Any(i => i.Operand is FieldInfo f && f.Name == field),
                message: "Native gaff controller must constrain both tacks from one sheet."
            );

        var configure = IlReader
            .Instructions(method: Method(type: "BoomedSpritsailRig", name: "Configure"))
            .ToArray();
        Require(
            value: configure.Any(i => i.Operand is FieldInfo f && f.Name == "angleControllerMid")
                && configure.Any(i =>
                    i.Code == OpCodes.Stfld && i.Operand is FieldInfo f && f.Name == "attachment"
                ),
            message: "Construction must attach the native single sheet to an independent endpoint."
        );
        int reefDirection = Array.FindIndex(
            configure,
            i => i.Code == OpCodes.Stfld && i.Operand is FieldInfo f && f.Name == "reverseReefing"
        );
        Require(
            value: reefDirection > 0 && configure[reefDirection - 1].Code == OpCodes.Ldc_I4_0,
            message: "Boomed construction must override the gaff donor: paid-out rope deploys, hauling reefs."
        );
        var nativeReef = native.GetType(name: "RopeControllerSailReef", throwOnError: true);
        var unroll = IlReader
            .Instructions(method: nativeReef.GetMethod(name: "UpdateSailUnroll", bindingAttr: All))
            .ToArray();
        int directLength = Array.FindLastIndex(
            unroll,
            i => i.Operand is FieldInfo f && f.Name == "currentLength"
        );
        Require(
            value: unroll.Any(i => i.Operand is FieldInfo f && f.Name == "reverseReefing")
                && directLength >= 0
                && unroll[directLength + 1].Code == OpCodes.Stfld
                && unroll[directLength + 1].Operand is FieldInfo unrollField
                && unrollField.Name == "currentUnroll",
            message: "Installed normal reefing must map paid-out length directly to deployment."
        );
        var resistance = IlReader
            .Instructions(
                method: nativeReef.GetMethod(name: "UpdateWindResistance", bindingAttr: All)
            )
            .ToArray();
        foreach (
            string field in new[]
            {
                "appliedWindForce",
                "windPullMult",
                "weightMult",
                "currentResistance",
            }
        )
            Require(
                value: resistance.Any(i => i.Operand is FieldInfo f && f.Name == field),
                message: "Native reefing must retain weight and wind resistance: " + field + "."
            );
        Require(
            value: !configure.Any(i =>
                i.Code == OpCodes.Stfld
                && i.Operand is FieldInfo f
                && f.DeclaringType == nativeReef
                && (f.Name == "weightMult" || f.Name == "windPullMult")
            ),
            message: "Changing deployment direction must retain the donor's loaded hauling resistance."
        );
        var configureCalls = IlReader
            .CalledMethods(method: Method(type: "BoomedSpritsailRig", name: "Configure"))
            .ToArray();
        Require(
            value: configureCalls.Any(m => m.Name == "DestroyImmediate")
                && configureCalls.Any(m =>
                    m.Name == "AddComponent"
                    && m is MethodInfo method
                    && method.IsGenericMethod
                    && method.GetGenericArguments()[0].Name == "Cloth"
                ),
            message: "New topology needs fresh Cloth on the inactive clone."
        );
        var runtime = IlReader
            .CalledMethods(method: Method(type: "BoomedSpritsailRig", name: "LateUpdate"))
            .ToArray();
        Require(
            value: runtime.Any(m =>
                m.DeclaringType.Name == "BoomedSpritsailDeployment" && m.Name == "Evaluate"
            )
                && runtime.Any(m =>
                    m.DeclaringType.Name == "BoomedSpritsailBoom" && m.Name == "Pose"
                )
                && runtime.Any(m => m.Name == "ExposedArea")
                && runtime.Any(m => m.Name == "UpdateHalyard"),
            message: "The boomed pose must drive boom, cloth, purchase and exposed area together."
        );
        foreach (string methodName in new[] { "LateUpdate", "UpdateShapeBones" })
            Require(
                value: !IlReader
                    .CalledMethods(method: Method(type: "BoomedSpritsailRig", name: methodName))
                    .Any(m =>
                        m.Name
                            is "set_sharedMesh"
                                or "set_bindposes"
                                or "AddComponent"
                                or "DestroyImmediate"
                    ),
                message: "Runtime posing must retain initialized Cloth topology."
            );
        var boomedTypes = assembly
            .GetTypes()
            .Where(t => t.FullName.StartsWith(Family, StringComparison.Ordinal))
            .ToArray();
        foreach (var type in boomedTypes)
        foreach (
            var method in type.GetMethods(All | BindingFlags.DeclaredOnly)
                .Where(m => m.GetMethodBody() != null)
        )
            Require(
                value: !IlReader
                    .CalledMethods(method: method)
                    .Any(m =>
                        m.DeclaringType.Name
                            is "FishermanWinchControls"
                                or "JibAngleMaster"
                                or "LooseFootedSpritsailFlex"
                    ),
                message: "Boomed runtime must not acquire paired controls or loose-footed flex."
            );
        Require(
            value: !boomedTypes.Any(t =>
                t.GetCustomAttributes<HarmonyPatch>()
                    .Any(p =>
                        p.info.declaringType == mast
                        && p.info.methodName == "UpdateControllerAttachments"
                    )
            ),
            message: "Boomed sails must remain on the native binding path."
        );

        var collision = IlReader
            .Instructions(method: Method(type: "BoomedSpritsailRig", name: "RefreshSparCollision"))
            .ToArray();
        Require(
            value: collision.Any(i =>
                i.Operand is string s && s == "BoomedSpritsail deployed sprit"
            ) && collision.Any(i => i.Operand is string s && s == "BoomedSpritsail deployed boom"),
            message: "Fitting must include the deployed sprit and boom."
        );
        var nativeAwake = native
            .GetType(name: "ShipyardSailColChecker", throwOnError: true)
            .GetMethod(name: "Awake", bindingAttr: All);
        var nativeAwakeCalls = IlReader.CalledMethods(method: nativeAwake).ToArray();
        Require(
            value: nativeAwakeCalls.Any(m =>
                m.Name == "GetEnumerator" && m.DeclaringType.Name == "Transform"
            )
                && nativeAwakeCalls.Any(m =>
                    m.Name == "GetComponent"
                    && m is MethodInfo generic
                    && generic.IsGenericMethod
                    && generic.GetGenericArguments()[0].Name == "Collider"
                )
                && nativeAwakeCalls.Any(m =>
                    m.Name == "AddComponent"
                    && m is MethodInfo generic
                    && generic.IsGenericMethod
                    && generic.GetGenericArguments()[0].Name == "ShipyardSailColCheckerSub"
                ),
            message: "Installed checker Awake requires a collider and initializes reporting on every direct child."
        );
        var constructionIL = IlReader
            .Instructions(method: Method(type: "BoomedSpritsailRig", name: "ConfigureCollision"))
            .ToArray();
        int destroy = Array.FindIndex(
            constructionIL,
            i => i.Operand is MethodInfo m && m.Name == "DestroyImmediate"
        );
        Require(
            value: destroy > 0
                && constructionIL[destroy - 1].Operand is MethodInfo objectGetter
                && objectGetter.Name == "get_gameObject",
            message: "Remove complete donor collider objects; deleting only components leaves children that crash native Awake."
        );
        Require(
            value: IlReader
                .CalledMethods(
                    method: Method(type: "BoomedSpritsailRig", name: "ConfigureCollision")
                )
                .Count(m => m.Name == "CreateSparCollider") == 2
                && configureCalls.Any(m => m.Name == "RefreshSparCollision"),
            message: "Both deployed spar colliders must be created and posed on the inactive template before native Awake."
        );
        Require(
            value: !IlReader
                .CalledMethods(
                    method: Method(type: "BoomedSpritsailRig", name: "RefreshSparCollision")
                )
                .Any(m =>
                    m.Name == "AddComponent"
                    || (m is ConstructorInfo && m.DeclaringType.Name == "GameObject")
                ),
            message: "Runtime collision refresh must reuse native-initialized shapes, never add unregistered children."
        );
        var travel = IlReader
            .CalledMethods(
                method: Method(type: "Patches.BoomedSpritsailTravelPatch", name: "Postfix")
            )
            .ToArray();
        Require(
            value: travel.Any(m => m.Name == "ConstrainHinge"),
            message: "Clamp after native late limits without widening collision stops."
        );
        var forceRestore = IlReader
            .Instructions(
                method: Method(type: "Patches.BoomedSpritsailWindFramePatch", name: "Finalizer")
            )
            .ToArray();
        Require(
            value: forceRestore.Any(i =>
                i.Code == OpCodes.Stfld && i.Operand is FieldInfo f && f.Name == "currentUnroll"
            ),
            message: "Restore deployment input/save state after the force calculation, including exceptions."
        );
        var order = Method(type: "Patches.BoomedSpritsailOrderTextPatch", name: "Prefix");
        Require(
            value: order
                .GetCustomAttribute<HarmonyBefore>()
                .info.before.Contains("com.nandbrew.nandfixes")
                && order.GetParameters()[0].ParameterType.IsByRef,
            message: "Consume wrapped order input before NANDFixes."
        );
        var hud = assembly
            .GetType(
                name: "MoreSailwindSails.Compatibility.Patches.SailInfoNamesPatch",
                throwOnError: true
            )
            .GetMethod(name: "Prefix", bindingAttr: All);
        Require(
            value: IlReader
                .CalledMethods(method: hud)
                .Any(m => m.DeclaringType == builder && m.Name == "DisplayName"),
            message: "SailInfo must resolve boomed mark identities."
        );
        Console.WriteLine(
            "PASS (structural): boomed 406/407 registration and rollback, installed gaff single-sheet contract, native binding, fixed topology, deployed spars and pre-Awake collider lifecycle, force restoration, order guards and HUD names. Unity lifecycle and rendering not executed."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message: message);
    }
}
