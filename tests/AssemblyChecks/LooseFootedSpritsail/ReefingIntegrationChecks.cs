// Guards native reef ownership, independent retries and pose/physics lifecycle boundaries.
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.LooseFootedSpritsail;

internal static class ReefingIntegrationChecks
{
    private const BindingFlags All =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    internal static void Run(Assembly assembly)
    {
        Type Family(string name) =>
            assembly.GetType(
                name: "MoreSailwindSails.Sails.LooseFootedSpritsail." + name,
                throwOnError: true
            );
        var rig = Family(name: "LooseFootedSpritsailRig");
        var rigging = Family(name: "LooseFootedSpritsailRigging");
        var cloth = Family(name: "LooseFootedSpritsailCloth");
        var prefab = Family(name: "LooseFootedSpritsailPrefab");
        var reef = rigging.GetProperty(name: "Reef", bindingAttr: All).PropertyType;
        Check(
            reef.Name == "RopeControllerSailReef" && reef.Assembly != assembly,
            "Spritsails must retain the installed native reef controller."
        );
        Check(
            !assembly.GetTypes().Any(t => t.IsSubclassOf(c: reef)),
            "No custom reef controller is needed."
        );
        var register = Instructions(method: prefab.GetMethod(name: "Register", bindingAttr: All))
            .ToArray();
        int reverse = Array.FindIndex(
            array: register,
            match: i =>
                i.Code == OpCodes.Stfld && i.Operand is FieldInfo f && f.Name == "reverseReefing"
        );
        Check(
            reverse > 0 && register[reverse - 1].Code == OpCodes.Ldc_I4_0,
            "Winding must decrease deployment using native forward reefing."
        );
        var strip = Instructions(method: prefab.GetMethod(name: "StripDonor", bindingAttr: All));
        Check(
            !strip.Any(i =>
                i.Code == OpCodes.Ldfld && i.Operand is FieldInfo f && f.Name == "reefController"
            ),
            "Donor stripping must not destroy the retained reef controller."
        );

        foreach (var type in new[] { rig, rigging, cloth })
        foreach (var method in type.GetMethods(bindingAttr: All | BindingFlags.DeclaredOnly))
        foreach (var instruction in Instructions(method: method))
            Check(
                !(
                    instruction.Code == OpCodes.Stfld
                    && instruction.Operand is FieldInfo f
                    && f.Name is "currentUnroll" or "currentLength"
                ),
                "Runtime must preserve native reef state across fitting, disable and winch replacement."
            );

        var retry = rigging.GetMethod(name: "RefreshReef", bindingAttr: All);
        Check(
            retry.GetMethodBody().ExceptionHandlingClauses.Count > 0,
            "Missing halyard templates must be retried."
        );
        var retryCalls = CalledMethods(method: retry).ToArray();
        Check(
            !retryCalls.Any(m => m.Name is "ReleaseControls" or "Unregister" or "Retire"),
            "A reef retry must not release sheet claims."
        );
        Check(
            retryCalls.Any(m => m.Name == "FindGuide")
                && retryCalls.Any(m => m.Name == "EnsureControl"),
            "Halyard allocation must discover a usable mast guide and winch."
        );
        Check(
            Calls(type: rigging, method: "EligibleGuide", name: "CanRoute"),
            "Mast routes must shorten as the sprit rises."
        );
        Check(
            Calls(type: rigging, method: "ReleaseControls", name: "Hide"),
            "Unavailable controls must hide their routed ropes."
        );

        var admission = CalledMethods(
                method: rig.GetMethod(name: "PrepareSailing", bindingAttr: All)
            )
            .ToArray();
        Check(
            Array.FindIndex(
                array: admission,
                match: m => m.DeclaringType == rigging && m.Name == "Refresh"
            ) < Array.FindIndex(array: admission, match: m => m.Name == "Clear"),
            "Fully brailed sails must still allocate controls so they can reopen."
        );
        Check(
            Calls(type: rig, method: "PrepareSailing", name: "UpdateBodyMotion")
                && !CalledMethods(method: rig.GetMethod(name: "PrepareSailing", bindingAttr: All))
                    .Any(m => m.Name is "set_localRotation" or "set_localPosition"),
            "Full brail must suppress wind without resetting the sail's pose or anchoring the boat."
        );
        CheckBodyMotion(rig: rig);
        Check(
            Calls(type: rig, method: "PrepareSailing", name: "Clear"),
            "Closing must clear native force caches."
        );
        Check(
            admission.Any(m => m.DeclaringType == cloth && m.Name == "Refresh"),
            "Cloth pose must update before physics admission."
        );
        Check(
            Calls(type: rig, method: "RefreshAerodynamics", name: "get_Peak")
                && Calls(type: rig, method: "RefreshAerodynamics", name: "get_Clew"),
            "Aerodynamics must read posed corners."
        );
        Check(
            !Instructions(method: cloth.GetMethod(name: "Refresh", bindingAttr: All))
                .Any(i =>
                    i.Code == OpCodes.Stfld && i.Operand is FieldInfo f && f.Name == "sailArea"
                ),
            "Deployment must not reduce area a second time."
        );

        var constructor = cloth.GetConstructors(bindingAttr: All).Single(c => !c.IsStatic);
        Check(
            CalledMethods(method: constructor).Any(m => m.Name == "Instantiate")
                && Calls(type: cloth, method: "Release", name: "Destroy"),
            "Dynamic cloth meshes must be owned and released per instance."
        );
        Check(
            Calls(type: cloth, method: "Render", name: "RecalculateNormals")
                && Calls(type: cloth, method: "Render", name: "RecalculateBounds")
                && Calls(type: cloth, method: "UpdateBounds", name: "set_localBounds"),
            "Posed cloth needs updated normals and skinned bounds."
        );
        Check(
            !Calls(type: cloth, method: "Refresh", name: "set_vertices")
                && !Calls(type: cloth, method: "Refresh", name: "Render")
                && !Calls(type: rig, method: "PrepareSailing", name: "Render")
                && !Calls(type: rig, method: "Refresh", name: "Render")
                && Calls(type: rig, method: "LateUpdate", name: "Render")
                && Calls(type: cloth, method: "Render", name: "get_frameCount"),
            "Physics preparation must not upload cloth; visuals belong to a guarded LateUpdate."
        );
        Check(
            Calls(type: cloth, method: "Render", name: "GetCapturedWindFraction")
                && Calls(type: cloth, method: "Render", name: "GetCurrentShadowMult")
                && Calls(type: cloth, method: "Render", name: "get_SailingReady")
                && Calls(type: cloth, method: "Render", name: "VisualVertex")
                && !Calls(type: cloth, method: "Refresh", name: "VisualVertex"),
            "Flutter must use native wind capture after sailing admission and affect only rendered vertices."
        );
        var sail = rig.GetField(name: "Sail", bindingAttr: All).FieldType;
        var vector = sail.GetField(name: "apparentWind", bindingAttr: All).FieldType;
        Check(
            sail.GetMethod(
                name: "GetWindAngle",
                bindingAttr: All,
                binder: null,
                types: new[] { vector },
                modifiers: null
            )?.ReturnType == typeof(float),
            "Preview capture requires the installed native wind-angle calculation."
        );
        Check(
            Calls(type: cloth, method: "Refresh", name: "WindLoad")
                && Calls(type: cloth, method: "Render", name: "RefreshAerodynamics")
                && Calls(type: cloth, method: "Render", name: "CanAnimate"),
            "Preview cloth must sample wind and refresh its posed wind frame independently of propulsion."
        );
        var physics = Family(name: "LooseFootedSpritsailPhysics");
        foreach (var method in cloth.GetMethods(bindingAttr: All | BindingFlags.DeclaredOnly))
            Check(
                !CalledMethods(method: method)
                    .Any(m => m.DeclaringType == physics || m.Name == "UpdateWindForceOnSail")
                    && !Instructions(method: method)
                        .Any(i =>
                            i.Code == OpCodes.Stfld
                            && i.Operand is FieldInfo f
                            && f.DeclaringType == sail
                            && f.Name == "apparentWind"
                        ),
                "Visual preview wind must not repopulate native propulsion caches."
            );
        foreach (string method in new[] { "GetCapturedWindFraction", "GetCurrentShadowMult" })
            Check(
                sail.GetMethod(
                    name: method,
                    bindingAttr: All,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null
                )?.ReturnType == typeof(float),
                "Installed flutter wind contract changed: " + method
            );
        Check(
            Calls(type: rig, method: "OnDestroy", name: "Release"),
            "Destroying a rig must release its cloth mesh."
        );
        Check(
            Calls(type: rig, method: "RefreshCollision", name: "Clearance"),
            "Installation must include the moving sprit and billow envelope."
        );
        var nativeUpdate = Instructions(method: reef.GetMethod(name: "Update", bindingAttr: All))
            .ToArray();
        Check(
            nativeUpdate.Any(i => i.Operand is FieldInfo f && f.Name == "currentlyLoading")
                && nativeUpdate.Any(i => i.Operand is FieldInfo f && f.Name == "justStarted"),
            "Installed native startup behavior changed; review deployment initialization."
        );
        Console.WriteLine(
            "PASS (structural): native spritsail reef controller, startup/state ownership, independent halyard retry, posed physics, full-brail torque suppression and owned mesh cleanup."
        );
    }

    private static void CheckBodyMotion(Type rig)
    {
        var motion = rig.GetMethod(name: "UpdateBodyMotion", bindingAttr: All);
        var calls = CalledMethods(method: motion).ToArray();
        Check(
            calls.Any(m => m.Name == "get_connectedBody")
                && calls.Count(m => m.Name == "get_isKinematic") == 2,
            "Sail body mode must account for the connected boat's actual physics state."
        );
        Check(
            calls.Any(m => m.Name == "GetPointVelocity")
                && calls.Any(m => m.Name == "get_angularVelocity"),
            "A reactivated sail must inherit the boat's motion instead of braking it."
        );
        Check(
            !Instructions(method: motion)
                .Any(i =>
                    i.Operand is FieldInfo f
                    && f.Name is "currentUnroll" or "currentShipyard" or "playing"
                ),
            "Deployment and loading flags must not freeze a sail attached to a dynamic boat."
        );
        var prepare = Instructions(method: rig.GetMethod(name: "PrepareSailing", bindingAttr: All))
            .ToArray();
        int update = Array.FindIndex(
            array: prepare,
            match: i => i.Operand is MethodBase m && m.Name == "UpdateBodyMotion"
        );
        int deployment = Array.FindIndex(
            array: prepare,
            match: i => i.Operand is FieldInfo f && f.Name == "currentUnroll"
        );
        Check(
            update >= 0 && update < deployment,
            "Body motion must recover before a brailed sail can skip native FixedUpdate."
        );
        Check(
            Calls(type: rig, method: "Refresh", name: "UpdateBodyMotion"),
            "Refitting must leave the sail body safe for its connected boat."
        );
        foreach (var method in rig.GetMethods(bindingAttr: All | BindingFlags.DeclaredOnly))
            Check(
                method == motion
                    || !CalledMethods(method: method).Any(m => m.Name == "set_isKinematic"),
                "A readiness or fitting path bypasses connected-body motion handling."
            );
    }

    private static bool Calls(Type type, string method, string name) =>
        CalledMethods(method: type.GetMethod(name: method, bindingAttr: All))
            .Any(m => m.Name == name);

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
