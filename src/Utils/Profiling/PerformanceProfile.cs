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
            timestamp: Stopwatch.GetTimestamp
        );
        private static readonly ProfileSession session = new ProfileSession(
            collector: collector,
            report: Report
        );
        private static ConfigEntry<bool> enabled;
        private static ConfigEntry<KeyboardShortcut> shortcut;
        private static ConfigEntry<string> targets;
        private static ConfigEntry<string> bypass;
        private static bool changed;

        internal static void Configure(ConfigFile config)
        {
            // Bind reads saved/orphaned entries. New saved keys override these migration defaults.
            bool oldEnabled = config
                .Bind(
                    section: "Diagnostics",
                    key: "EnableSpritsailMountProfiling",
                    defaultValue: false
                )
                .Value;
            var oldShortcut = config
                .Bind(
                    section: "Diagnostics",
                    key: "ToggleSpritsailMountProfiling",
                    defaultValue: new KeyboardShortcut(KeyCode.F7)
                )
                .Value;
            config.Remove(new ConfigDefinition("Diagnostics", "EnableSpritsailMountProfiling"));
            config.Remove(new ConfigDefinition("Diagnostics", "ToggleSpritsailMountProfiling"));
            enabled = config.Bind(
                section: "Diagnostics",
                key: "EnableProfiling",
                defaultValue: oldEnabled,
                description: "Enable optional CPU/frame diagnostics. Captures start OFF; use ToggleProfiling. Does not measure Unity Cloth or GPU execution."
            );
            shortcut = config.Bind(
                section: "Diagnostics",
                key: "ToggleProfiling",
                defaultValue: oldShortcut,
                description: "Cycle NORMAL, the selected visual BYPASS, and OFF. With ProfileBypass=None, toggle NORMAL/OFF. Set to None to disable the shortcut."
            );
            targets = config.Bind(
                section: "Diagnostics",
                key: "ProfileTargets",
                defaultValue: ProfileSelection.DefaultTargets,
                description: "Comma-separated CPU categories: "
                    + ProfileSelection.DefaultTargets
                    + ". All selects every category; empty records frame statistics only. Reports distinguish families across all loaded boats."
            );
            bypass = config.Bind(
                section: "Diagnostics",
                key: "ProfileBypass",
                defaultValue: "SailMount",
                description: "One visual comparison: None, SailMount, Snotter (including purchase/collar), or SpritsailLiveRopes (custom sheets/lashings). Sail mechanics continue."
            );
            enabled.SettingChanged += Changed;
            shortcut.SettingChanged += Changed;
            targets.SettingChanged += Changed;
            bypass.SettingChanged += Changed;
            changed = true;
        }

        private static void Changed(object sender, EventArgs args) => changed = true;

        internal static ProfileScope Measure(
            ProfileTarget target,
            ProfileFamily family = ProfileFamily.Unspecified,
            ProfileStage stage = ProfileStage.Total
        ) => collector.Begin(target: target, owner: family, stage: stage);

        internal static bool IsBypassed(ProfileBypass target) => session.IsBypassed(target: target);

        internal static void Count(ProfileTarget target, ProfileCounter counter) =>
            collector.Count(target: target, counter: counter);

        internal static void SurfaceQuery(bool hit) => collector.SurfaceQuery(hit: hit);

        internal static void Revision(int reasons)
        {
            if (!collector.Selected(target: ProfileTarget.VisualCache))
                return;
            for (int i = 0; i < 7; i++)
                if ((reasons & (1 << i)) != 0)
                    Count(target: ProfileTarget.VisualCache, counter: ProfileCounter.Initial + i);
        }

        internal static void Consumer(ProfileTarget target, bool rebuild)
        {
            ProfileCounter counter;
            switch (target)
            {
                case ProfileTarget.SailMount:
                    counter = ProfileCounter.MountRebuild;
                    break;
                case ProfileTarget.Sprit:
                    counter = ProfileCounter.SpritRebuild;
                    break;
                case ProfileTarget.Snotter:
                    counter = ProfileCounter.SnotterRebuild;
                    break;
                default:
                    return;
            }
            Count(target: ProfileTarget.VisualCache, counter: counter + (rebuild ? 0 : 1));
        }

        internal static void Tick()
        {
            if (enabled == null)
                return;
            if (changed)
            {
                session.Configure(
                    selection: ProfileSelection.Parse(text: targets.Value, warn: Warn),
                    bypass: ProfileSelection.ParseBypass(text: bypass.Value, warn: Warn)
                );
                changed = false;
                Log(
                    "[PerformanceProfile] Configuration applied; OFF, normal visuals restored. targets="
                        + ProfileReport.Names(selection: collector.Selection)
                        + " bypass="
                        + session.Bypass
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
                            session.Mode == 0 ? "OFF; normal visuals restored."
                            : session.Mode == 1
                                ? "NORMAL; recording selected CPU categories and frame statistics."
                            : "BYPASS " + session.Bypass + "; mechanics unchanged."
                        )
                );
        }

        internal static void Stop()
        {
            bool active = session.Mode != 0;
            session.Stop();
            if (active)
                Log("[PerformanceProfile] OFF; normal visuals restored.");
        }

        private static void Report(ProfileSession value) =>
            ProfileReport.Write(session: value, log: Log);

        private static void Log(string message) => Plugin.Log.LogInfo(data: message);

        private static void Warn(string message) =>
            Plugin.Log.LogWarning(data: "[PerformanceProfile] " + message);
    }
}
