using System;
using System.Diagnostics;
using BepInEx.Configuration;
using UnityEngine;

namespace MoreSailwindSails.Utils.Profiling
{
    // Integrates the reusable collector/session with BepInEx configuration and Unity's frame clock.
    internal static class PerformanceProfile
    {
        private static readonly ProfileCollector collector = new ProfileCollector(
            timestamp: Stopwatch.GetTimestamp,
            allocatedBytes: ProfileAllocationCounter.TryCreate(runtime: typeof(GC))
        );
        private static readonly ProfileSession session = new ProfileSession(
            collector: collector,
            report: Report
        );
        private static ConfigEntry<bool> enabled;
        private static ConfigEntry<KeyboardShortcut> shortcut;
        private static ConfigEntry<string> targets;
        private static bool changed;

        internal static void Configure(ConfigFile config)
        {
            enabled = config.Bind(
                section: "Diagnostics",
                key: "EnableProfiling",
                defaultValue: false,
                description: "Enable optional CPU/frame diagnostics. Captures start OFF; use ToggleProfiling. Does not measure Unity Cloth or GPU execution."
            );
            shortcut = config.Bind(
                section: "Diagnostics",
                key: "ToggleProfiling",
                defaultValue: new KeyboardShortcut(KeyCode.F7),
                description: "Toggle CPU/frame capture on or off. Set to None to disable the shortcut."
            );
            targets = config.Bind(
                section: "Diagnostics",
                key: "ProfileTargets",
                defaultValue: ProfileSelection.DefaultTargets,
                description: "Comma-separated CPU categories: "
                    + ProfileSelection.DefaultTargets
                    + ". All selects every category; empty records frame statistics only. Reports distinguish families across all loaded boats."
            );
            enabled.SettingChanged += Changed;
            shortcut.SettingChanged += Changed;
            targets.SettingChanged += Changed;
            changed = true;
        }

        private static void Changed(object sender, EventArgs args) => changed = true;

        internal static ProfileScope Measure(
            ProfileTarget target,
            ProfileFamily family = ProfileFamily.Unspecified,
            ProfileStage stage = ProfileStage.Total
        ) => collector.Begin(target: target, owner: family, stage: stage);

        internal static void Tick()
        {
            if (enabled == null)
                return;
            if (changed)
            {
                session.Configure(
                    selection: ProfileSelection.Parse(text: targets.Value, warn: Warn)
                );
                changed = false;
                Log(
                    "[PerformanceProfile] Configuration applied; OFF. targets="
                        + ProfileReport.Names(selection: collector.Selection)
                );
                return;
            }
            int previous = session.Mode;
            bool toggle = enabled.Value && shortcut.Value.IsDown();
            if (!toggle && session.Mode == 0)
                return;
            session.Tick(
                enabled: enabled.Value,
                toggle: toggle,
                frame: Time.frameCount,
                seconds: Time.unscaledDeltaTime,
                now: Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency,
                gc: GC.CollectionCount(generation: 0)
            );
            if (previous != session.Mode)
                Log(
                    "[PerformanceProfile] "
                        + (
                            session.Mode == 0
                                ? "OFF."
                                : "NORMAL; recording selected CPU categories and frame statistics."
                        )
                );
        }

        internal static void Stop()
        {
            bool active = session.Mode != 0;
            session.Stop();
            if (active)
                Log("[PerformanceProfile] OFF.");
        }

        private static void Report(ProfileSession value) =>
            ProfileReport.Write(session: value, log: Log);

        private static void Log(string message) => Plugin.Log.LogInfo(data: message);

        private static void Warn(string message) =>
            Plugin.Log.LogWarning(data: "[PerformanceProfile] " + message);
    }
}
