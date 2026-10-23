# Profiling

MoreSailwindSails includes optional CPU and frame diagnostics for investigating
sail performance. Captures cover all loaded boats and distinguish Flying Sail
and Staysail measurements. CPU timings cover the mod's instrumented operations;
Unity Cloth simulation and GPU execution are excluded.

## Configuration and capture

The settings are in `BepInEx/config/com.august.moresailwindsails.cfg`, under
`[Diagnostics]`.

| Setting           | Default                              | Purpose                                                         |
| ----------------- | ------------------------------------ | --------------------------------------------------------------- |
| `EnableProfiling` | `false`                              | Enables the capture shortcut; capture initially remains off.    |
| `ToggleProfiling` | `F7`                                 | Starts or stops capture. Set to `None` to disable the shortcut. |
| `ProfileTargets`  | `Rig,Frame,Shape,Aerodynamics,Ropes` | Selects the CPU categories to record.                           |

For example, configure the following before launching the game:

```ini
[Diagnostics]
EnableProfiling = true
ToggleProfiling = F7
ProfileTargets = Rig,Frame,Shape,Aerodynamics,Ropes
```

Press **F7** to start recording and press it again to stop. Applied changes to
any of these settings stop capture; press the shortcut again to start a new
capture with the updated settings. Disabling profiling or the plugin also stops
capture.

Reports are written to `BepInEx/LogOutput.log` with the prefix
`[PerformanceProfile]`. While recording, a report is emitted approximately every
ten seconds. Stopping capture writes the remaining window if it contains any
recorded frames. The log also records capture transitions as `NORMAL` and `OFF`.

## Selecting CPU categories

`ProfileTargets` accepts comma-separated names, ignores surrounding whitespace
and duplicate entries, and is case-insensitive. Unknown names produce a warning
and are ignored; recognized names still apply.

| Target         | Measured work                                                                        |
| -------------- | ------------------------------------------------------------------------------------ |
| `Rig`          | The sail rig's `LateUpdate` work, including nested measured operations.              |
| `Frame`        | Refreshing the sail's spatial frame and supporting rig transforms.                   |
| `Shape`        | Updating the sail's shape and billow.                                                |
| `Aerodynamics` | Refreshing the sail's aerodynamic frame and wind-center transform.                   |
| `Ropes`        | Instrumented custom rope updates, including Flying Sail support lines and luff ties. |

Use `ProfileTargets = All` to select every CPU category. An empty value
(`ProfileTargets =`) records overall frame statistics and GC interval summaries
without CPU category timings. Overall frame statistics are collected during
every capture; the `Frame` category specifically measures spatial-frame work.

## Reading reports

Each report identifies its `session` and `window`. The summary includes the
recorded frame count, mean and maximum frame time in milliseconds, average FPS,
and `over33ms`, the number of frames longer than one thirtieth of a second.
These describe the game's frame timing across the capture window.

CPU rows identify `family`, `target`, and `stage`, and include call count, calls
per frame, CPU milliseconds per frame, and the longest individual call. Only
selected categories with recorded calls produce rows. Families aggregate across
all loaded boats; they do not identify a particular boat or sail instance.
Nested timings are inclusive, so adding category totals can double-count work.

When the runtime provides an allocation counter, CPU rows also contain
`allocatedBytes` and `maxCallAllocatedBytes` for the current thread. These are
inclusive measurements too. If the counter is unavailable, the report says
`allocations=unavailable` and omits those fields.

`gc0Global` counts generation-zero collections observed during the window,
across the process. Interval summaries group elapsed time between profiler ticks
into `with-GC` and `without-GC`, with counts, mean and maximum duration, and
counts above 50 and 100 milliseconds. These correlate slow intervals with
collections; they do not measure GC pause duration or attribute a collection to
a sail. Up to eight of the longest intervals above 50 milliseconds are listed
with their capture-relative end time and collection-count change. Intervals
crossing reporting or capture boundaries are excluded and counted in
`excludedIntervals`.

## Source

This reference describes plugin version **0.3.0** at revision
[`3a2062d`](https://github.com/sum-rock/MoreSailwindSails/tree/3a2062d3436d8067c923d5fe00b1c025c8b477f5).

- [Configuration and shortcuts](https://github.com/sum-rock/MoreSailwindSails/blob/3a2062d3436d8067c923d5fe00b1c025c8b477f5/src/Utils/Profiling/PerformanceProfile.cs)
- [Category selection](https://github.com/sum-rock/MoreSailwindSails/blob/3a2062d3436d8067c923d5fe00b1c025c8b477f5/src/Utils/Profiling/ProfileSelection.cs)
- [Capture windows and transitions](https://github.com/sum-rock/MoreSailwindSails/blob/3a2062d3436d8067c923d5fe00b1c025c8b477f5/src/Utils/Profiling/ProfileSession.cs)
- [CPU and allocation collection](https://github.com/sum-rock/MoreSailwindSails/blob/3a2062d3436d8067c923d5fe00b1c025c8b477f5/src/Utils/Profiling/ProfileCollector.cs)
- [Report fields](https://github.com/sum-rock/MoreSailwindSails/blob/3a2062d3436d8067c923d5fe00b1c025c8b477f5/src/Utils/Profiling/ProfileReport.cs)
- [GC interval sampling](https://github.com/sum-rock/MoreSailwindSails/blob/3a2062d3436d8067c923d5fe00b1c025c8b477f5/src/Utils/Profiling/ProfileIntervals.cs)
- [Flying Sail measurement scopes](https://github.com/sum-rock/MoreSailwindSails/blob/3a2062d3436d8067c923d5fe00b1c025c8b477f5/src/Sails/FishermansFlyingSail/FishermansFlyingSailRig.cs)
  and
  [Staysail measurement scopes](https://github.com/sum-rock/MoreSailwindSails/blob/3a2062d3436d8067c923d5fe00b1c025c8b477f5/src/Sails/FishermansStaysail/FishermansStaysailRig.cs)
