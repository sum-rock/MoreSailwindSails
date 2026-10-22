using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails
{
    [BepInPlugin(PluginGuid, PluginName, PluginRuntimeVersion)]
    [BepInDependency(
        "com.nandbrew.shipyardexpansion",
        BepInDependency.DependencyFlags.SoftDependency
    )]
    [BepInDependency("pr0skynesis.sailinfo", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.august.moresailwindsails";
        public const string PluginName = "MoreSailwindSails";
        public const string PluginVersion = "0.3.0";

        // BepInEx 5 accepts numeric System.Version metadata, not prerelease labels.
        public const string PluginRuntimeVersion = "0.3.0";

        internal static ManualLogSource Log { get; private set; }
        private ConfigEntry<KeyboardShortcut> captureWinchPosition;
        private ConfigEntry<bool> enableWinchMountOverlay;
        private ConfigEntry<KeyboardShortcut> toggleWinchMountOverlay;
        private ConfigEntry<KeyboardShortcut> toggleWinchModelPreview;
        private Utils.WinchMountOverlay winchMountOverlay;

        private void Awake()
        {
            Log = Logger;
            captureWinchPosition = Config.Bind(
                "Diagnostics",
                "CaptureWinchPosition",
                new KeyboardShortcut(KeyCode.F9),
                "Aim at a boat surface and press this key to log its boat-relative position and normal. Set to None to disable."
            );
            enableWinchMountOverlay = Config.Bind(
                section: "Diagnostics",
                key: "EnableWinchMountOverlay",
                defaultValue: false,
                description: "Enable the winch mount diagnostic tool. Use its shortcut to select a boat and toggle wireframes."
            );
            toggleWinchMountOverlay = Config.Bind(
                section: "Diagnostics",
                key: "ToggleWinchMountOverlay",
                defaultValue: new KeyboardShortcut(KeyCode.F8),
                description: "Aim at a boat within 10 metres and press to show its winch locations; press again to hide. Requires EnableWinchMountOverlay. Set to None to disable the shortcut."
            );
            toggleWinchModelPreview = Config.Bind(
                section: "Diagnostics",
                key: "ToggleWinchModelPreview",
                defaultValue: new KeyboardShortcut(
                    mainKey: KeyCode.F8,
                    modifiers: new[] { KeyCode.LeftShift }
                ),
                description: "Toggle winch model previews while the winch mount overlay is open. Purple boxes continue to mark fallbacks. Set to None to disable the shortcut."
            );
            Utils.Profiling.PerformanceProfile.Configure(config: Config);
            var harmony = new Harmony(PluginGuid);
            harmony.PatchAll(typeof(Plugin).Assembly);
            Sails.FishermansStaysail.Patches.FishermansStaysailSailInfoPatch.Install(harmony);
            Compatibility.Patches.SailInfoNamesPatch.Install(harmony: harmony);
            Logger.LogInfo($"{PluginName} {PluginVersion} loaded!");
        }

        private void LateUpdate()
        {
            Utils.Profiling.PerformanceProfile.Tick();
            if (captureWinchPosition.Value.IsDown())
                Utils.LogFallbackWinchPlacement.Capture();
            if (!enableWinchMountOverlay.Value)
            {
                RemoveOverlay();
                return;
            }
            if (!winchMountOverlay)
                winchMountOverlay = gameObject.AddComponent<Utils.WinchMountOverlay>();
            winchMountOverlay.PreviewShortcut = toggleWinchModelPreview.Value.ToString();
            if (toggleWinchModelPreview.Value.IsDown())
                winchMountOverlay.ToggleModels();
            else if (toggleWinchMountOverlay.Value.IsDown())
                winchMountOverlay.Toggle();
        }

        private void OnDisable()
        {
            Utils.Profiling.PerformanceProfile.Stop();
            RemoveOverlay();
        }

        private void RemoveOverlay()
        {
            if (!winchMountOverlay)
                return;
            winchMountOverlay.enabled = false;
            Destroy(winchMountOverlay);
            winchMountOverlay = null;
        }
    }
}
