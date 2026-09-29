using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.Compatibility;

// Verifies the optional naming contract without creating Unity objects or patch stubs.
internal static class SailInfoNamesChecks
{
    private const BindingFlags All =
        BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Run(Assembly assembly, string gameDir)
    {
        var patch = assembly.GetType(
            name: "MoreSailwindSails.Compatibility.Patches.SailInfoNamesPatch",
            throwOnError: true
        );
        var find = patch.GetMethod(name: "FindTarget", bindingAttr: All);
        var prefix = patch.GetMethod(name: "Prefix", bindingAttr: All);
        var sailType = prefix.GetParameters()[0].ParameterType;
        MethodInfo Find(Type type) =>
            (MethodInfo)find.Invoke(obj: null, parameters: new object[] { type });

        if (Find(type: null) != null || Find(type: typeof(string)) != null)
            throw new Exception("Missing SailInfo must not require a naming patch.");

        var module = AssemblyBuilder
            .DefineDynamicAssembly(
                name: new AssemblyName("SailInfoNamingContracts"),
                access: AssemblyBuilderAccess.Run
            )
            .DefineDynamicModule(name: "Contracts");
        Type Contract(
            string name,
            Type fieldType,
            Type returnType,
            bool staticMethod = false,
            bool staticField = false,
            bool parameter = false,
            bool generic = false
        )
        {
            var type = module.DefineType(name: name, attr: TypeAttributes.Public);
            if (fieldType != null)
                type.DefineField(
                    fieldName: "sailComponent",
                    type: fieldType,
                    attributes: FieldAttributes.Public | (staticField ? FieldAttributes.Static : 0)
                );
            var method = type.DefineMethod(
                name: "SailName",
                attributes: MethodAttributes.Public | (staticMethod ? MethodAttributes.Static : 0),
                returnType: returnType,
                parameterTypes: parameter ? new[] { typeof(int) } : Type.EmptyTypes
            );
            if (generic)
                method.DefineGenericParameters(names: new[] { "T" });
            var il = method.GetILGenerator();
            il.Emit(opcode: returnType == typeof(int) ? OpCodes.Ldc_I4_0 : OpCodes.Ldnull);
            il.Emit(opcode: OpCodes.Ret);
            return type.CreateType();
        }

        var valid = Contract(name: "Valid", fieldType: sailType, returnType: typeof(string));
        if (Find(type: valid) != valid.GetMethod(name: "SailName"))
            throw new Exception("Compatible SailInfo naming signature was rejected.");
        foreach (
            var incompatible in new[]
            {
                Contract(name: "NoField", fieldType: null, returnType: typeof(string)),
                Contract(name: "WrongField", fieldType: typeof(object), returnType: typeof(string)),
                Contract(name: "WrongReturn", fieldType: sailType, returnType: typeof(int)),
                Contract(
                    name: "StaticMethod",
                    fieldType: sailType,
                    returnType: typeof(string),
                    staticMethod: true
                ),
                Contract(
                    name: "StaticField",
                    fieldType: sailType,
                    returnType: typeof(string),
                    staticField: true
                ),
                Contract(
                    name: "Parameter",
                    fieldType: sailType,
                    returnType: typeof(string),
                    parameter: true
                ),
                Contract(
                    name: "Generic",
                    fieldType: sailType,
                    returnType: typeof(string),
                    generic: true
                ),
            }
        )
            if (Find(type: incompatible) != null)
                throw new Exception(
                    "Incompatible SailInfo naming signature accepted: " + incompatible.Name
                );

        var calls = CalledMethods(method: prefix).OfType<MethodInfo>().ToArray();
        foreach (string family in new[] { "FishermansFlyingSail", "FishermansStaysail" })
            if (
                !calls.Any(method =>
                    method.Name == "GetComponent"
                    && method.IsGenericMethod
                    && method.GetGenericArguments()[0].FullName
                        == $"MoreSailwindSails.Sails.{family}.{family}Rig"
                )
            )
                throw new Exception("SailInfo naming must recognize the custom family: " + family);
        var instructions = Instructions(method: prefix).ToArray();
        if (
            !instructions.Any(instruction =>
                instruction.Code == OpCodes.Ldfld
                && instruction.Operand is FieldInfo field
                && field.DeclaringType == sailType
                && field.Name == "sailName"
            )
            || !calls.Any(method =>
                method.DeclaringType == typeof(string) && method.Name == "IsNullOrEmpty"
            )
            || instructions.Any(instruction =>
                instruction.Code == OpCodes.Stfld || instruction.Code == OpCodes.Stsfld
            )
        )
            throw new Exception(
                "Naming must read the current nonempty sail name without rewriting state."
            );

        var startup = assembly
            .GetType(name: "MoreSailwindSails.Plugin", throwOnError: true)
            .GetMethod(name: "Awake", bindingAttr: All);
        var startupCalls = CalledMethods(method: startup).ToArray();
        if (
            startupCalls.Count(method => method.DeclaringType == patch && method.Name == "Install")
                != 1
            || !startupCalls.Any(method =>
                method.DeclaringType.Name == "FishermansStaysailSailInfoPatch"
                && method.Name == "Install"
            )
            || assembly.GetReferencedAssemblies().Any(reference => reference.Name == "SailInfo")
        )
            throw new Exception(
                "Naming and angle integrations must install independently with no SailInfo assembly dependency."
            );

        Console.WriteLine(
            "PASS: optional SailInfo naming contract rejection, family scoping, current-name access and startup wiring."
        );
        var file = Path.Combine(gameDir, "BepInEx/plugins/SailInfo.dll");
        if (!File.Exists(path: file))
        {
            Console.WriteLine(
                "SKIP: optional installed SailInfo naming signature check (DLL absent)."
            );
            return;
        }
        var installed = Assembly
            .LoadFrom(assemblyFile: file)
            .GetType(name: "SailInfo.WinchInfoSail", throwOnError: true);
        var target = Find(type: installed);
        var hud = installed.GetMethod(name: "WinchHUD", bindingAttr: All);
        if (target == null || CalledMethods(method: hud).Count(method => method == target) != 2)
            throw new Exception(
                "Naming must target both installed SailInfo sheet and halyard HUD calls."
            );
        foreach (var parameter in prefix.GetParameters())
        {
            var expected =
                parameter.Name == "__result"
                    ? target.ReturnType.MakeByRefType()
                    : installed
                        .GetField(name: parameter.Name.Substring(startIndex: 3), bindingAttr: All)
                        ?.FieldType;
            if (expected != parameter.ParameterType)
                throw new Exception(
                    "Invalid optional SailInfo naming injection: " + parameter.Name
                );
        }
        var hudInstructions = Instructions(method: hud).ToArray();
        if (
            !hudInstructions.Any(instruction =>
                instruction.Operand is string text && text.Contains(value: " Halyard")
            )
            || hudInstructions.Count(instruction =>
                instruction.Operand is FieldInfo field && field.Name == "sailNameConfig"
            ) != 2
        )
            throw new Exception(
                "Installed SailInfo HUD naming settings/suffix changed; review integration."
            );
        Console.WriteLine(
            "PASS: installed SailInfo naming target, injected sail reference, sheet/halyard callers and HUD settings/suffix structure; rendered labels require in-game validation."
        );
    }
}
