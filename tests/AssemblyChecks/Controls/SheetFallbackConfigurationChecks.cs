// Checks startup-only sheet diagnostics using temporary BepInEx config files and resolver wiring.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.Controls;

internal static class SheetFallbackConfigurationChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags flags =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.Instance;
        var plugin = assembly.GetType("MoreSailwindSails.Plugin", throwOnError: true);
        var configure = plugin.GetMethod("ConfigureSheetFallbacks", flags);
        var setting = plugin.GetProperty("ForceSheetFallbacks", flags);
        var configType = configure.GetParameters()[0].ParameterType;
        var configConstructor = configType
            .GetConstructors()
            .Single(c => c.GetParameters().Length == 3);
        var resolver = assembly.GetType(
            "MoreSailwindSails.Controls.Placement.SheetingWinchPlacementResolver",
            true
        );
        var constructor = resolver.GetConstructors(flags).Single();
        var snapshot = resolver.GetField("forceSheetFallbacks", flags);
        Check(snapshot.IsInitOnly, "Resolver must retain an immutable startup snapshot.");
        string directory = Path.Combine(
            Path.GetTempPath(),
            "mss-sheet-config-" + Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(directory);
        try
        {
            foreach (var value in new[] { "", "true", "false" })
            {
                string path = Path.Combine(directory, "config.cfg");
                File.WriteAllText(
                    path,
                    "[Diagnostics]\n" + (value == "" ? "" : "ForceSheetFallbacks = " + value + "\n")
                );
                var config = configConstructor.Invoke(new object[] { path, false, null });
                configure.Invoke(null, new[] { config });
                bool expected = value == "true";
                Check(
                    (bool)setting.GetValue(null) == expected,
                    "Incorrect sheet fallback default or explicit value."
                );
                var instance = constructor.Invoke(new object[] { null, null, expected });
                configType.GetMethod("Save").Invoke(config, null);
                Check(
                    File.ReadAllText(path)
                        .Contains(
                            "ForceSheetFallbacks = " + expected.ToString().ToLowerInvariant()
                        ),
                    "Startup setting must be persisted in the config."
                );
                File.WriteAllText(
                    path,
                    "[Diagnostics]\nForceSheetFallbacks = "
                        + (!expected).ToString().ToLowerInvariant()
                        + "\n"
                );
                configType.GetMethod("Reload").Invoke(config, null);
                Check(
                    (bool)setting.GetValue(null) == expected
                        && (bool)snapshot.GetValue(instance) == expected,
                    "Reloading live configuration must not change the startup snapshot."
                );
                if (expected)
                {
                    var candidateType = assembly.GetType(
                        "MoreSailwindSails.Controls.Placement.WinchPlacementCandidate",
                        true
                    );
                    var candidate = Activator.CreateInstance(candidateType, nonPublic: true);
                    Check(
                        !(bool)
                            resolver
                                .GetMethod("ValidateCurrent", flags)
                                .Invoke(
                                    instance,
                                    new object[] { null, null, candidate, 0f, Type.Missing, null }
                                ),
                        "Forced mode must reject native retention before inspecting native support data."
                    );
                }
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
        Check(
            CalledMethods(plugin.GetMethod("Awake", flags)).Any(m => m == configure)
                && !CalledMethods(plugin.GetMethod("LateUpdate", flags)).Any(m => m == configure),
            "Sheet configuration must be captured at startup only."
        );
        var manager = assembly.GetType("MoreSailwindSails.Controls.WinchControls", true);
        Check(
            CalledMethods(manager.GetMethod("For", flags))
                .Any(m => m.Name == "get_ForceSheetFallbacks"),
            "Winch manager must supply the startup setting to its sheet resolver."
        );
        var resolve = resolver.GetMethod("Resolve", flags);
        var body = Instructions(resolve).ToArray();
        int modeRead = Array.FindIndex(body, i => i.Operand is FieldInfo f && f == snapshot);
        int nativeSearch = Array.FindIndex(
            body,
            i => i.Operand is MethodInfo m && m.Name == "ControlSources"
        );
        int fallbacks = Array.FindIndex(
            body,
            i => i.Operand is MethodInfo m && m.Name == "Fallbacks"
        );
        Check(
            modeRead >= 0
                && modeRead < nativeSearch
                && nativeSearch < fallbacks
                && body[modeRead + 1].Code.FlowControl == FlowControl.Cond_Branch,
            "Forced mode must gate native discovery before the shared fallback path."
        );
        foreach (var method in resolver.GetMethods(flags).Where(m => m.DeclaringType == resolver))
            Check(
                !CalledMethods(method)
                    .Any(m => m.DeclaringType?.Namespace == "BepInEx.Configuration"),
                "Resolver must not read live configuration."
            );
        Console.WriteLine(
            "PASS: sheet fallback startup default/overrides, reload isolation, immutable resolver snapshot and native-retention rejection."
        );
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
