using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Spritsail;

// Validates installed category contracts and transforms real native IL without starting Unity.
internal static class CategoryChecks
{
    private const BindingFlags All =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private const string Family = "MoreSailwindSails.Sails.Spritsail.";

    internal static void Run(Assembly assembly, string gameDir)
    {
        Type Own(string name) => assembly.GetType(name: Family + name, throwOnError: true);
        MethodInfo Method(string name, string method) =>
            Own(name).GetMethod(name: method, bindingAttr: All);
        var native = Assembly.Load(assemblyString: "Assembly-CSharp");
        var sail = native.GetType(name: "Sail", throwOnError: true);
        var category = native.GetType(name: "SailCategory", throwOnError: true);
        Require(
            value: Enum.GetValues(category)
                .Cast<object>()
                .Select(Convert.ToInt32)
                .SequenceEqual(new[] { 0, 1, 2, 3, 4, 5 }),
            message: "Native categories changed; review reserved Spritsails category 6."
        );
        Require(
            value: Convert.ToInt32(
                Own("SpritsailCategory")
                    .GetField(name: "Value", bindingAttr: All)
                    .GetRawConstantValue()
            ) == 6,
            message: "Use the reserved category consistently."
        );

        var apply = sail.GetMethod(name: "ApplyForce", bindingAttr: All);
        var original = IlReader
            .Instructions(method: apply)
            .Select(i => new CodeInstruction(opcode: i.Code, operand: i.Operand))
            .ToArray();
        var force = Method("Patches.SpritsailForcePatch", "Transpiler");
        var transformed = Transform(method: force, original: original);
        Require(
            value: transformed.Length == original.Length + 2,
            message: "Inject only the local propulsion scale, keeping native application intact."
        );
        int at = Array.FindIndex(
            transformed,
            c => c.operand is MethodInfo m && m.Name == "ScalePropulsion"
        );
        Require(
            value: at >= 2
                && transformed[at - 1].opcode == OpCodes.Ldarg_0
                && transformed[at - 2].operand is MethodInfo power
                && power.Name == "GetRealSailPower",
            message: "Scale the propulsion-local result immediately after native power calculation."
        );
        Require(
            value: transformed
                .Where((_, index) => index != at && index != at - 1)
                .SequenceEqual(original),
            message: "All original force instructions must survive unchanged."
        );
        ExpectRejected(method: force, codes: Array.Empty<CodeInstruction>());
        ExpectRejected(method: force, codes: original.Concat(original).ToArray());

        var installer = native.GetType(name: "ShipyardSailInstaller", throwOnError: true);
        var overlap = installer.GetMethod(name: "CheckSailOverlap", bindingAttr: All);
        original = IlReader
            .Instructions(method: overlap)
            .Select(i => new CodeInstruction(opcode: i.Code, operand: i.Operand))
            .ToArray();
        var overlapTransform = Method("Patches.SpritsailOverlapPatch", "Transpiler");
        var field = sail.GetField(name: "category", bindingAttr: All);
        int reads = original.Count(c => c.LoadsField(field: field));
        transformed = Transform(method: overlapTransform, original: original);
        Require(
            value: reads == 4
                && transformed.Length == original.Length
                && transformed.Count(c => c.operand is MethodInfo m && m.Name == "OverlapCategory")
                    == 4,
            message: "Map all four category reads so overlap works in both installation orders."
        );
        Require(
            value: !transformed.Any(c => c.opcode == OpCodes.Stfld && Equals(c.operand, field)),
            message: "Overlap must never temporarily change live sail categories."
        );
        ExpectRejected(method: overlapTransform, codes: Array.Empty<CodeInstruction>());

        foreach (string name in new[] { "SpritsailMassPatch", "SpritsailPricePatch" })
            Require(
                value: !IlReader
                    .CalledMethods(method: Method("Patches." + name, "Prefix"))
                    .Any(m => m.Name == "ScalePropulsion"),
                message: "Price and mass must stay independent of force tuning."
            );

        Require(
            value: IlReader
                .CalledMethods(method: Method("SpritsailCatalog", "ValidateCategory"))
                .Any(m => m.Name == "IsDefined"),
            message: "Reject native category conflicts before constructing a template."
        );
        var registerCalls = IlReader
            .CalledMethods(
                method: Method("LooseFootedSpritsail.LooseFootedSpritsail", "RegisterMark")
            )
            .ToArray();
        foreach (
            string name in new[] { "ValidateCategory", "Initialize", "Register", "Unregister" }
        )
            Require(
                value: registerCalls.Any(m =>
                    m.Name == name && m.DeclaringType.Namespace == Family.TrimEnd('.')
                ),
                message: "Mk.A registration must participate in category validation, initialization, catalog and rollback: "
                    + name
            );

        var scalerCalls = IlReader
            .Instructions(method: Method("Patches.SpritsailScalerPatch", "Postfix"))
            .Where(i => i.Code == OpCodes.Stfld)
            .Select(i => ((FieldInfo)i.Operand).Name)
            .ToArray();
        foreach (string name in new[] { "rotatablePart", "scaleType", "flippable" })
            Require(
                value: scalerCalls.Contains(name),
                message: "Explicit scaler initialization is missing: " + name
            );
        var browse = Method("Patches.SpritsailBrowsePatch", "Prefix");
        Require(
            value: browse
                .GetCustomAttribute<HarmonyBefore>()
                .info.before.Contains("NatoriusG.AllSailsAllShipyards"),
            message: "Insert custom cache entries before All Sails reads them."
        );

        CheckOptionalContract(assembly: assembly, gameDir: gameDir);
        CheckNativeDefaults(native: native, sail: sail, categoryField: field);
        Console.WriteLine(
            "PASS: native category reservation, exactly-once propulsion IL, symmetric overlap IL, registration rollback, scaler and optional paging contracts."
        );
    }

    private static CodeInstruction[] Transform(MethodInfo method, CodeInstruction[] original) =>
        (
            (IEnumerable<CodeInstruction>)
                method.Invoke(obj: null, parameters: new object[] { original })
        ).ToArray();

    private static void ExpectRejected(MethodInfo method, CodeInstruction[] codes)
    {
        try
        {
            Transform(method: method, original: codes);
        }
        catch (InvalidOperationException)
        {
            return;
        }
        throw new Exception(message: "Changed native IL must require integration review.");
    }

    private static void CheckNativeDefaults(Assembly native, Type sail, FieldInfo categoryField)
    {
        var height = IlReader
            .Instructions(method: sail.GetMethod(name: "UseExtendedMastHeight", bindingAttr: All))
            .ToArray();
        var reads = Enumerable
            .Range(start: 0, count: height.Length)
            .Where(i => Equals(height[i].Operand, categoryField))
            .ToArray();
        Require(
            value: reads.Length == 2
                && height[reads[0] + 1].Code == OpCodes.Brfalse_S
                && height[reads[1] + 1].Code == OpCodes.Ldc_I4_1
                && height[reads[1] + 2].Code == OpCodes.Bne_Un_S
                && height[height.Length - 2].Code == OpCodes.Ldc_I4_0,
            message: "Native extended height must continue to reject categories other than square/lateen."
        );

        var shadow = IlReader
            .Instructions(
                method: native
                    .GetType(name: "SailShadowCol", throwOnError: true)
                    .GetMethod(name: "Awake", bindingAttr: All)
            )
            .ToArray();
        int category = Array.FindIndex(shadow, i => Equals(i.Operand, categoryField));
        var triggerCalls = Enumerable
            .Range(start: 0, count: shadow.Length)
            .Where(i => shadow[i].Operand is MethodInfo m && m.Name == "set_isTrigger")
            .ToArray();
        Require(
            value: category >= 0
                && shadow[category + 1].Code == OpCodes.Ldc_I4_5
                && shadow[category + 2].Code == OpCodes.Bne_Un_S
                && triggerCalls.Length == 2
                && shadow[triggerCalls[0] - 1].Code == OpCodes.Ldc_I4_0
                && shadow[triggerCalls[1] - 1].Code == OpCodes.Ldc_I4_1,
            message: "Non-staysail shadow colliders must retain the native trigger path."
        );

        var start = IlReader
            .Instructions(method: sail.GetMethod(name: "Start", bindingAttr: All))
            .ToArray();
        category = Array.FindIndex(start, i => Equals(i.Operand, categoryField));
        Require(
            value: category >= 0
                && start[category + 1].Code == OpCodes.Ldc_I4_5
                && start[category + 2].Code == OpCodes.Bne_Un_S,
            message: "Native startup must leave explicitly initialized spritsail rigidbody settings alone."
        );
        var topsails = IlReader
            .Instructions(
                method: native
                    .GetType(name: "Mast", throwOnError: true)
                    .GetMethod(name: "TopsailsApplyLowestAngle", bindingAttr: All)
            )
            .ToArray();
        category = Array.FindIndex(topsails, i => Equals(i.Operand, categoryField));
        Require(
            value: category >= 0 && topsails[category + 1].Code == OpCodes.Brtrue_S,
            message: "Only square-category sails should join native topsail angle coordination."
        );
    }

    private static void CheckOptionalContract(Assembly assembly, string gameDir)
    {
        Require(
            value: !assembly.GetReferencedAssemblies().Any(a => a.Name == "AllSailsAllShipyards"),
            message: "All Sails must remain optional."
        );
        string file = Path.Combine(gameDir, "BepInEx/plugins/AllSailsAllShipyards.dll");
        if (!File.Exists(file))
        {
            Console.WriteLine("SKIP: installed All Sails paging contract (optional DLL absent).");
            return;
        }
        var optional = Assembly.LoadFrom(assemblyFile: file);
        Type Type(string name) =>
            optional.GetType(name: "AllSailsAllShipyards." + name, throwOnError: true);
        var check = assembly
            .GetType(name: Family + "SpritsailAllSails", throwOnError: true)
            .GetMethod(name: "HasContract", bindingAttr: All);
        Require(
            value: (bool)
                check.Invoke(
                    obj: null,
                    parameters: new object[]
                    {
                        Type("ShipyardUIPatch"),
                        Type("ShipyardSailPage"),
                        Type("ShipyardSailPageButton"),
                        Type("Main"),
                    }
                ),
            message: "Installed All Sails cache/page/navigation API changed."
        );
        Require(
            value: !(bool)
                check.Invoke(
                    obj: null,
                    parameters: new object[]
                    {
                        typeof(string),
                        Type("ShipyardSailPage"),
                        Type("ShipyardSailPageButton"),
                        Type("Main"),
                    }
                ),
            message: "Missing optional contracts must be rejected."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new Exception(message: message);
    }
}
