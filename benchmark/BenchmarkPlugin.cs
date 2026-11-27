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
        public const string Version = "0.1.1";
        private ConfigEntry<KeyboardShortcut> shortcut;
        private ConfigEntry<string> scenario,
            variant,
            notes;
        private ConfigEntry<double> warmup,
            duration;
        private BenchmarkCapture capture;
        private BenchmarkMetadata metadata;
        private KeyboardShortcut captureShortcut;
        private bool applicationPaused;

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
            Config.SettingChanged += SettingsChanged;
            SceneManager.activeSceneChanged += SceneChanged;
            SceneManager.sceneLoaded += SceneLoaded;
            SceneManager.sceneUnloaded += SceneUnloaded;
            Logger.LogInfo(
                "Sailwind Benchmark ready. Set Scenario and Variant, then press F6. Results: BepInEx/benchmark-results. Primary runs should have MSS profiling OFF."
            );
        }

        private void SettingsChanged(object sender, SettingChangedEventArgs args) =>
            capture?.Observe(
                now: Now,
                kind: BenchmarkEventKind.ConfigurationChanged,
                value: 1,
                occurrence: true
            );

        private void SceneChanged(Scene previous, Scene next) =>
            capture?.Observe(
                now: Now,
                kind: BenchmarkEventKind.ActiveScene,
                value: next.buildIndex
            );

        private void SceneLoaded(Scene scene, LoadSceneMode mode) =>
            capture?.Observe(
                now: Now,
                kind: BenchmarkEventKind.SceneLoaded,
                value: scene.buildIndex,
                occurrence: true
            );

        private void SceneUnloaded(Scene scene) =>
            capture?.Observe(
                now: Now,
                kind: BenchmarkEventKind.SceneUnloaded,
                value: scene.buildIndex,
                occurrence: true
            );

        private void LateUpdate()
        {
            if (capture != null && capture.Active)
            {
                if (captureShortcut.IsDown())
                    Cancel(reason: "user-cancelled");
                else
                {
                    double now = Now;
                    ObserveState(now: now);
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

        private void ObserveState(double now)
        {
            Observe(
                now: now,
                kind: BenchmarkEventKind.Focused,
                value: Application.isFocused ? 1 : 0
            );
            Observe(
                now: now,
                kind: BenchmarkEventKind.ApplicationPaused,
                value: applicationPaused ? 1 : 0
            );
            Observe(now: now, kind: BenchmarkEventKind.Playing, value: GameState.playing ? 1 : 0);
            Observe(
                now: now,
                kind: BenchmarkEventKind.CurrentlyLoading,
                value: GameState.currentlyLoading ? 1 : 0
            );
            Observe(
                now: now,
                kind: BenchmarkEventKind.LoadingBoatLocalItems,
                value: GameState.loadingBoatLocalItems ? 1 : 0
            );
            Observe(
                now: now,
                kind: BenchmarkEventKind.LoadingScenes,
                value: GameState.loadingScenes
            );
            Observe(
                now: now,
                kind: BenchmarkEventKind.Recovering,
                value: GameState.recovering ? 1 : 0
            );
            Observe(now: now, kind: BenchmarkEventKind.Sleeping, value: GameState.sleeping ? 1 : 0);
            Observe(
                now: now,
                kind: BenchmarkEventKind.CursorMenu,
                value: GameState.inCursorMenu ? 1 : 0
            );
            Observe(
                now: now,
                kind: BenchmarkEventKind.Shipyard,
                value: GameState.currentShipyard ? 1 : 0
            );
            Observe(
                now: now,
                kind: BenchmarkEventKind.BoatInstance,
                value: GameState.currentBoat ? GameState.currentBoat.GetInstanceID() : 0
            );
            Observe(
                now: now,
                kind: BenchmarkEventKind.ActiveScene,
                value: SceneManager.GetActiveScene().buildIndex
            );
            Observe(now: now, kind: BenchmarkEventKind.Width, value: Screen.width);
            Observe(now: now, kind: BenchmarkEventKind.Height, value: Screen.height);
            Observe(
                now: now,
                kind: BenchmarkEventKind.Fullscreen,
                value: Screen.fullScreen ? 1 : 0
            );
            Observe(
                now: now,
                kind: BenchmarkEventKind.Quality,
                value: QualitySettings.GetQualityLevel()
            );
            Observe(
                now: now,
                kind: BenchmarkEventKind.VSyncCount,
                value: QualitySettings.vSyncCount
            );
            Observe(
                now: now,
                kind: BenchmarkEventKind.TargetFrameRate,
                value: Application.targetFrameRate
            );
            Observe(now: now, kind: BenchmarkEventKind.TimeScale, value: Time.timeScale);
            Observe(now: now, kind: BenchmarkEventKind.FixedDeltaTime, value: Time.fixedDeltaTime);
        }

        private void Observe(double now, BenchmarkEventKind kind, double value) =>
            capture.Observe(now: now, kind: kind, value: value);

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
            ObserveState(now: now);
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

        private void OnApplicationFocus(bool focused)
        {
            capture?.Observe(now: Now, kind: BenchmarkEventKind.Focused, value: focused ? 1 : 0);
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            capture?.Observe(
                now: Now,
                kind: BenchmarkEventKind.ApplicationPaused,
                value: paused ? 1 : 0
            );
        }

        private void OnDisable() => Cancel(reason: "recorder-disabled");

        private void OnDestroy()
        {
            Cancel(reason: "recorder-destroyed");
            Config.SettingChanged -= SettingsChanged;
            SceneManager.activeSceneChanged -= SceneChanged;
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneUnloaded -= SceneUnloaded;
        }
    }
}
