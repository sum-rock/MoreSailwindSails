using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SailwindBenchmark
{
    // Optional, independent observer: never patches or changes gameplay, saves, or graphics settings.
    [BepInPlugin("com.august.sailwindbenchmark", "Sailwind Benchmark", Version)]
    [DefaultExecutionOrder(32000)]
    public sealed class BenchmarkPlugin : BaseUnityPlugin
    {
        public const string Version = "0.1.2";
        private ConfigEntry<KeyboardShortcut> shortcut;
        private ConfigEntry<string> scenario,
            variant,
            notes;
        private ConfigEntry<double> warmup,
            duration;
        private BenchmarkCapture capture;
        private BenchmarkMetadata metadata;
        private KeyboardShortcut captureShortcut;

        private static double Now => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

        private void Awake()
        {
            shortcut = Config.Bind(
                section: "Capture",
                key: "Shortcut",
                defaultValue: new KeyboardShortcut(mainKey: KeyCode.F6),
                description: "Reload configuration and start capture, or cancel the current capture. None disables the shortcut."
            );
            scenario = Config.Bind(
                section: "Capture",
                key: "Scenario",
                defaultValue: "",
                description: "Required matching scenario label across comparisons (boat, location, camera and test conditions)."
            );
            variant = Config.Bind(
                section: "Capture",
                key: "Variant",
                defaultValue: "",
                description: "Required condition label, e.g. native-without-mss, native-with-mss, or spritsails."
            );
            notes = Config.Bind(
                section: "Capture",
                key: "Notes",
                defaultValue: "",
                description: "Sail count, cuts, approximate area, reef state, weather and unavoidable differences. Keep MSS profiling OFF for primary captures."
            );
            warmup = Config.Bind(
                section: "Capture",
                key: "WarmupSeconds",
                defaultValue: 15.0,
                configDescription: new ConfigDescription(
                    "Warm-up before measurement.",
                    new AcceptableValueRange<double>(0, 600)
                )
            );
            duration = Config.Bind(
                section: "Capture",
                key: "DurationSeconds",
                defaultValue: 60.0,
                configDescription: new ConfigDescription(
                    "Wall-clock measurement duration, including pauses, background time and loading. Includes the last complete frame interval.",
                    new AcceptableValueRange<double>(1, 600)
                )
            );
            Logger.LogInfo(
                "Sailwind Benchmark ready. Set Scenario and Variant, then press F6. Results: BepInEx/benchmark-results. Primary runs should have MSS profiling OFF."
            );
        }

        private void LateUpdate()
        {
            if (capture != null && capture.Active)
            {
                if (captureShortcut.IsDown())
                    Cancel(reason: "user-cancelled");
                else
                {
                    double now = Now;
                    capture.Tick(
                        now: now,
                        frame: Time.frameCount,
                        collections0: GC.CollectionCount(generation: 0),
                        collections1: GC.CollectionCount(generation: 1),
                        collections2: GC.CollectionCount(generation: 2)
                    );
                    if (!capture.Active)
                        Export();
                }
                return;
            }
            if (shortcut.Value.IsDown())
            {
                try
                {
                    // Configuration is frozen for this capture, including its stop shortcut.
                    Config.Reload();
                    StartCapture();
                }
                catch (Exception error)
                {
                    Logger.LogError("Could not start benchmark: " + error);
                }
            }
        }

        private void StartCapture()
        {
            if (
                !GameState.playing
                || string.IsNullOrWhiteSpace(scenario.Value)
                || string.IsNullOrWhiteSpace(variant.Value)
                || double.IsNaN(warmup.Value)
                || double.IsNaN(duration.Value)
                || double.IsInfinity(warmup.Value)
                || double.IsInfinity(duration.Value)
            )
            {
                Logger.LogWarning(
                    "Capture requires a running game and nonempty Scenario/Variant labels with valid durations."
                );
                return;
            }
            capture = new BenchmarkCapture();
            var scene = SceneManager.GetActiveScene();
            captureShortcut = shortcut.Value;
            var plugins = Chainloader
                .PluginInfos.Values.Select(info => info.Metadata.GUID + "@" + info.Metadata.Version)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            metadata = new BenchmarkMetadata
            {
                captureId = Guid.NewGuid().ToString("N"),
                createdUtc = DateTime.UtcNow.ToString("O"),
                scenario = scenario.Value.Trim(),
                variant = variant.Value.Trim(),
                notes = notes.Value,
                recorderVersion = Version,
                gameVersion = Application.version,
                unityVersion = Application.unityVersion,
                mssVersion = Chainloader.PluginInfos.TryGetValue(
                    "com.august.moresailwindsails",
                    out var mss
                )
                    ? mss.Metadata.Version.ToString()
                    : "absent",
                plugins = plugins,
                cpu = SystemInfo.processorType,
                gpu = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceVersion,
                operatingSystem = SystemInfo.operatingSystem,
                width = Screen.width,
                height = Screen.height,
                fullscreen = Screen.fullScreen,
                quality = QualitySettings.GetQualityLevel(),
                vSyncCount = QualitySettings.vSyncCount,
                targetFrameRate = Application.targetFrameRate,
                scene = scene.name,
                shipyard = GameState.currentShipyard,
                timeScale = Time.timeScale,
                fixedDeltaTime = Time.fixedDeltaTime,
                warmupSeconds = warmup.Value,
                durationSeconds = duration.Value,
            };
            Logger.LogInfo(
                "Capture "
                    + metadata.captureId
                    + " warming up; no further output until capture stops."
            );
            double now = Now;
            capture.Start(
                now: now,
                warmupSeconds: metadata.warmupSeconds,
                durationSeconds: metadata.durationSeconds
            );
        }

        private void Cancel(string reason)
        {
            if (capture == null || !capture.Active)
                return;
            capture.Cancel(reason: reason);
            Export();
        }

        private void Export()
        {
            try
            {
                string path = BenchmarkExport.Write(
                    directory: Path.Combine(Paths.BepInExRootPath, "benchmark-results"),
                    metadata: metadata,
                    capture: capture,
                    serialize: value => JsonUtility.ToJson(obj: value, prettyPrint: true)
                );
                Logger.LogInfo(
                    "Capture "
                        + (metadata.complete ? "complete" : "incomplete: " + metadata.reason)
                        + "; frames="
                        + metadata.frames
                        + "; results="
                        + path
                );
                if (metadata.frames > 0)
                    Logger.LogInfo(
                        FormattableString.Invariant(
                            $"Whole-game frame intervals: meanMs={metadata.elapsedSeconds * 1000 / metadata.frames:F3} fps={metadata.frames / metadata.elapsedSeconds:F3}"
                        )
                    );
            }
            catch (Exception error)
            {
                Logger.LogError("Could not export benchmark: " + error);
            }
        }

        private void OnDisable() => Cancel(reason: "recorder-disabled");

        private void OnDestroy() => Cancel(reason: "recorder-destroyed");
    }
}
