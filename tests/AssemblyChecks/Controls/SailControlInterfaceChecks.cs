// Checks consumer isolation and native handoff contracts against the compiled plugin.
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.Controls;

internal static class SailControlInterfaceChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags flags =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static;
        Type Find(string name) => assembly.GetType("MoreSailwindSails." + name, true);
        var contract = Find("Controls.ISailControlRig");
        var request = Find("Controls.SailControlRequest");
        var group = Find("Controls.Runtime.WinchGroup");
        var manager = Find("Controls.WinchControls");
        var owned = Find("Controls.Runtime.OwnedWinch");
        var flying = Find("Sails.FishermansFlyingSail.FishermansFlyingSailRigging");
        var stay = Find("Stays.FishermansStay.FishermansStayControls");
        Check(
            contract.IsInterface && contract.GetMethods().Length == 2,
            "Consumers need the request and replacement contract."
        );
        foreach (var consumer in new[] { flying, stay })
            Check(
                contract.IsAssignableFrom(consumer),
                "Missing control consumer: " + consumer.Name
            );
        Check(
            request.IsValueType
                && request.GetField("FittedSheetSpan", flags).FieldType == typeof(float?),
            "Pending geometry must remain distinct from a zero span."
        );
        foreach (
            var type in assembly
                .GetTypes()
                .Where(t => t.Namespace?.StartsWith("MoreSailwindSails.Controls") == true)
        )
        {
            foreach (var field in type.GetFields(flags))
                Check(
                    field.FieldType.Namespace?.StartsWith("MoreSailwindSails.Sails") != true,
                    "Controls holds sail-family state: " + field.Name
                );
            foreach (var method in type.GetMethods(flags).Where(m => m.DeclaringType == type))
                Check(
                    !CalledMethods(method)
                        .Any(m =>
                            m.DeclaringType?.Namespace?.StartsWith("MoreSailwindSails.Sails")
                            == true
                        ),
                    "Controls calls sail geometry: " + method.Name
                );
        }
        var update = CalledMethods(group.GetMethod("UpdateRequest", flags)).ToArray();
        Check(
            update.Any(m => m.Name == "TryGetControlRequest")
                && update.Any(m => m.Name == "Suspend"),
            "Invalid requests must release placements."
        );
        var register = CalledMethods(manager.GetMethod("Register", flags)).ToArray();
        Check(
            Instructions(manager.GetMethod("Register", flags))
                .Any(i => i.Code == OpCodes.Beq || i.Code == OpCodes.Beq_S || i.Code == OpCodes.Ceq)
                && register.Any(m => m.Name == "Publish"),
            "Registration must preserve owner identity and publish initial controls."
        );
        var unregister = CalledMethods(manager.GetMethod("Unregister", flags)).ToArray();
        Check(
            unregister.Any(m => m.Name == "Suspend")
                && unregister.Any(m => m.Name == "Retire")
                && unregister.Any(m => m.Name == "OnControlsChanged"),
            "Unregistration must release claims, preserve controllers and notify the consumer."
        );
        Check(
            CalledMethods(owned.GetMethod("Adopt", flags)).Any(m => m.Name == "Publish"),
            "Replacement controls must reach the consumer."
        );
        foreach (var method in owned.GetMethods(flags).Concat(flying.GetMethods(flags)))
            Check(
                !Instructions(method)
                    .Any(i =>
                        i.Code == OpCodes.Stfld
                        && i.Operand is FieldInfo f
                        && f.DeclaringType.Name == "Mast"
                        && f.Name is "reefWinch" or "leftAngleWinch" or "rightAngleWinch" or "sails"
                    ),
                "Shared runtime or Flying Sails overwrite physical mast ownership."
            );
        var patch = Find("Controls.Patches.NativeSailControlsPatch");
        var handoff = CalledMethods(patch.GetMethod("Postfix", flags)).ToArray();
        Check(
            Array.FindIndex(handoff, m => m.Name == "ReleaseNativeBindings")
                < Array.FindIndex(handoff, m => m.Name == "AttachControls"),
            "Native bindings must be released before the consumer registers."
        );
        var release = Find("Controls.NativeSailControls").GetMethod("ReleaseNative", flags);
        var releaseCalls = CalledMethods(release).ToArray();
        Check(
            releaseCalls.Count(m => m.Name == "op_Equality") == 3
                && releaseCalls.Any(m => m.Name == "ShowWinch")
                && releaseCalls.Any(m => m.Name == "SetParent"),
            "Native handoff must compare all three controller identities and hide released controls."
        );
        foreach (
            string controller in new[]
            {
                "angleControllerLeft",
                "angleControllerRight",
                "reefController",
            }
        )
            Check(
                Instructions(release).Any(i => i.Operand is FieldInfo f && f.Name == controller),
                "Handoff omits " + controller
            );
        Check(
            CalledMethods(flying.GetMethod("OnDisable", flags))
                .Any(m => m.Name == "ReleaseControls")
                && CalledMethods(flying.GetMethod("OnDestroy", flags))
                    .Any(m => m.Name == "ReleaseControls"),
            "Flying controls must be released on disable and destruction."
        );
        Check(
            Instructions(flying.GetMethod("OnEnable", flags))
                .Any(i =>
                    i.Code == OpCodes.Stfld && i.Operand is FieldInfo f && f.Name == "controlsDirty"
                ),
            "Re-enabled Flying Sails must recreate registration."
        );
        var rig = Find("Sails.FishermansFlyingSail.FishermansFlyingSailRig");
        Check(
            CalledMethods(rig.GetMethod("RefreshFlyingFrame", flags))
                .Any(m => m.Name == "SheetSpan")
                && !CalledMethods(rig.GetMethod("get_FittedSheetSpan", flags))
                    .Any(m => m.Name is "GetComponent" or "RefreshFlyingFrame" or "SheetSpan"),
            "Flying span must be calculated at fitting time and read from cache."
        );
        var deck = Instructions(flying.GetMethod("get_DeckPoint", flags)).ToArray();
        Check(
            deck.Any(i => i.Operand is FieldInfo f && f.Name == "reefWinch")
                && !deck.Any(i => i.Operand is FieldInfo f && f.Name == "reefControl"),
            "Deck geometry must retain its original native datum."
        );
        var native = Find("Controls.Discovery.NativeWinchInventory");
        Check(
            CalledMethods(native.GetMethod("IsControlSource", flags))
                .Any(m => m.Name == "IsStaySource"),
            "Matching-stay eligibility must remain available alongside fitted-mast selection."
        );
        Console.WriteLine(
            "PASS (structural): shared control consumers, family isolation, identity-scoped native handoff, physical mast preservation, lifecycle and cached Flying Sail span."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
