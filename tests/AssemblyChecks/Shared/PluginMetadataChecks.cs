using System;
using System.Linq;
using System.Reflection;

namespace MoreSailwindSails.Tests.AssemblyChecks.Shared;

// Constructs the real loader attribute so invalid BepInEx versions fail before shipping a DLL.
internal static class PluginMetadataChecks
{
    internal static void Run(Assembly assembly)
    {
        var plugin = assembly.GetType(name: "MoreSailwindSails.Plugin", throwOnError: true);
        var metadata = plugin
            .GetCustomAttributes(inherit: false)
            .Single(attribute => attribute.GetType().FullName == "BepInEx.BepInPlugin");
        var versionProperty = metadata.GetType().GetProperty(name: "Version");
        var version = versionProperty.GetValue(obj: metadata) as Version;
        if (version == null)
            throw new Exception(
                message: "BepInEx rejects the built plugin's version metadata; use a numeric runtime version."
            );

        var display = (string)plugin.GetField(name: "PluginVersion").GetRawConstantValue();
        var expected = Version.Parse(input: display.Split('-')[0]);
        if (version != expected)
            throw new Exception(
                message: "Loader metadata must match the numeric part of the development version."
            );
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            .InformationalVersion;
        if (informational.Split('+')[0] != display)
            throw new Exception(
                message: "The project version and startup display version must stay aligned."
            );

        // Exercise the actual installed parser with the value that caused the observed load failure.
        var invalid = Activator.CreateInstance(
            type: metadata.GetType(),
            args: new object[] { "com.august.moresailwindsails", "MoreSailwindSails", "0.3.0-dev" }
        );
        if (versionProperty.GetValue(obj: invalid) != null)
            throw new Exception(
                message: "Installed BepInEx version parsing changed; review the prerelease metadata regression."
            );

        Console.WriteLine(
            $"PASS: installed BepInEx accepts built plugin metadata {version}; development label {display} matches the project; prerelease loader regression reproduced."
        );
    }
}
