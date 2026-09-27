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

### Placement and ownership

Boat profiles in `src/BoatRigs/` own placement definitions;
`FishermanWinchControls` owns cloning, binding and reservations shared by both
sail families. Clone controls inactive with owned external handles and fresh
outlines. Reserve by actual donor identity only for active owners with bound
ropes; unbound stay variants must not reserve slots. Release unused reservations
and rebind on donor changes without destroying sail-owned controllers. Move the
parent mount, never the wheel whose local rotation drives input.

`WinchMountDefinition.SourceMast` optionally selects controls from another mast
without changing stay geometry or save IDs; `SourceIndex` selects an explicit
native control row. Resolve mast overrides by authored `orderIndex` through
`GetComponentsInChildren<Mast>(true)`: the live `BoatRefs.masts` array may be
incomplete before native `Mast.Awake`. Never activate donors or populate native
array slots to make lookup succeed. Missing explicit donors must not silently
fall back to a blocked donor. Jong's foremast sheet mappings use this override.

Use measured supports and finite candidates. Native winch datums supply radius
and facing; mast collider axes supply spar direction, but sail-space collider
ends are not physical spar ends.

- **Masts:** candidate spacing is at least **0.35 m**, or twice the padded
  interaction radius plus **2 cm**. Prefer vertical stacks, then **±90°/180°**
  faces, rotating position and facing together. Travel is **−0.7 to +1.4 m**
  from the datum; try the upper endpoint last, without adding a lower endpoint
  near deck level.
- **Surface strips:** use measured finite solid strips, explicit normals and
  donor-specific mesh-base offsets within **1.401 m** of the donor. Include
  strip ends inset by interaction radius. Native fittings and reserved controls
  exclude candidates. Never restore unsupported surface-tangent offsets.
- **Fixed points:** use a measured contact position and normal plus the donor's
  mounting offset. Each produces one candidate independent of donor distance;
  native clearance and reservations still apply. Shroud's forward sheets use
  fixed points on the trim; its aft sheets retain bounded strips.

Exhaustion hides the fitting, retains its controller and retries at one-second
intervals. Do not expand bounds to mask exhaustion. Capacity depends on the
complete native and custom rig: isolated placement checks do not establish
mixed-sail capacity. Keep coordinates and detailed measurements in profiles and
fixtures rather than duplicating them here.

### Shroud belaying pins

**Only Shroud enables belaying-pin reuse.** The optional support lives in the
shared controller, but only Shroud's four halyard mappings provide `PinNames`.
All other boats retain their existing placement and clearance rules.

The mainmast and mizzen racks each offer twelve authored side-pin seats. Custom
halyards clone their usual donor and borrow the selected native coil's live
position and orientation. Native `GPButtonRopeWinch.ShowWinch(false)` disables
its renderer and collider without deactivating the object; an active native coil
is occupied if either component is enabled. Look up named coils through the
inactive hierarchy without activating them. An empty bank hides/retries normally.

Only these named pins may lend unused seats. Allocation uses **0.14 m** clearance
radius per coil to fit the existing pin spacing; actual interaction colliders
are unchanged. Other native fittings retain their usual clearance and protection.
Both custom families share reservations. Check the selected native coil every
refresh: if a native sail needs it, release the seat and try another free pin.
Otherwise keep the reservation stable, including when other pins become free.

### Placement logging

Read `BepInEx/LogOutput.log`; full log paths and installed references are under
[Local investigation](#local-investigation).

| Message                                 | Meaning                                                                               |
| --------------------------------------- | ------------------------------------------------------------------------------------- |
| `Placed authored winch`                 | Initial successful placement, or a Shroud pin reassignment without placement failure. |
| `No free authored winch position`       | All candidates were rejected, or none were available; the hidden control will retry.  |
| `Missing authored winch support`        | The required mast support is unavailable; the hidden control will retry.              |
| `Winch successfully placed after retry` | Placement succeeded after a failure, including missing support.                       |

Messages identify boat/owner instances, stay or mast, requested/resolved donor
mast, role, source instance, boat-local origin and radius. Exhaustion adds
`candidates`, `nativeBlocked` and `reservationBlocked`. Rejection counts are
mutually exclusive, with native obstructions taking precedence; zero candidates
produce zero rejection counts. They do not identify individual blocking fittings.
Success adds the boat-local `position` and zero-based `slot`; borrowed Shroud
pins also report `pin=<name>#<id>`.

Success is logged after positioning, binding and showing the control. Warnings
are limited to once per control instance; repeated failed retries and routine
refreshes/rebindings stay silent. Recovery is reported for each failure episode,
and Shroud pin reassignment logs the new pin. Match boat/owner instances and role
between warnings and recovery, then inspect the control after leaving the
shipyard before treating a warning as a persistent missing fitting.

### Capturing proposed winch positions

Version **0.2.1** includes an on-demand capture tool, **F9** by default. Load the
boat, close menus, aim the centre of the screen at bare mounting structure within
**10 m**, and press the key once. An on-screen notification confirms the numbered
capture. `BepInEx/LogOutput.log` records `Winch position capture #N` with boat
identity, boat-relative surface `position` and `normal`, collider object path/type,
hit-model identity and distance. Record capture numbers and intended roles
(for example forward port/starboard sheets). Captures do not place winches or
modify saves.

Configure the shortcut with the game closed in
`BepInEx/config/com.august.moresailwindsails.cfg`; `None` disables it:

```ini
[Diagnostics]
CaptureWinchPosition = F9
```

The tool checks ordinary world obstructions and separately transforms the camera
ray into each boat's displaced walking model **before** querying its enabled,
non-trigger colliders. Compare hits in visual-world distances and map the chosen
point/normal back to boat coordinates, accounting for rotated/scaled roots.
Nearby terrain still blocks farther boat surfaces. No colliders, transforms or
physics settings are modified.

Captures describe **collision surfaces**, which can differ from visible rail
geometry. Check the reported object and compare against installed meshes or a
screenshot before authoring a position. Winch orientation and mounting offset
still need authoring; a captured surface point is not automatically a winch pivot.

### Asset provenance and measurement fixtures

Installed references are `Sailwind_Data/level24`, SE's `shipyard_expansion.assets`,
`Leopard/leopard` and `ShatteredSeasExpansion/veil piercer`. Include import-parent
transforms when comparing measurements in boat coordinates. Confirm dependencies
against installed assemblies. Fixtures contain numeric measurements only, never
meshes, textures or assemblies.

| Boat       | Permanent support references                                                                                       |
| ---------- | ------------------------------------------------------------------------------------------------------------------ |
| Brig       | `medi medium new/structure_container/trim_006` rail caps; omit bevels/bends and stair opening                      |
| Sanbuq     | `structure/Cube_013` forward/raised aft caps; exclude lower trim                                                   |
| Junk       | `structure/trim_001` caps, `Cube_035` handrails, `Cube_032` transverse reef beam                                   |
| Jong       | `structure/trim_010` forward/middle/aft caps                                                                       |
| Cog        | `structure/trim_001` aft caps                                                                                      |
| Leopard    | `structure_container/decking trim`, `mainfife back`, `mizzenfife`; exclude raised end posts                        |
| Shroud     | `Clipper_Upper_Trim` fixed forward sheet points and aft strips; native mainmast/mizzen side-pin seats for halyards |
| Large dhow | `Cube_001` and `Cube_008` lower/sloped/raised caps and inner aft rail faces                                        |

Fixtures in `tests/GeometryChecks/FishermansStay/` cover mast/support transforms
(`StayMeasurements.txt`), donor frames/radii (`WinchMeasurements.txt`), rail
bounds (`BrigRailMeasurements.txt`, `WinchSurfaceMeasurements.txt`), full native
obstructions (`LargeDhowNativeWinchMeasurements.txt`,
`JongNativeWinchMeasurements.txt`, `ShroudNativeWinchMeasurements.txt`) and Shroud
trim edges for both finishes (`ShroudTrimMeasurements.txt`). Include native/SE
options and neighboring custom controls when checking clearance. Geometry checks
cover supported seating, reservations, exhaustion/retry and pin reuse; assembly
checks cover donor lookup and native visibility contracts. Surface comparisons
allow **2 cm** for slight face warp; neither suite establishes runtime accessibility.

### Related issues

- [#16](https://github.com/sum-rock/MoreSailwindSails/issues/16): Jong foremast staysail port sheet placement.
- [#21](https://github.com/sum-rock/MoreSailwindSails/issues/21): broader winch placement and capacity investigation.

## Local investigation

For placement diagnostics and F9 instructions, see
[Placement logging](#placement-logging) and
[Capturing proposed winch positions](#capturing-proposed-winch-positions).

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
