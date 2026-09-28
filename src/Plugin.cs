using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("com.nandbrew.shipyardexpansion")]
    [BepInDependency("pr0skynesis.sailinfo", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.august.moresailwindsails";
        public const string PluginName = "MoreSailwindSails";
        public const string PluginVersion = "0.2.1";

        internal static ManualLogSource Log { get; private set; }
        private ConfigEntry<KeyboardShortcut> captureWinchPosition;

        private void Awake()
        {
            Log = Logger;
            captureWinchPosition = Config.Bind(
                "Diagnostics",
                "CaptureWinchPosition",
                new KeyboardShortcut(KeyCode.F9),
                "Aim at a boat surface and press this key to log its boat-relative position and normal. Set to None to disable."
            );
            var harmony = new Harmony(PluginGuid);
            harmony.PatchAll(typeof(Plugin).Assembly);
            Sails.FishermansStaysail.Patches.FishermansStaysailSailInfoPatch.Install(harmony);
            Logger.LogInfo($"{PluginName} {PluginVersion} loaded!");
        }

        private void LateUpdate()
        {
            if (captureWinchPosition.Value.IsDown())
                Utils.LogFallbackWinchPlacement.Capture();
        }
    }
}
