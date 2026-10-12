using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MoreSailwindSails.Tests.AssemblyChecks.Utils;

// Checks profiling defaults and explicit settings against the installed BepInEx config implementation, using temporary files only.
internal static class ProfileConfigurationChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var profile = assembly.GetType(
            "MoreSailwindSails.Utils.Profiling.PerformanceProfile",
            throwOnError: true
        );
        var configure = profile.GetMethod("Configure", flags);
        var configType = configure.GetParameters()[0].ParameterType;
        var constructor = configType.GetConstructors().Single(c => c.GetParameters().Length == 3);
        string directory = Path.Combine(
            Path.GetTempPath(),
            "mss-profile-config-" + Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(directory);
        try
        {
            void Check(string name, string input, bool expectedEnabled, string expectedShortcut)
            {
                string path = Path.Combine(directory, name + ".cfg");
                File.WriteAllText(path, "[Diagnostics]\n" + input);
                var config = constructor.Invoke(new object[] { path, false, null });
                configure.Invoke(null, new[] { config });
                object Value(string field)
                {
                    var entry = profile.GetField(field, flags).GetValue(null);
                    return entry.GetType().GetProperty("Value").GetValue(entry);
                }
                if (
                    (bool)Value("enabled") != expectedEnabled
                    || Value("shortcut").ToString() != expectedShortcut
                )
                    throw new InvalidOperationException(
                        "Incorrect profiling configuration for " + name
                    );
                configType.GetMethod("Save").Invoke(config, null);
                var saved = File.ReadAllText(path);
                if (!saved.Contains("ProfileTargets = "))
                    throw new InvalidOperationException("Target defaults were not persisted.");
            }
            Check(name: "fresh", input: "", expectedEnabled: false, expectedShortcut: "F7");
            Check(
                name: "explicit",
                input: "EnableProfiling = true\nToggleProfiling = F5\n",
                expectedEnabled: true,
                expectedShortcut: "F5"
            );
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
        Console.WriteLine(
            "PASS: fresh profiling defaults and explicit settings using installed BepInEx configuration."
        );
    }
}
