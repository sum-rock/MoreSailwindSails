# Spritsail hitching investigation

Status: **unresolved**, updated **2026-10-10** for **0.3.0-dev**. This records
evidence and remaining leads; it is not a diagnosis of the allocating code or
actual garbage-collection pause duration. General profiler instructions live in
[DEVELOPMENT.md](DEVELOPMENT.md#performance-profiling).

## Symptom and current conclusion

The user reports a small hitch every one or two seconds with spritsails. It
becomes clearer with two spritsails, including outside the shipyard. On Junk,
replacing the spritsails with gaff sails while retaining the Fisherman's
Staysail makes sailing visibly smoother. Updating the computer and outdated mods
did not resolve it. Earlier investigation also used Sanbuq with two spritsails;
the quantitative comparisons below use Junk.

Average FPS and periodic hitching are separate problems. Sheet-flex, SailMount
fitting and mast-intersection optimizations reduced measured custom CPU cost,
but hitching persisted. Most measured long intervals coincide with GC. That
supports a GC connection without establishing whether allocation rate, retained
objects, another subsystem or a combination causes the pauses.

What the experiments establish:

- Hitching occurs while both spritsails reuse their mount, sprit and snotter
  caches without rebuilding. Repeated fitting or mesh uploads from those
  rebuilds are not required for these hitches.
- Reverting native binding while retaining later performance improvements did
  not remove the hitch pattern.
- Skipping hidden native rope visual updates improved average FPS by roughly
  4–5% in the latest ABAB capture, but did not reduce long-interval counts.
- Neither triangle count nor Blender smooth shading has been established as the
  cause. The user's suspicion that onset accompanied the snotter remains an
  observation, not a confirmed regression boundary.

## Measurements

### Gaff versus spritsail comparisons

The initial matched one-minute samples averaged **50.2 FPS with gaffs** and
**45.9 with spritsails**. GC0 frequency increased from **0.90 to 1.23
collections/second**. Spritsail windows had worst frames of **107–124 ms**,
without mount, sprit or snotter rebuilds.

Both spritsail rigs were then changed to reuse scaled-corner arrays and avoid
LINQ enumeration when checking support parts. A subsequent matched minute still
showed the problem:

| Arrangement | Average FPS | GC0/second | Tick intervals >100 ms | Of those, with GC |
| ----------- | ----------: | ---------: | ---------------------: | ----------------: |
| Gaffs       |        51.7 |       1.00 |                      0 |                 0 |
| Spritsails  |        46.7 |       1.20 |                     24 |                21 |

These allocation fixes were useful but insufficient. Per-frame allocation bytes
were unavailable on the installed Mono runtime, so the remaining allocator was
not identified by these captures.

### Native-binding rollback

“Native binding” means assigning spritsail controls to native mast winch slots
and resolving their native lower/upper guides and halyard routing. It does not
mean all native physics or rendering.

A diagnostic based on `4c5b1ed` restored pre-`932960e` control assignment,
profile-based supports and halyard routing, retaining subsequent rendering,
caching and allocation improvements. It also restored earlier mounting
restrictions. The test nevertheless ran with **one loose-footed spritsail, one
boomed spritsail and one Fisherman's Staysail** on Junk.

Its first minute averaged **45.9 FPS**, with **72 GC0 collections** and **27
tick intervals over 100 ms**, **25 containing GC**. No mount, sprit or snotter
rebuilds or capture exceptions were recorded. Startup identification and the
installed DLL checksum confirmed the rollback. Native binding is therefore no
longer the primary lead; this result does not exonerate every native subsystem.

### Hidden native rope updates: ABAB

The next diagnostic restored current native binding and selected
`ProfileBypass = HiddenNativeRopes`. The same three sail families were active.
Compare the first six complete ten-second windows in each phase; the longer tail
of the second bypass period is excluded to keep durations comparable.

| Phase    | Session / windows | Frames | Average FPS | GC0 collections | Intervals >100 ms | With GC / without GC |
| -------- | ----------------- | -----: | ----------: | --------------: | ----------------: | -------------------- |
| Normal 1 | 1 / 1–6           |  2,838 |       47.24 |              66 |                27 | 27 / 0               |
| Bypass 1 | 1 / 8–13          |  2,954 |       49.17 |              61 |                31 | 25 / 6               |
| Normal 2 | 2 / 1–6           |  2,867 |       47.71 |              60 |                31 | 26 / 5               |
| Bypass 2 | 2 / 8–13          |  3,004 |       50.00 |              62 |                30 | 26 / 4               |

Each selection spans approximately 60.1 seconds. FPS is weighted by frame count
and elapsed frame time, not an unweighted average of window FPS.

Counters confirm **five loose-footed and four boomed native visual updates per
frame** in NORMAL, replaced by the same number of skips in BYPASS. There were no
skips in NORMAL or updates in BYPASS. Both spritsails reused all three visual
caches every frame, with no rebuilds in these windows. Capture logs contained no
exceptions. A startup All Sails in All Shipyards prefab-list error and HarmonyX
warnings about `Climate.WeatherService` were present; neither is established as
the periodic cause.

**Result:** a repeatable small FPS gain in this capture, with no improvement in
the recurring long intervals. Hidden native visual updates are not a sufficient
explanation or fix for the hitching.

## What the rope bypass does and does not test

Installed `RopeEffect.LateUpdate` updates attachments, applies limits and
updates tension before generating visuals. Inspection found that `DisplayRope`
builds a Bezier point list and allocates a positions array on each call. With 3D
ropes enabled, native code also creates or activates a `ClothRope` and calls
`UpdateRope`. Existing mod postfixes then hide the replaced native line and rope
object.

The diagnostic in
[HiddenNativeRopeVisualsPatch.cs](../src/Sails/Spritsail/Patches/HiddenNativeRopeVisualsPatch.cs)
skips the visual block only for ropes bearing the existing spritsail suppression
markers. It preserves attachment, limit and tension calculations, native
settings cleanup and hiding postfixes. The visible boomed sheet, custom ropes
and other sail families are unaffected.

The bypass **does not prevent startup creation or remove existing hidden rope
objects**. `RopeEffect.Start` can still instantiate them. It therefore tests
visual updates, not retained-object or startup costs. `ClothRope.UpdateRope`
moves rope bones; its name alone is not evidence of Unity Cloth solver work.

Release, both check suites and formatting passed for this diagnostic; installed
IL checks validated mechanics/cleanup boundaries and all **115 Harmony
targets**. Those structural checks do not execute Unity lifecycle behavior.
Runtime skip counters establish that the bypass actually ran in this capture.

## Historical builds and evidence identity

| Revision  | Experiment / observation                                                                            | Interpretation                                                                   |
| --------- | --------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| `0c56cd2` | Tuesday's completed snotter build: no clear hitch reported, only one spritsail fitted, about 34 FPS | Not a matched good baseline; low FPS and sail-count limits weaken the comparison |
| `932960e` | Wednesday's native-binding revision: subtle with one spritsail, clear with two outside shipyard     | Reproduces the symptom, but does not establish which change introduced it        |
| `2810dd2` | Wednesday's later build also stuttered                                                              | Friday's SailMount/performance changes are not required to reproduce it          |
| `4c5b1ed` | Base for current-versus-binding-rollback diagnostics                                                | Rollback still hitched with two spritsails                                       |
| `1fd40bc` | Records the hidden-native-rope diagnostic and its checks                                            | Latest ABAB results above; source was built before this commit                   |

Tubular ropes (`adbeafe`) follow `932960e`; the later SailMount change
(`f85d2a9`) follows those historical tests' source revisions. Neither addition
is required to reproduce the reported historical symptom. There is still **no
reliable good/bad commit pair under matched two-sail conditions**.

DLL SHA-256 identities retained from the experiments:

| Artifact                           | SHA-256                                                            |
| ---------------------------------- | ------------------------------------------------------------------ |
| Unchanged `4c5b1ed` control build  | `fc1e7b4440e433e8a5c3cf4145c69feb72c65a6a2c38754336285bb15d3663ce` |
| Native-binding rollback diagnostic | `e0cae2b8e07b39f65d12c082a4bbe9c27f0416ce6b6ec67ae45646bca7738ce6` |
| Hidden-native-rope diagnostic      | `b63282d1ee8404c9cbe4bbdacd967ee6bdef6e6213125a84ec1291182e9cd956` |

The last identity matched the installed DLL when the ABAB capture was reviewed.
The reviewed BepInEx log SHA-256 was
`244efa296fb230c5d3e890c5b07c583e577eef81d15026ce30d0abe01a1cbce0`. Live log
locations are in
[Installed references and logs](DEVELOPMENT.md#installed-references-and-logs);
they may be overwritten on restart. Earlier build manifests, diagnostic patches
and capture copies under `/tmp/mss-native-binding-rollback-t14khzs9` and
`/tmp/mss-hidden-native-071fb862` were no longer available when this document
was written. Do not depend on those paths to reproduce a build.

## Remaining leads and useful next experiments

These are untested directions, not established causes or approved changes:

- **Hidden rope lifetime:** prevent creation of only replaced native rope
  visuals, including the startup path, while preserving rope mechanics and
  visible ropes. Inspect initialization and teardown dependencies first. Use
  fresh launches for comparisons; the existing F7 bypass does not isolate object
  lifetime.
- **Periodic control discovery:**
  [NativeWinchSeats.cs](../src/Controls/NativeWinchSeats.cs) refreshes its
  inventory on a one-second schedule while its coordinator owns controls.
  Refresh scans the hierarchy, copies arrays and rebuilds seat aliases with
  allocations and pairwise position comparisons. Instrument its actual timing
  and calls before changing it. It predates native binding, and the retained
  Fisherman's Staysail can keep it active in both gaff and spritsail tests;
  cadence alone is not evidence of causation.
- **Remaining allocations and live objects:** the collector currently cannot
  attribute bytes on this Mono runtime. Investigate methods outside custom rig
  timing and differences in retained native objects as well as allocation
  frequency. A GC-correlated interval cannot distinguish these explanations.

For another comparison, keep the same Junk, both spritsail types, staysail,
camera, graphics settings and other loaded boats. Separate steady sailing from
sheeting/reefing; exclude loading, shipyard transitions and pauses. Use about 60
seconds per ABAB phase for hitch counts and verify the selected bypass's
counters. Record the source revision, installed DLL hash, configuration, sail
count and the user's perceived hitching; archive both logs before restarting. An
FPS gain alone is not a successful hitch fix.

The profiler covers **all loaded boats**, its CPU timings are inclusive and
exclude Unity Cloth/GPU completion, and its GC counts are process-wide. Long
intervals containing GC are not measured GC pauses. Reporting/transition
intervals are excluded from interval correlation, and warmed sampling avoids
per-sample allocation, but reporting and instrumentation still add overhead. Use
these limits when interpreting small differences or designing the next isolated
bypass.
