// Guards donor audio ownership, serialized clone wiring and silent lifecycle paths.
using System;
using System.Linq;
using System.Reflection;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks;

internal static class FlutterAudioChecks
{
    private const BindingFlags All =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    internal static void Run(Assembly assembly)
    {
        Type SailType(string name) =>
            assembly.GetType(name: "MoreSailwindSails.Sails." + name, throwOnError: true);
        var audio = SailType(name: "Audio.SailFlutterAudio");
        var staysail = SailType(name: "FishermansStaysail.FishermansStaysailRig");
        var spritsail = SailType(name: "LooseFootedSpritsail.LooseFootedSpritsailRig");
        var cloth = SailType(name: "LooseFootedSpritsail.LooseFootedSpritsailCloth");
        var prefab = SailType(name: "LooseFootedSpritsail.LooseFootedSpritsailPrefab");
        foreach (
            var pair in new[]
            {
                (audio, "source"),
                (audio, "sail"),
                (staysail, "flutterAudio"),
                (spritsail, "FlutterAudio"),
            }
        )
            Check(
                condition: pair.Item1.GetField(name: pair.Item2, bindingAttr: All)
                    .CustomAttributes.Any(a => a.AttributeType.Name == "SerializeField"),
                message: "Audio references must survive prefab cloning."
            );

        var configure = Instructions(method: Method(type: audio, name: "Configure")).ToArray();
        Check(
            condition: configure.Any(i =>
                i.Operand is FieldInfo f
                && f.Name == "flapAudio"
                && f.DeclaringType.Name == "SailFlapAudio"
            ),
            message: "Reuse the native flap source and its clip/spatial settings."
        );
        Check(
            condition: Calls(type: audio, method: "Configure", target: "set_enabled"),
            message: "Disable native playback before activation."
        );
        Check(
            condition: Calls(
                type: staysail,
                method: "Configure",
                target: "Configure",
                owner: audio
            ),
            message: "Staysail templates must configure shared audio."
        );
        var registration = CalledMethods(method: Method(type: prefab, name: "Register")).ToList();
        int capture = registration.FindIndex(m =>
            m.DeclaringType == audio && m.Name == "Configure"
        );
        int strip = registration.FindIndex(m => m.Name == "StripDonor");
        Check(
            condition: capture >= 0 && strip > capture,
            message: "Capture spritsail audio before removing native playback."
        );
        Check(
            condition: Calls(
                type: staysail,
                method: "UpdateFlutter",
                target: "SetFlutter",
                owner: audio
            ) && Calls(type: cloth, method: "Render", target: "SetFlutter", owner: audio),
            message: "Both visual flutter paths must drive shared audio."
        );
        foreach (
            var pair in new[]
            {
                (audio, "OnDisable"),
                (staysail, "OnDisable"),
                (spritsail, "OnDisable"),
                (staysail, "LateUpdate"),
                (cloth, "Render"),
            }
        )
            Check(
                condition: Calls(
                    type: pair.Item1,
                    method: pair.Item2,
                    target: "Silence",
                    owner: audio
                ),
                message: "Disable and render early-return paths must silence audio."
            );

        var playback = Instructions(method: Method(type: audio, name: "SetFlutter")).ToArray();
        foreach (
            string field in new[]
            {
                "playing",
                "currentShipyard",
                "currentlyLoading",
                "shipyardUpdatingOrder",
                "sunk",
            }
        )
            Check(
                condition: playback.Any(i => i.Operand is FieldInfo f && f.Name == field),
                message: "Missing audio gate: " + field
            );
        Check(
            condition: Calls(type: audio, method: "SetFlutter", target: "get_deltaTime"),
            message: "Pause must silence audio."
        );
        Check(
            condition: !playback.Any(i =>
                i.Operand is MethodInfo m && m.DeclaringType.Name == "WindCloth"
            ),
            message: "Procedural flutter must not depend on native cloth acceleration."
        );
        Check(
            condition: Calls(type: audio, method: "Silence", target: "Stop")
                && Calls(type: audio, method: "SetFlutter", target: "Play"),
            message: "Silent sources must stop and audible sources must restart."
        );
        Console.WriteLine(
            "PASS: Flutter donor audio, clone references, family wiring and lifecycle gates."
        );
    }

    private static MethodInfo Method(Type type, string name) =>
        type.GetMethod(name: name, bindingAttr: All);

    private static bool Calls(Type type, string method, string target, Type owner = null) =>
        CalledMethods(method: Method(type: type, name: method))
            .Any(m => m.Name == target && (owner == null || m.DeclaringType == owner));

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
