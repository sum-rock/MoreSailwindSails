using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;
using FlyingSailChecks = MoreSailwindSails.Tests.AssemblyChecks.FishermansFlyingSail.PatchChecks;
using StayChecks = MoreSailwindSails.Tests.AssemblyChecks.FishermansStay.PatchChecks;

namespace MoreSailwindSails.Tests.AssemblyChecks;

internal static class Program
{
    private static void Main(string[] args)
    {
        // Validate the installed game's actual signatures without starting Unity or
        // patching the user's game process. No game objects or saves are created.
        string gameDir =
            args.Length > 0
                ? args[0]
                : Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local/share/Steam/steamapps/common/Sailwind"
                );
        string[] libraryDirs =
        {
            Path.Combine(gameDir, "Sailwind_Data/Managed"),
            Path.Combine(gameDir, "BepInEx/core"),
        };
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            if (name.Name is "ShipyardExpansion" or "SE_Bridge")
                throw new Exception("Assembly checks must run without resolving SE: " + name.Name);
            foreach (var dir in libraryDirs)
            {
                var file = Path.Combine(dir, name.Name + ".dll");
                if (File.Exists(file))
                    return context.LoadFromAssemblyPath(file);
            }
            return null;
        };
        var assembly = Assembly.LoadFrom(
            Path.Combine(AppContext.BaseDirectory, "MoreSailwindSails.dll")
        );

        PluginMetadataChecks.Run(assembly: assembly);
        Visuals.RoutedRopeChecks.Run(assembly: assembly);
        HarmonySignatureChecks.Run(assembly);
        LooseFootedSpritsail.IntegrationChecks.Run(assembly: assembly);
        Utils.ProfileIntegrationChecks.Run(assembly: assembly);
        Utils.ProfileConfigurationChecks.Run(assembly: assembly);
        Controls.SheetFallbackConfigurationChecks.Run(assembly: assembly);
        Compatibility.OrderTextChecks.Run(assembly: assembly);
        Compatibility.NativeAppearanceChecks.Run(assembly);
        Compatibility.ShipyardExpansionTextureChecks.Run(assembly: assembly);
        Compatibility.SailInfoNamesChecks.Run(assembly: assembly, gameDir: gameDir);
        FlyingSailChecks.Run(assembly);
        StayChecks.Run(assembly);
        FishermansStay.CollarChecks.Run(assembly);
        Controls.Runtime.WinchChecks.Run(assembly);
        Controls.NativeResolverChecks.Run(assembly);
        Controls.SailControlInterfaceChecks.Run(assembly);
        Utils.WinchDiagnosticChecks.Run(assembly);
        FishermansStaysail.RegistrationChecks.Run(assembly);
        FishermansStaysail.MkA.PatchChecks.Run(assembly);
        FishermansStaysail.MkB.PatchChecks.Run(assembly);
        FishermansStaysail.MkC.PatchChecks.Run(assembly);
        FishermansStaysail.SailInfoChecks.Run(assembly, gameDir);
    }
}
