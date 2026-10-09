using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MoreSailwindSails.Tests.AssemblyChecks.Utils;

// Executes migration against the installed BepInEx config implementation, using temporary files only.
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
                        "Incorrect profiling migration for " + name
                    );
                configType.GetMethod("Save").Invoke(config, null);
                var saved = File.ReadAllText(path);
                if (
                    saved.Contains("EnableSpritsailMountProfiling")
                    || saved.Contains("ToggleSpritsailMountProfiling")
                )
                    throw new InvalidOperationException(
                        "Obsolete profiling keys remained after migration."
                    );
                if (
                    !saved.Contains("ProfileTargets = ")
                    || !saved.Contains("ProfileBypass = SailMount")
                )
                    throw new InvalidOperationException(
                        "New target/bypass defaults were not persisted."
                    );
            }
            Check(name: "fresh", input: "", expectedEnabled: false, expectedShortcut: "F7");
            Check(
                name: "legacy",
                input: "EnableSpritsailMountProfiling = true\nToggleSpritsailMountProfiling = F6\n",
                expectedEnabled: true,
                expectedShortcut: "F6"
            );
            Check(
                name: "explicit",
                input: "EnableSpritsailMountProfiling = true\nToggleSpritsailMountProfiling = F6\nEnableProfiling = false\nToggleProfiling = F5\n",
                expectedEnabled: false,
                expectedShortcut: "F5"
            );
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
        Console.WriteLine(
            "PASS: fresh profiling defaults, legacy migration, new-key precedence, and obsolete-key removal using installed BepInEx configuration."
        );
    }
}
