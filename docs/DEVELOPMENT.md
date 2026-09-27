# MoreSailwindSails development

See [README.md](../README.md) for features and player instructions and
[AGENTS.md](../AGENTS.md) for agent workflow. This guide owns technical reference,
verification procedures and current validation status; resolved history is in Git.

## Build and automated checks

The pinned Nix flake supports **x86_64 Linux** with .NET 8; the plugin targets
`netstandard2.0`. Install Sailwind, BepInEx 5 and Shipyard Expansion locally.
The build references their installed assemblies, including HarmonyX and Unity;
no Unity editor or separate asset bundle is required.

From the repository root, restore a fresh checkout:

```sh
nix develop
dotnet tool restore
dotnet restore
```

Initial Nix/NuGet restores need network access. Keep `flake.lock` and
`.config/dotnet-tools.json` pinned. Optional `pre-commit install` inside the
development shell enables the formatting hook, which invokes Nix on later commits.

After restoring, run from the repository root:

```sh
nix develop -c bash -c 'dotnet csharpier check . && dotnet build -c Release --no-restore && dotnet run --project tests/GeometryChecks -c Release --no-restore && dotnet run --project tests/AssemblyChecks -c Release --no-restore'
git diff --check
```

Use `dotnet csharpier format .` inside the development shell to fix formatting;
`check` does not rewrite files. Output:
`src/bin/Release/netstandard2.0/MoreSailwindSails.dll`.

The default game directory is `~/.local/share/Steam/steamapps/common/Sailwind`.
For another installation, pass `-p:SailwindDir="/path/to/Sailwind"` to the plugin
and test builds. AssemblyChecks also needs the path as a runtime argument:

```sh
nix develop -c dotnet build -c Release -p:SailwindDir="/path/to/Sailwind"
nix develop -c dotnet run --project tests/AssemblyChecks -c Release -p:SailwindDir="/path/to/Sailwind" -- "/path/to/Sailwind"
```

- **GeometryChecks** executes pure calculations: geometry/skinning, coupled
  tension, wind frames, hoist/reef poses, collision/travel bounds, text wrapping,
  authored profiles, winch placement and older shipyard snapshots. All three
  staysail cuts share a fixed-head, sheet, reef and mirrored-skin behavior matrix.
- **AssemblyChecks** inspects installed signatures, Harmony wiring/order, native
  list restoration, appearance, registration/save capacity and lifecycle structure.
  Use direct IL decoding; Harmony native patch stubs failed in this environment.
  Structural checks do not execute control exception recovery or Unity prefabs.

Neither suite simulates Unity Cloth, rendering, audio initialization, hinge
physics or the live shipyard. Passing checks do not establish in-game behavior.

## Release and manual installation

For **0.2.1**, keep `Plugin.PluginVersion`, the project `<Version>`, README and
startup example consistent. Preserve GUID `com.august.moresailwindsails`, assembly
`MoreSailwindSails.dll`, display name/namespace `MoreSailwindSails` and prefab
IDs **400** (Flying Sail), **401/402/403** (Mk.A/B/C). Distribute only the plugin DLL.

The following scripts are **manual maintainer workflows**. Agents must not
execute files from `scripts/` or use that directory as their working directory.
Builds/checks do not install the plugin, change saves or publish a release.

- With Sailwind closed, `./scripts/install-local.sh` copies the built Release
  DLL into the default game's plugin directory. An optional game-directory
  argument selects another installation. It does not build the DLL.
- After merging release changes to `master`, `./scripts/tag-release.sh` requires
  a clean checkout, switches to `master`, fetches/fast-forwards and requires it
  to match `origin/master`. It checks matching versions and tag availability,
  shows the tag and commit, then asks `Are you sure? [y/N]` before tagging.
  Only `y` or `yes` (case-insensitive) continues; empty input, other responses
  or EOF cancel with exit status 1. Confirmation happens after the branch
  switch and fetch/fast-forward. Once confirmed, it creates/pushes annotated
  `v<version>`, rebuilds the Release DLL, then creates
  a GitHub release with that DLL and generated notes. It requires Nix and an
  authenticated `gh`. A later build/publish failure can leave the pushed tag.

After manual installation, confirm `MoreSailwindSails 0.2.1 loaded!` in
`BepInEx/LogOutput.log`. Flying Sail registration uses donor **110**, prefab
**400** and **825** vertices; staysails register **401/402/403**. Check the
installed DLL separately from build output when diagnosing.

## Source organization

The solution includes the plugin and both check projects. Feature namespaces
follow their directories under `MoreSailwindSails`; Harmony patches live in each
feature's `Patches/` directory and `.Patches` namespace.

| Location                          | Responsibility                                                                                                                      |
| --------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `src/Plugin.cs`                   | Identity, dependencies, Harmony discovery and optional SailInfo patch                                                               |
| `src/Sails/FishermansFlyingSail/` | Mast-mounted sail registration, rig, geometry, tension, billow and aerodynamics                                                     |
| `src/Sails/FishermansStaysail/`   | Family prefab builder, rig, fixed head, edge fitting and reefing; `MkA/`, `MkB/`, `MkC/` supply cuts and identities                 |
| `src/Stays/FishermansStay/`       | Independent mounts, registration, previews, controls and save compatibility                                                         |
| `src/BoatRigs/`                   | One class per boat owns supports, stays, mast ancestry and winch mounts; `Definitions.cs` owns shared types, validation and catalog |
| `src/Controls/`                   | Shared reservations/cloning and `WinchPlacementGeometry.cs` placement math                                                          |

Add future sail families under their own `src/Sails/<Family>/` directory.
Keep existing families independently editable; [shared-helper extraction is
deferred](CLEANUP.md). Runtime mesh/object labels use the family or mark prefix;
preserve donor hierarchy names.

Both check suites mirror feature directories and namespaces under
`MoreSailwindSails.Tests.<Suite>.<Feature>`. Staysail behavior is parameterized by
`BehaviorCases` at family level; cut/identity checks belong under `MkA/`, `MkB/`
and `MkC/`. Root `Program.cs` files arrange execution. Shared Harmony/IL helpers
live in `tests/AssemblyChecks/Shared/`; measurement fixtures stay with features.
Geometry projects link source files directly and assembly checks resolve full
type names: update both when moving or renaming code.

## Runtime safeguards

- **Live Cloth topology stays fixed.** Shape by moving existing bones; no mesh
  swaps, bind-pose changes, Cloth rebuilding or solver resets on tacks. The
  mirrored-mesh experiment passed geometry checks but detached/reset in game.
  Initialization and existing furl/render refreshes serve separate purposes.
- Construct templates under inactive parents with fresh Cloth for new topology.
  Preserve the donor Animator as SE's scaling reference and the hierarchy expected
  by `SailShadowCol`. Template-owned and instance-owned meshes have distinct
  lifetimes. Never mirror collider transforms using negative scale.
- Rope endpoints are independent leaves: native `RopeEffect` calls `LookAt`
  on them and must not rotate skin bones. Preserve coupled edge fitting and finite fallbacks.
- Keep corner control, appearance and propulsion separate. Aerodynamic frames
  follow posed corners; force patches are scoped to custom sails. Do not fake
  SailInfo values or alter vanilla forces. Retain donor wind-cloth response
  unless evidence warrants a change.
- Filter custom sails only during native control binding and restore the full
  list in a finalizer. Capacity, collision, overlap and saves must see all sails;
  custom controls cannot depend on native mast sail order.
- Resolve authored, connected **active** mast sections/guides; registration and
  previews can precede activation. Protect occupied stays and support chains.
- Clamp travel to **±40°** after `JibAngleMaster.Update` adds sway, preserving
  tighter collision, prefab, sweep and restored limits without snapping transforms.
- Each family retains its iterative order-text guard before NANDFixes. HarmonyX
  runs later prefixes even after `false`: append wrapped lines to the native list
  and consume input so later prefixes cannot recurse on it.
- New sails use native white palette **11** and SE plain texture **0**. Preserve
  saved colors, recoloring and the hidden color-reference renderer; scope plain
  texture/selector/material guards to custom sails. Never change donor/shared assets.
  The SE compatibility patch seeds the native plain texture before catalog discovery:
  numeric saved selections and SE's fixed option lists require plain at zero.
  Never reorder a catalog after texture indices have been assigned.

### Sailwind 0.39 audio

Donor 110's wind-center object carries `SailFlapAudio`, which searches only its
parent and grandparent for `Sail`. Both families place it beneath their pivot
frame during inactive construction, preserving its initial world pose. Posed
aerodynamic refreshes continue updating its center/orientation. Retain native
clips, unmute delay and snap initialization; no native audio methods are patched.

## Flying Sail

- Fits a physical mast under **Other**, with active aft support, native mast save
  slots and normal vertical-space/overlap rules. Hoists from deck; partial hoists
  use a procedural renderer, full deployment uses Cloth, and striking hides
  cloth and parks ropes. Only the four corners are pinned.
- Base width is donor 110's `installHeight / 3`; installation height comes from
  the new luff. The isosceles trapezoid has luff `2 × width`, head rising **20°**
  aft, foot falling **20°** aft and aft edge about `2.728 × width`. Derive edge
  budgets, area, bounds and shadow samples from this cut. New selections use
  `SailScaler.SetScaleAbs(1f, 1f)` after SE initialization; preserve saved sizes.
- Retain **85%** upper-corner sheeting response with coupled foot/leech fitting.
  The attempted 60% response caused in-game creases and was reverted.
- Pivot around the offset luff. Two fixed **0.4572 m (18-inch)** ties hold its
  corners beyond the mast capsule surface; they do not scale. Mast ends follow
  hoisting height; ties hide when struck, unsupported, loading or disabled.
- The luff arches mastward on both tacks, capped at **0.2286 m (9 inches)**.
  Shrinking reduces it with the smallest scale; enlargement retains the cap.
  Scale-dependent Cloth coefficients reserve at least one-quarter of the mast
  gap beyond arch plus travel. Update coefficients during fitting/scaling;
  tack changes move bones only.
- The transverse rest section is a circular arc of depth `0.20 × width`, retained
  from head to foot. Twelve shaping intervals use the existing **24 × 32** mesh.
  Interior travel stays near local camber with edge freedom. Bending stiffness
  is **0.15**, stretching **0.99**, native WindCloth damping **0.08–0.45**.
- Collision uses thin neutral trapezoid strips swept around the offset luff,
  separate from billow bounds. Retain the first strip; include mast radius/tie
  gap in span checks and compare each head to its own active guide. Restore the
  aligned neutral rotation after sweeping; keep native obstruction rules.

### Rope visuals and knots

Head/foot visuals join mast ties, luff corners and aft corners. The shared head
span continues through the aft guide before branching to sheet controls; lower
branches run directly from clew to controls. Draw shared spans once, easing the
head/foot extensions into straight ties. External chords use downward parabolic
sag of **0.5–2.5%** from native slack and retain native rope material/width.

A marker-scoped `RopeEffect.LateUpdate` postfix suppresses original line and
optional `ClothRope` visuals without changing native input/tension. Draw routes
after posing; striking hides extensions/ties and parks upper/lower branches at
aft/fore guides. Loading, missing supports and disabling hide custom routes.

Each corner shares one native-style knot between meeting ropes, with either rope
setting. Bake the unreadable jib-sheet mesh from a private inactive renderer
copy and isolate the connected knot (installed asset: **96** knot vertices,
**192** triangles; exclude **66** tube vertices). Template ownership covers the
generated geometry; discard temporary objects without running donor scripts.
Missing/incompatible donors log once and omit knots without disabling the sail.
Knot leaves stay outside fabric scaling and follow corner pose/rope direction.

## Staysails

All marks fit registered Fisherman's Stays. Register after SE and before All
Sails caches its inventory. Each mark's `FishermansStaysailShape` supplies geometry
and owned-asset prefixes for template and instance creation; do not reintroduce
separate geometry/prefix arguments. The family builder owns donor **110** and
template slope **20°**; first binding creates the owned mesh for the actual stay
slope before enabling Cloth. Mesh and bind poses then stay fixed.

| Cut  | Geometry in the forward-mast frame                                                   |
| ---- | ------------------------------------------------------------------------------------ |
| Mk.A | Original 110° foot cut, sloping downward aft                                         |
| Mk.B | Same head/width, 90° foot cut; deck-parallel on upright masts                        |
| Mk.C | Same head/width, 50% longer luff (`1.5 × width`), foot rising `width × tan(40°)` aft |

The deployed luff stays on the forward mast and neutral head aligns with the
stay. The native stay slot owns the save; installation coordinate measures
downward from the stay's forward endpoint. Retain **15 cm** head/aft-mast
clearances, spar-length/span fitting and native collision checks. Collision
strips use lowest head/highest foot with **5 cm** edge margins; disable strips
too shallow for margins plus **1 cm** height or clipped below **1 mm** width.
New selections start at **50% width/height** through SE after initialization;
uniform scaling preserves the cut and existing saves retain their dimensions.

### Fixed head and reefing

- Upper aft target is **14° × clamped currentUnroll** leeward: **14°/7°/0°** at
  full/half/zero deployment. Rotate the neutral head about the fore-mast axis,
  preserving height/span and independence from lower sheets and wind strength.
- Select tacks from apparent wind projected on `Cross(mastAxis, neutralAftDirection)`.
  A **±0.6 m/s** deadband retains the previous side; indeterminate initialization
  defaults positive. Smooth transitions. The old 85% sheet-following policy and
  inward trim are removed; `FishermansStaysailEdgeFit` retains support bow and
  coupled lower-corner fitting without moving the fixed head.
- Rounded billow uses a spanwise sine with `(1-v)*(1+0.75*v)` vertical taper and
  a **12%-width** head peak. Interior travel is capped near **60%** of local camber
  plus foot/leech allowances and clew taper. Opposite-tack skin tests did not
  reproduce Sanbuq's reported asymmetry; excess travel was a suspected contributor,
  not a confirmed Cloth-level cause.
- Reef **upward** using the brig jib's native `reef` clip/controller and
  `furled__sail_cloth_jib` mesh from `sharedassets15.assets`. Sample an inactive,
  stripped animation hierarchy at `1 - currentUnroll`, with no live donor scripts
  or colliders. Preserve animation binding paths and donor Animator; disable
  the original reef component so it cannot compete with custom rendering.
- The lower corner gathers toward the reefed head angle. Below **4%** deployment,
  show the recolorable native bundle fitted between posed head endpoints,
  returning to neutral at full furl. Above it use the sampled panel pose, then
  initialized Cloth at full deployment. Do not substitute Flying Sail hoisting.
- Clone the aft-base reef winch for the native reef controller. Route its halyard
  through active aft guides (with **5 cm** separation) and a leaf below the top-aft
  bone. Keep independent lower sheets; no fore halyard or decorative upper sheets.
  Resolve active aft ancestry without falling back to the fore mast; protect both
  support chains. Controls reconstruct from the native save slot without migration.
- Optional SailInfo integration (inspected against **1.2.1**) reports actual
  mast-relative sheet angle without clamping the label. Keep scope limited to
  custom staysails and preserve settings/force readouts.

## Boat profiles and stays

Profiles author physical mast IDs, endpoints, active guides, prerequisites,
exclusions, ancestry and permanent mount IDs. Each boat's `Definition` owns its
data; resolve `Sections`, `Base` and `WinchMount` through that profile.

| Boat                       | Part groups | Stay variants | Forward-masthead fallbacks |
| -------------------------- | ----------: | ------------: | -------------------------: |
| Brig                       |           2 |            24 |                          4 |
| Junk                       |           2 |             9 |                          2 |
| Jong                       |           5 |             9 |                          0 |
| Sanbuq                     |           2 |            26 |                          5 |
| Cog                        |           1 |             3 |                          0 |
| Leopard                    |           2 |            18 |                          4 |
| Shroud                     |           2 |             8 |                          2 |
| Large dhow (Sailwind 0.39) |           2 |            14 |                          8 |

The **111** variants prefer **70°** between the aft spar's downward axis and stay.
If that intersects above the connected forward spar, use its physical masthead
and a steeper stay. Preserve physical fore/aft ordering, exclude higher aft
topmasts from lower variants and store endpoints rather than infer them at runtime.

Append groups/options after SE initializes customization; never reorder save
slots. Reserve mount IDs **128–255** and expand capacity to **256** without
shrinking larger arrays. Missing old-save/cancellation entries restore **None**.
Validate profiles and occupied IDs before construction; roll back a boat's new
stays on failure. Protect occupied stays/supports during invalid previews and
restore preview state in a finalizer. Normalize stay and walking geometry
independently to the same endpoints; donor bounds may differ from `mastHeight`.

### Large dhow

`LargeDhow.cs` uses installed native `BOAT dhow large (30)`. Its **24** native mast
combinations cover two foremast choices, two main positions with optional matching
topmasts, and three mizzen choices. Ten Flying Sail supports and fourteen stays
cover adjacent pairs: eight fore/main masthead fallbacks and six main/mizzen
70° variants. Topmast IDs **3/5** require bases **2/4** and retain lower-mainmast
halyard guides on the overlapping section. Main/mizzen stays attach to the lower
mainmast whether its topmast is fitted or absent. Twenty sheet and nine reef
mappings include raked spars, with topmast controls mounted on their bases.

Fore/main stays meet the **rendered** foremast end ring at local z **2.223295**.
The capsule tip at z **2.29** is 6.7 cm higher and produced the reported floating
attachment. All eight fore/main variants use the corrected, slightly steeper
slope; aft guide heights and save IDs stay unchanged.

Foremast/mainmast reef donors **0/1/2/4** use the upper port control at native
array index **2**. Lower-row donors exhaust their mounting space against the
complete native rig. This explicit `WinchMountDefinition.SourceIndex` changes
the donor datum without enlarging travel or reducing clearance radii. Other
profiles retain first-usable selection (`-1`); missing optional control roles
return empty before looking up a mounting definition.

## Winch placement

`FishermanWinchControls` clones inactive controls with owned external handles and
fresh outlines. All three control paths use boat-owned reservations keyed by
actual donor identity. Only active owners with bound ropes reserve slots; unbound
stay variants do not. Release unused reservations and rebind on donor changes
without destroying sail-owned controllers. Reposition the parent mount, never
the wheel whose local rotation drives input.

Eight profiles contain **180** donor/role mappings: **46** mast and **134** bounded
surface mappings. Native winch datums supply attachment radius/facing; mast
collider axes identify spar direction, but sail-space collider ends are not
physical spar ends. Deck-facing coils use measured supporting surfaces.

- Spacing uses interaction radii with a **0.35 m** minimum and **2 cm** beyond
  padded radii. Mast candidates prefer vertical stacks, then **±90°/180°** faces,
  rotating position and facing together. Travel is **−0.7 to +1.4 m** from the
  datum; try the upper endpoint after regular positions, without adding a lower
  endpoint near deck level.
- Surface candidates follow measured finite solid strips, explicit normals and
  donor-specific mesh-base offsets within **1.401 m** of the donor. Include safe
  strip ends inset by interaction radius. Native fittings and reserved controls
  exclude candidates. Never restore unsupported surface-tangent offsets.
- Exhaustion hides the control, logs once and retries while retaining its
  controller. The warning identifies boat/owner instances, stay or mast,
  requested and resolved donor mast, role, source instance, boat-local origin,
  radius, candidate count and native/reservation rejection counts. Counts are
  mutually exclusive: native obstructions take precedence. Repeated one-second
  retries remain silent; no support candidates produces zero rejection counts.
  After positioning, binding and showing a control, an info-level message reports
  `Placed authored winch` on initial success or `Winch successfully placed after retry`
  after a placement failure (including missing support). Both include the warning's
  boat/owner/donor context plus the selected boat-local `position` and zero-based
  candidate `slot`. Routine refreshes/rebindings stay silent. Recovery is reported
  for each failure episode; warnings remain limited to once per control instance.
  Do not expand bounds. With large-dhow mesh interaction colliders,
  checks establish at least **one** extra reef control per donor; multiple custom
  sails can exhaust space. Older isolated mast checks require three, bounded
  surface checks at least two. These counts do not establish mixed-sail capacity.

### Jong foremast sheets (#16 and #21)

The reported persistent missing port winch in **0.2.1** is reproduced by including
Shipyard Expansion's additional forward sheet fittings in the obstruction model.
The old mast **10** donor and three-strip profile leave no port candidate, while
starboard retains a position. A suspended mount leaves its controller/line alive,
consistent with the user's screenshot of the port line ending at the rail.
This establishes the measured Jong case of [#21](https://github.com/sum-rock/MoreSailwindSails/issues/21)'s
exhaustion mechanism alongside [#16](https://github.com/sum-rock/MoreSailwindSails/issues/16);
older unlabelled warnings from both sides cannot all be attributed to that case.

Both mast **10** sheet mappings now use `WinchMountDefinition.SourceMast = 7`,
the native `front_stay_lower` controls, and only the measured lower longitudinal
`trim_010` caps. The clear slots are approximately `(±3.114, 4.145, 2.527)` in boat
coordinates. They are within **1.401 m** of mast 7's donors but outside that band
around the former donors. Changing rail endpoints alone would not admit them.
Stay geometry still comes from mast **10**, and mount **128** and its save slot
are unchanged. `SourceMast = -1` preserves every other mapping; missing explicit
donors yield no usable source rather than reverting to the blocked donor.

Resolve explicit donors through the owning boat's `GetComponentsInChildren<Mast>(true)`
hierarchy and authored `orderIndex`, including inactive native options. Native
`Mast.Awake` populates `BoatRefs.masts`; that live array is not a complete donor
catalog during early stay construction. The first override implementation used
`boat.masts[7]` and failed Jong registration with incomplete controls, rolling
back all five groups. Do not activate donors or write native array slots to make
lookup succeed. Incomplete-control errors now include the custom mount, missing
roles, and sheet/halyard mapping IDs.

The complete conservative native/SE obstruction fixture establishes **one**
extra sheet per side, including the logged neighboring Flying Sail between masts
**2–3**, which shares reef donor **2** with the foremast staysail. More custom
sails sharing the foremast sheet space can still exhaust it; hidden controls
retain their controllers and retry when reservations are released.

### Asset provenance and measurement fixtures

Installed sources are `Sailwind_Data/level24`, SE's `shipyard_expansion.assets`,
`Leopard/leopard` and `ShatteredSeasExpansion/veil piercer`. SE imports below the
boat model; compare transformed coordinates in that common frame. Confirm import
parents/dependencies against installed assemblies. Fixtures contain numeric
measurements only, never meshes, textures or assemblies.

| Boat       | Permanent surface references                                                                  |
| ---------- | --------------------------------------------------------------------------------------------- |
| Brig       | `medi medium new/structure_container/trim_006` rail caps; omit bevels/bends and stair opening |
| Sanbuq     | `structure/Cube_013` forward/raised aft caps; exclude lower trim                              |
| Junk       | `structure/trim_001` caps, `Cube_035` handrails, `Cube_032` transverse reef beam              |
| Jong       | `structure/trim_010` forward/middle/aft caps                                                  |
| Cog        | `structure/trim_001` aft caps                                                                 |
| Leopard    | `structure_container/decking trim`, `mainfife back`, `mizzenfife`; exclude raised end posts   |
| Shroud     | `Clipper_Upper_Trim`, `Halyard_Points/Cube.004` and `Cube.005`; exclude rounded ends          |
| Large dhow | `Cube_001` and `Cube_008` lower/sloped/raised caps and inner aft rail faces                   |

Fixtures in `tests/GeometryChecks/FishermansStay/` are `StayMeasurements.txt`
(mast transforms, spar extents and guides), `WinchMeasurements.txt` (donor frames,
roles and radii), `BrigRailMeasurements.txt` and `WinchSurfaceMeasurements.txt`
(independent solid face bounds). `LargeDhowNativeWinchMeasurements.txt` includes
all **85** native fittings, not just the first entry in each donor array. It
reproduces both blocked lower-mainmast cases and checks the corrected donors
against every native row, including mutually exclusive variants.
`JongNativeWinchMeasurements.txt` contains **151** fittings: **60** from native
`level24` and **91** from SE's `SE_parts_jong` prefab. Positions include the
outer boat-model import transform used by installed `JongPatches.Patch`; the
fixture records default activation but checks conservatively include all rows.
The Jong checks reconstruct the original three-strip profile from independent
face measurements, reproduce port-only exhaustion, and cover corrected symmetry,
donor travel, mixed-sail fitting order, release/retry and diagnostic counts.
Checks cover attachment/angles, fixed IDs,
ancestry/cycle rejection, reservations, strip ends, obstructions and exhaustion.
Surface comparisons allow **2 cm** for slight face warp. Brig/Sanbuq/Junk
screenshots confirmed unsupported tangent-based placement; measured strips
replace it. Keep numeric details in profiles/fixtures instead of duplicating tables.

## Runtime validation

For the Jong correction in **0.2.1**, the user's pre-fix observation and screenshot
confirm a persistently missing port control with a visible starboard control.
The first donor-override build then failed in game: existing stays did not load
and none were available at the shipyard. The matching installed DLL logged
`The stay donor has no complete independent controls` and rejected the entire
Jong profile. Geometry checks missed the startup dependency on native mast
registration, and the old structural check incorrectly required live-array access.
The lookup now searches inactive hierarchy objects; updated assembly checks verify
that path, authored-ID matching, and absence of activation/registration side effects.
The user subsequently reported that the correction appears to work on Jong.
The latest game log confirms successful registration of **5 groups and 9 variants**.
This is a positive observation for the tested setup, not coverage of every sail,
stay, save/load or shipyard combination. Neither suite executes Unity rendering,
interaction or control lifecycle recovery.

The same session's contextual warnings identify cases to investigate, not
confirmed persistent failures. The user reports the Junk's current build appears
fine and suspects the Jong conflicts came from trying additional sails in the
shipyard:

- **Junk** (`BOAT junk medium (80)`), Mk.B on mount **136**, mapping/donor **52**,
  starboard sheet: **5** candidates, all blocked by native fittings and none by
  custom reservations. Mount **136** is `main mast 2 / mizzen mast 3`. This warning
  occurs during initial loading and again during shipyard rebuilding. Later,
  after leaving the shipyard, the rebuilt sail also warns for its port sheet:
  **10** candidates, **7** native-blocked and **3** reservation-blocked.
- **Jong**, Mk.B on mount **128**, mapping **10** / donor **7**, both sheets:
  **5** candidates, **4** blocked by native fittings and **1** by a reservation.
  Several different owner instances warn during the session. These identify
  contention for the remaining slot; the logs do not identify the reserving owner
  or report recovery in that build, so persistent failure versus transient shipyard contention
  is unconfirmed. Preserve the user's positive observation alongside these warnings.

That session's build retried silently at one-second intervals after failure;
its logs identify the affected sail and donor, but neither the individual blocking
fittings nor successful recovery. The updated **0.2.1** build adds initial-placement
and recovery messages. A subsequent game session confirms both messages, including
successful retries on Junk and Brig. Shroud Mk.B mount **128** (`Foremast 1 /
Mainmast 1`) fails all three controls; mount **132** (`Mainmast 1 / Mizzen 1`)
fails its halyard while both sheets place. All Shroud rejections are native-blocked,
repeat after leaving the shipyard, and have no logged recovery in that session.
These remain concrete unresolved #21 cases; the capture tool below gathers
proposed mounting points without changing the placement policy.
Match boat/owner instances and role between warnings and recovery messages.
Check both sheets of the identified sail after leaving the shipyard before
treating a warning as a currently missing control. After installation, verify
initial success logs once, failed retries remain quiet, and recovery logs once
when space or missing support becomes available.

After manual installation, start on **Brig** to check existing sheet/reef controls.
Then confirm **Jong** logs successful registration of **5 groups and 9 variants**,
restores stays from an intact previous save and offers stays in the shipyard.
Test a foremast staysail alone and with a Flying Sail between
masts **2–3**. Confirm both lower-rail sheet winches are visible, reachable and
independently usable; repeat after save/load, shipyard cancellation, and fitting
or removing neighboring sails. Genuine exhaustion must preserve controllers,
emit one contextual warning per control instance, and recover after space frees.
Do not mark the broader #21 capacity cases resolved without their own evidence.

## Local investigation

### Capturing proposed winch positions

Version **0.2.1** includes an on-demand capture key, **F9** by default. After
installing the DLL, load the boat, close menus, aim the centre of the screen at
the desired mounting surface within **10 m**, and press **F9** once. An on-screen
notification confirms the numbered capture; `BepInEx/LogOutput.log` records
`Winch position capture #N` with boat identity, boat-relative surface `position`
and `normal`, full collider object path/type, hit-model identity and distance.
Record the capture numbers for each intended role (for example forward port and
forward starboard sheet). Captures do not place winches or modify saves.

The key is configured in `BepInEx/config/com.august.moresailwindsails.cfg`:

```ini
[Diagnostics]
CaptureWinchPosition = F9
```

With the game closed, change the shortcut as needed; `None` disables it. The
tool uses the main camera's centre ray. It checks ordinary world obstructions
using the native pointer layer mask, and separately transforms the ray from each
boat's visual model into its displaced walking model before querying that model's
enabled, non-trigger colliders. Hits compete by distance in visual-world units,
so nearby terrain still blocks a farther boat surface. Walking hits are mapped
back into the same `BoatRefs` coordinates used by winch definitions. Normals use
the inverse-transpose transform, and ray distance limits account for scaled roots.
No colliders, transforms or physics settings are modified.

Captured points describe **collision surfaces**, which may differ from visible
rail geometry. Aim at bare structure, check the reported object, and retain a
screenshot when needed for comparison with installed meshes. A capture is a
surface reference; winch orientation and mounting offset still need authoring.
The first capture build failed in-game on Shroud, reporting terrain or no hit.
It transformed walking-model hits only after raycasting, leaving the ray in the
visual world where most boat surfaces have no solid collider. The corrected path
transforms the ray before collision queries. Geometry checks reproduce the miss
against a displaced plane and verify rotated/scaled rays, returned points and
world-distance limits. In-game verification of the correction is pending:
check both Shroud sheet positions, a miss and
a non-boat hit, menu suppression, a rebound key, and repeated captures of one
spot as the boat moves. Holding the key should produce only one capture.

### Installed references and logs

Game directory: `/home/august/.local/share/Steam/steamapps/common/Sailwind`.
Inspect `Sailwind_Data/Managed/Assembly-CSharp.dll`, Unity assemblies,
`BepInEx/core/` (including HarmonyX), and installed dependencies under
`BepInEx/plugins/`: ShipyardExpansion/SE_Bridge, NANDFixes, AllSailsAllShipyards
and SailInfo. Keep debugging dependencies out of the plugin and asset inspection
read-only. The installed mod DLL is distinct from build output.

Read both logs; Unity warnings may be absent from BepInEx output:

```text
/home/august/.local/share/Steam/steamapps/common/Sailwind/BepInEx/LogOutput.log
/home/august/.local/share/Steam/steamapps/compatdata/1764530/pfx/drive_c/users/steamuser/AppData/LocalLow/Raw Lion Workshop/Sailwind/Player.log
```

Capture logs before restarting a freeze. Distinguish other mods' exceptions and
suspected causes from confirmed evidence. Prefer `rg`/`rg --files`; inspect
screenshots with the local image viewer. Temporary tools may exist at
`/tmp/fisherman-inspect/` (ILSpy helper/cache) and
`/tmp/fisherman-assets-env/bin/python` (UnityPy). Inspect their projects,
dependency paths and cached results before use; recreate if absent.
