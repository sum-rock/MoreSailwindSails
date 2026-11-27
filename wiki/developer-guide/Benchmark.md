# Benchmark

The optional `benchmark/` module builds `SailwindBenchmark.dll`, a standalone
BepInEx plugin for measuring whole-game frame timing. It runs with or without
MoreSailwindSails and does not patch gameplay or change graphics settings. This
reference describes benchmark **0.1.2**, JSON schema **1**, alongside MSS
**0.4.1**. The source links below define the behavior.

Use this module to compare native sails without MSS, native sails with MSS, and
custom sails with MSS. Use the [Utils profiler](Profiling) separately when you
need CPU time or allocations attributed to instrumented sail code.

## Install and configure

Close Sailwind, then run from the repository root:

```sh
./mss performance install
```

Use `--game-dir /path/to/Sailwind` for another installation. The command
rebuilds Release and installs only the recorder at
`BepInEx/plugins/SailwindBenchmark/SailwindBenchmark.dll`. `./mss install` and
`./mss uninstall` manage the base MSS plugin independently; uninstalling MSS
leaves the recorder available for a native baseline.

After the recorder has loaded once, edit
`BepInEx/config/com.august.sailwindbenchmark.cfg`:

```ini
[Capture]
Shortcut = F6
Scenario = junk-offshore-route
Variant = native-without-mss
Notes = Native rig; record sail count, area, reef state, weather and camera here.
WarmupSeconds = 15
DurationSeconds = 60
```

| Setting           | Default | Behavior                                                                                              |
| ----------------- | ------- | ----------------------------------------------------------------------------------------------------- |
| `Shortcut`        | `F6`    | Start a capture or cancel the active capture; `None` disables the shortcut                            |
| `Scenario`        | Empty   | Required label shared by comparable runs                                                              |
| `Variant`         | Empty   | Required condition label, such as `native-without-mss`, `native-with-mss`, or `spritsails-fishermans` |
| `Notes`           | Empty   | Manually record conditions the recorder cannot identify                                               |
| `WarmupSeconds`   | `15`    | Unmeasured warmup, from 0 to 600 seconds                                                              |
| `DurationSeconds` | `60`    | Measured wall-clock duration, from 1 to 600 seconds                                                   |

Keep MSS `EnableProfiling = false` for benchmark comparisons. The benchmark
reloads its configuration when starting; labels, durations, and the stop
shortcut are frozen for that capture.

## Capture behavior and files

With a game running and labels configured, press **F6**. The recorder logs the
capture ID, waits through warmup, and then measures until the requested duration
has elapsed. The default is about **75 seconds total**, including 60 seconds of
measurement. The final whole frame interval is retained, so measured time can
exceed the requested duration.

Pauses, loading, focus changes, and configuration changes do not abort a
capture. Long gaps between callbacks remain in the samples, including a pause
that lasts beyond the requested end time; completion occurs when sampling
resumes. Pressing F6 again cancels and exports a partial capture. Disabling or
destroying the recorder also exports partial data. Invalid clocks or GC counters
and a full sample buffer stop recording with an incomplete reason.

Sampling uses preallocated storage for up to 262,144 intervals. File writing and
result logging happen after sampling stops. Each capture produces a pair under
`BepInEx/benchmark-results`:

- `<captureId>.csv`: `frame,seconds,gc0,gc1,gc2`, with a zero-based sample
  index, frame interval in seconds, and collection-count deltas for GC
  generations 0–2.
- `<captureId>.json`: UTC creation time, labels, notes, software/plugin
  versions, hardware and graphics settings, initial scene and timing settings,
  requested durations, completion status/reason, and frame/GC totals.

The interval is measured with a monotonic clock between `LateUpdate` samples,
not a direct CPU, GPU, or display-presentation measurement. GC counts are
process-wide collections, not GC pause durations or MSS-specific allocations.

There is no event stream in version 0.1.2. Settings in metadata describe capture
startup; later state changes are not tracked. For causal investigation,
correlate the capture ID and UTC creation time with available BepInEx and Player
log entries using [log following](MSS-command#log-following). Logs may provide
context but do not guarantee a record of every pause or state transition.

## Compare conditions

Keep the same scenario and recorder version across conditions. A useful pair of
comparisons is:

1. Native sails without MSS versus the same native rig with MSS installed.
2. Native sails with MSS versus custom sails with MSS installed.

Collect at least five runs per condition, alternating conditions where
practical. Keep boat, route or stationary position, camera, weather, graphics,
sail count, and approximate sail area comparable. A sailing route is appropriate
for sailing behavior; a stationary setup is useful for a more controlled scene.
Treat reefed, furled, off-camera, and shipyard setups as separate scenarios.

Copy the selected JSON/CSV pairs into one directory per condition, retaining
matching filenames. Each directory must contain only that scenario and variant:

```sh
./mss performance compare --baseline /path/to/native-without-mss --candidate /path/to/native-with-mss
./mss performance compare --baseline /path/to/native-with-mss --candidate /path/to/custom-with-mss
```

The command checks recorded environment and non-MSS plugin versions across
conditions. Variant and MSS version may differ between conditions, but each
group must have a consistent variant and plugin inventory. It rejects incomplete
captures, inconsistent totals, missing pairs, duplicate capture IDs, and
incompatible environments, including different recorder versions. Boat setup,
weather, and camera matching remain manual. Historical event fields, when
present, are ignored.

## Interpret results

Each run reports mean frame time, FPS, median, p95, p99, maximum, and counts and
percentages above 16.67, 33.33, 50, and 100 ms. The first two thresholds use
exact 1/60 and 1/30 second budgets. FPS is frame count divided by total measured
time; percentiles use nearest rank.

Across runs, the command reports the median and range of each metric and the
candidate's absolute and percentage change. Runs are not pooled. Positive
frame-time changes mean slower performance; positive FPS changes mean faster.
There is no automatic pass/fail budget or statistical significance test.
Differences within repeat variation remain unresolved and do not prove zero mod
overhead. Frame caps and VSync can conceal additional work; the command flags
configured caps but cannot detect external driver limits.

## Source

- [Recorder configuration and lifecycle](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/benchmark/BenchmarkPlugin.cs)
- [Capture clock, boundaries, and buffer](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/benchmark/Core/BenchmarkCapture.cs)
- [Metadata and CSV export](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/benchmark/Core/BenchmarkExport.cs)
- [Independent build](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/benchmark/SailwindBenchmark.csproj)
- [Installation](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/performance/install.py)
  and
  [comparison and compatibility checks](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/performance/cli.py)
