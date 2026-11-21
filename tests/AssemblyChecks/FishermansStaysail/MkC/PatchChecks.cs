using System;
using System.Linq;
using System.Reflection;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.FishermansStaysail.MkC;

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
                Type("MkC.FishermansStaysailMkC").GetField("PrefabIndex", all).GetRawConstantValue()
                != 403
            || (string)
                Type("MkC.FishermansStaysailMkC").GetField("DisplayName", all).GetRawConstantValue()
                != "Fisherman's Staysail Mk.C"
        )
            throw new Exception("Mk.C save identity changed.");
        if (
            !CalledMethods(Method("Patches.FishermansStaysailRegistrationPatch", "Postfix"))
                .Any(m =>
                    m.DeclaringType == Type("MkC.FishermansStaysailMkC") && m.Name == "Register"
                )
        )
            throw new Exception("Mk.C is absent from prefab registration.");
        foreach (string patch in new[] { "Shipyard", "ShipyardFallback" })
        {
            string method = patch == "Shipyard" ? "Postfix" : "Prefix";
            if (
                !CalledMethods(Method("Patches.FishermansStaysail" + patch + "Patch", method))
                    .Any(m =>
                        m.DeclaringType == Type("MkC.FishermansStaysailMkC")
                        && m.Name == "AddToShipyard"
                    )
            )
                throw new Exception("Mk.C is absent from shipyard insertion: " + patch);
        }
        if (
            Type("MkC.FishermansStaysailMkCShape").BaseType != Type("FishermansStaysailShape")
            || !CalledMethods(Method("MkC.FishermansStaysailMkCShape", "CreateAnimation"))
                .Any(m =>
                    m.DeclaringType.Name == "FishermansStaysailMkCBakedMesh"
                    && m.Name == "CreateAnimation"
                )
        )
            throw new Exception("Mk.C does not use its authored animation in the shared rig.");
        Console.WriteLine(
            "PASS: Mk.C index 403, shipyard registration and shared authored animation rig."
        );
    }
}
