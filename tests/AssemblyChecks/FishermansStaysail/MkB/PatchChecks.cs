using System;
using System.Linq;
using System.Reflection;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.FishermansStaysail.MkB;

internal static class PatchChecks
{
    internal static void Run(Assembly assembly)
    {
        const string family = "MoreSailwindSails.Sails.FishermansStaysail.";
        const BindingFlags all =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.Instance;
        Type Type(string name) => assembly.GetType(family + name, true);
        MethodInfo Method(string type, string name) => Type(type).GetMethod(name, all);
        if (
            (int)
                Type("MkB.FishermansStaysailMkB").GetField("PrefabIndex", all).GetRawConstantValue()
                != 402
            || (string)
                Type("MkB.FishermansStaysailMkB").GetField("DisplayName", all).GetRawConstantValue()
                != "Fisherman's Staysail Mk.B"
        )
            throw new Exception("Mk.B save identity changed.");
        if (
            !CalledMethods(Method("Patches.FishermansStaysailRegistrationPatch", "Postfix"))
                .Any(m =>
                    m.DeclaringType == Type("MkB.FishermansStaysailMkB") && m.Name == "Register"
                )
        )
            throw new Exception("Mk.B is absent from prefab registration.");
        foreach (string patch in new[] { "Shipyard", "ShipyardFallback" })
        {
            string method = patch == "Shipyard" ? "Postfix" : "Prefix";
            if (
                !CalledMethods(Method("Patches.FishermansStaysail" + patch + "Patch", method))
                    .Any(m =>
                        m.DeclaringType == Type("MkB.FishermansStaysailMkB")
                        && m.Name == "AddToShipyard"
                    )
            )
                throw new Exception("Mk.B is absent from shipyard insertion: " + patch);
        }
        if (
            Type("MkB.FishermansStaysailMkBShape").BaseType != Type("FishermansStaysailShape")
            || (float)
                Type("FishermansStaysailFixedHead")
                    .GetField("DeployedAngle", all)
                    .GetRawConstantValue() != 14f
        )
            throw new Exception("Mk.B does not use the shared rig's 14-degree fixed head.");
        Console.WriteLine("PASS: Mk.B index 402, shipyard registration and shared fixed-head rig.");
    }
}
