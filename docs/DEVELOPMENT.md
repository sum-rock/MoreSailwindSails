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
| `src/BoatRigs/`                   | One class per boat owns supports, stays, mast ancestry, sheet categories and fallbacks |
| `src/Controls/`                   | Native seat discovery, separate sheet/halyard resolvers, atomic reservations and owned control cloning                                                          |
| `src/Utils/`                      | Optional, read-only in-game diagnostic tools and their geometry helpers |

Shared boat-rig definitions and the catalog live in `src/BoatRigs/Definitions/`,
with one type per file. They retain the `MoreSailwindSails.BoatRigs` namespace.

In boat profile files, use named arguments for every supplied domain constructor
argument, including explicit null fallbacks and boolean flags. Keep conventional
`Vector3(x, y, z)` coordinates positional. Preserve authored values and ordering
when changing argument style.

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
data; resolve `Sections`, `Base` and `SheetCategory` through that profile.

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
mainmast whether its topmast is fitted or absent. Native control arrays include
raked spars and topmast seats physically mounted on their lower sections.

Fore/main stays meet the **rendered** foremast end ring at local z **2.223295**.
The capsule tip at z **2.29** is 6.7 cm higher and produced the reported floating
attachment. All eight fore/main variants use the corrected, slightly steeper
slope; aft guide heights and save IDs stay unchanged.

## Winch placement

The **0.2.1** native winch placement redesign is **accepted as valid and complete**
on Brig, Junk, Jong, Sanbuq, Cog, Leopard, Shroud and large dhow. It applies to
Flying Sails, all three staysail cuts and native sails fitted to Fisherman's
Stays. This section owns the current behavior, including the completed follow-up
architecture cleanup. The implementation plan and cleanup checklist have been
retired; [runtime validation](#runtime-validation) records the checks and remaining
in-game uncertainty.

### Placement and ownership

Family rigging remains separate; placement, bootstrap-template selection, cloning
and allocation are shared. Boat profiles provide authored data, native inventory
discovers controls,
separate sheet/halyard resolvers select seats, and the boat-owned reservation
ledger and clone coordinator manage allocation and control lifetime.

Each boat profile authors physical mast categories separately from ordered native
sheet-source rigs. Sources include physical variants, their topmasts and verified
associated native stays. Lookup uses IDs and authored ancestry, never display-name
parsing. Definitions defensively copy collection inputs into read-only wrappers;
the catalog is cached. Categories store ordered source IDs directly, with pairing
only at matching native array indices. Native and installed SE shipyard group
metadata distinguishes variants
from simultaneous mast positions: Jong has separate `MainmastA`/`MainmastB`
categories; Cog's raked foremast belongs to its separate foremast group.

The forward physical mast selects the sheet category (`References.Fore` for
custom stays, `Pair.Fore` for Flying Sails). `SheetingWinchPlacementResolver`
searches complete native left/right pairs in authored source order and native
array order. It discovers inactive source rigs through the boat hierarchy without
relying on the startup `BoatRefs.masts` array. Missing or unmatched array entries
are skipped with a diagnostic. Centre sheets never become pair candidates.

Native poses preserve both sides' position, orientation and asymmetry. Shared
references and coincident seats are deduplicated; the first supported whole pair
or halyard wins over an earlier unsupported equivalent. All aliases still
participate in occupancy checks. A bound native rope blocks borrowing even while
hidden, as does
an active renderer or collider. Mounting ancestors and requested physical supports
must be active. Native poses and manually authored fallback positions are trusted:
there are no radius, proximity or geometric clearance checks. Authors must choose
fallback positions that avoid native fittings and other fallback positions.

`WinchReservations` atomically claims both sheet seats by identity and aliases.
Halyards share the same boat-owned ledger. No failed sheet claim can consume
only one side. The coordinator registers both sheet bindings before
allocation, excludes all owned clones from native discovery and keeps unused
startup variants unclaimed. Native-control suppression prevents custom-stay
mount controls and sail-owned controls from competing for the same sail.

Boat profiles may define `HalyardWinchGroup(mast, sources)` entries through
`halyardGroups`. Each group maps one exact requested mast ID to a complete ordered
list of native rigs whose reef arrays supply seats. Lookup does not inherit groups
across mast variants or ancestry, and there is no category-wide or proximity search.
An unconfigured mast uses only its own reef array. All authored groups search the
requested mast first, then verified associated native stay sources. For example,
**Cog mast 57 (mizzen mast 2)** uses sources **[57, 58]**: its own seats first, then
**58 (midstay 2-2)**, whose `winch_reef_midstay2` is mounted beside the mizzen's own
reef winch. The source stay need not be installed, but the
requested physical mast and the selected winch's mounting support must be active.
Existing native occupancy, aliases, reservations and stable-placement rules apply.
Startup, allocation and retained-placement validation share this source mapping;
source-array changes and native reclaim still invalidate a borrowed seat.

The installed support audit is recorded in
`tests/GeometryChecks/Controls/HalyardMounts.txt`; native identities and parent-local
poses remain in `FishermansStay/NativeWinchSeats.txt` under the same test project.
Groups are authored separately for each exact requested section, including native
stay controls on lower supports used by matching topmasts. They do not borrow the
ordinary reef arrays of other physical mast variants or topmasts. Shared stay-seat
references are intentional and use the existing identity/alias reservations.

| Boat | Halyard groups |
| --- | ---: |
| Brig | 14 |
| Junk | 9 |
| Jong | 7 |
| Sanbuq | 13 |
| Cog | 1 |
| Leopard | 9 |
| Shroud | 6 |
| Large dhow | 9 |

Unlisted masts retain their original lookup, including Jong's raked foremast and
unsupported bermuda variants. Cog retains the user-tested group without additional
expansion. The other seven profiles now cover their audited stay reef seats.

Startup uses a complete usable pair from the forward category's ordered native
sources, then its valid fallback templates. All three ownership paths use this
policy, independently of geometry donors. Template selection ignores occupancy
and does not reserve seats; halyard templates follow the requested mast's authored
group order, or use that mast alone when no group is configured.
A missing template reports its category or requested mast and preserves the
existing registration rollback. Native startup still receives initialized control
arrays: installed `Mast.UpdateControllerAttachments` indexes them directly and
calls `GPButtonRopeWinch.AttachToController`.

The selected placement later supplies its exact clone templates. Clones initialize
inactive
with owned handles and fresh outlines; native objects and bindings remain untouched.
Template changes prepare both replacement sheet clones before retiring either
old clone, retain sail-owned controllers and update custom-mount winch arrays.
Placement moves the parent mount and preserves wheel-local input rotation.

Validate current source-array slots, mounting support, native occupancy, aliases
and poses before constructing alternatives. A valid placement retains its ledger
entries and clones; parent mounting poses and visibility still update. Native
reclaim, lost support or a changed boat-relative pose invalidates the entire pair.
Keep a valid
current pair stable, including a fallback when native seats later free. Exhaustion
hides both sheets, preserves controllers and retries every **one second**; it does
not roll back stays or remove saved sails. Removal and inactive owners release
claims. All placement state is transient; GUID, prefab/stay IDs, save ordering and
saved geometry are unchanged.

### Controller lifetime during cleanup

Final `OwnedWinch.Dispose()` releases group claims and destroys owned winches
without reparenting their controllers, including during sibling suspension.
Boat, stay and sail destruction all use this path, so cleanup does not depend
on their `OnDestroy` callback order. Controllers still beneath a destroyed mount
are removed with that hierarchy; controllers already moved elsewhere are left alone.

Live reconciliation and its rollback use `Retire()`: preserve sibling controllers,
detach the retiring control explicitly, then dispose its winch. Explicit detachment
also protects a retiring control whose group slot already holds its replacement.
Template adoption detaches before destroying the old clone and rebinding. Ordinary
support loss, inactive owners and seat exhaustion continue preserving controllers
while hiding controls for later reuse. A missing owner triggers final disposal;
a broken winch with a surviving owner uses retirement.

The inspected pre-fix `Player-prev.log` on 2026-09-28 contained **66** rope-controller
reparenting warnings across seven boats, clustered after `Saved game. (compressed)`
at the end of the session. These are consistent with teardown; the native warnings
did not include managed call stacks. Separate `SE_Bridge.OnDisable` exceptions
are outside this fix.

Assembly checks cover the preservation policy, retirement/adoption ordering,
reconciliation cleanup and destruction entrypoints. CSharpier check, the Release
build (zero warnings/errors), GeometryChecks, AssemblyChecks and `git diff --check`
passed for this change. The suites do not execute Unity destruction or exception
recovery. **In-game validation remains pending:** start
on Brig, then Junk and Shroud; exercise both custom sail families and native sails
on custom stays. Check exit/unload for absent reparenting warnings, removal for
released seats, rig/template changes for working controls, and seat exhaustion
followed by recovery. Existing cloth validation status is unchanged.

### Inventory lifetime and inspection counters

The coordinator owns one native inventory, mast lookup and seat/alias lookup per
boat. Its empty `LateUpdate` returns without discovery. Groups already released
skip repeated suspension. Alternative searches run only on first allocation,
invalidation or the existing one-second exhaustion retry.

Discovery includes inactive hierarchy objects and does not rely on
`BoatRefs.masts`. The installed-assembly inspection established these hooks:

- `Mast.Awake` registers masts; `OnEnable` and `OnDisable` update winch visibility.
- `BoatCustomParts.RefreshParts` and `RefreshPartsWithOrder` call
  `BoatPart.SetOptionEnabled` for ordinary and previewed options.
- SE's `Patch.PartsPatch.Adder` runs during `SaveableBoatCustomization.Awake`,
  expands the native mast array and dispatches boat-specific part installation.

Postfixes invalidate discovery at those six native methods, with ordering after
Shipyard Expansion. Native control-array contents are also compared with snapshots,
including in-place edits. A one-second discovery sweep catches delayed/inactive
parts that missed lifecycle hooks and incomplete startup inventories. Dirty
inventory refreshes before allocation. Native rope/renderer/collider occupancy and
support activation remain live checks; boat-relative pose changes rebuild the
shared seat/alias lookup. Wheel-local input rotation remains separate from the
cached mounting frame.

For runtime inspection, `NativeWinchSeats.HierarchyScans` counts hierarchy API
calls, `CandidateBuilds` counts native candidate-construction attempts, and
`WinchReservations.EntriesCreated` counts newly allocated ledger entries. These
internal counters are per boat and do not log every frame. They are not elapsed
frame-time or total managed-allocation measurements.

### Halyards and Shroud belaying pins

`HalyardWinchPlacementResolver` uses the concrete requested active mast's authored
group, preserving native array order within each source. Flying Sails request their mounting mast;
Mk.A/B/C request the aft base; native sails on custom stays request that stay's
existing aft halyard source. Topmast seats on an active lower support retain their
native association. There is no inferred alternative-mast search, generated offset or
manual halyard fallback.

Shroud uses these same resolvers. Its short mainmast has **five** ordinary reef
entries and its tall mainmast **seven**. Each keeps its own ordinary array first,
then adds sources **16/17/24** from the mainmast's separate jib reef coils. Foremast
and mizzen groups likewise add only their associated stay coils. This does not
restore the former twelve-pin pool across physical variants. Native reclaim and
stable reservations follow the same rules as on every other boat; capacity depends
on the authored sources, their live mounting support and occupancy.

### Manual sheet fallbacks

Every category names a nullable port/starboard fallback pair. Native pairs take
priority on a new allocation. Each fallback side specifies its own contact point,
normal, mounting offset, compatible template and support requirements; a captured
surface point alone is not a finished mounting pose. Never reflect one side or
generate positions along a rail or mast.

Only **Shroud / ForemastFallback** is currently populated, using its previously
measured forward trim pair. All other category fallbacks remain explicitly null,
including Jong's separate mainmast fallbacks. This is supported configuration,
not unfinished redesign work. Any future fallback additions require labeled F9
captures and visual/reachability confirmation before authoring. Missing vectors
do not block native placement.

Null, incomplete or non-finite fallbacks are unavailable as a whole. When native
pairs exhaust, missing fallback data produces one contextual error per definition
and boat/category during that boat instance's lifetime. Retries remain quiet.
A valid fallback already reserved by another owner produces ordinary exhaustion
diagnostics. No default origin placement, partial pair, generated strip or
rotated mast search remains.

### Placement logging

Read `BepInEx/LogOutput.log`; paths are under [Local investigation](#local-investigation).

| Message | Meaning |
| --- | --- |
| `Placed native winch` | Initial successful placement; inspect `origin=native/fallback`. |
| `No free native winch pair/seat` | Exhausted or unsupported; hidden controls retry. |
| `Missing or invalid sheet fallback` | Required fallback is unrecorded or invalid; error is limited per boat/category. |
| `Unmatched native sheet pair` | Missing, null or unmatched native array entries were skipped. |
| `Winch reassigned after native/support change` | A previously valid placement became unavailable or moved. |
| `Winch successfully placed after retry` | Placement recovered after an exhaustion or binding failure. |
| `Winch binding/placement failed` | Clone/binding exception; claims release and controllers survive for retry. |

Sheet messages identify boat and owner instances, forward mast, category, selected
source rig and array indices, pair identity, origin, both poses and both templates.
Halyard placement messages identify the requested active mast, source rig and
native array index.
Exhaustion separates `nativeUnavailable`, `reserved`, `missingSupports` and
`fallbackBlocked`. Success is reported after placement and showing both controls;
routine refreshes and repeated failed retries remain silent. Missing-data errors
identify the fallback name and missing/invalid side.

### Capturing proposed winch positions

Version **0.2.1** includes **LogFallbackWinchPlacement**, an on-demand capture
tool in `src/Utils`, **F9** by default. The existing `CaptureWinchPosition`
config key, capture numbering and log format are retained for compatibility. Load the
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

### Viewing winch mounting points

Version **0.2.1** includes **WinchMountOverlay**, an optional diagnostic utility
in `src/Utils`, alongside **LogFallbackWinchPlacement**. Enable it with the game
closed in `BepInEx/config/com.august.moresailwindsails.cfg`:

```ini
[Diagnostics]
EnableWinchMountOverlay = true
ToggleWinchMountOverlay = F8
CaptureWinchPosition = F9
```

`EnableWinchMountOverlay` defaults to **false**. With it enabled, load a boat,
close menus, aim at a boat surface within **10 m**, and press **F8**. The overlay
stays attached to that boat as you move around. Press **F8** again to hide it;
to inspect a different boat, hide it first, then aim and toggle again. The shortcut
is configurable; `None` disables the shortcut. Setting the enable option back to
false removes the overlay. Normal BepInEx config loading applies; editing the file
outside the game is not a live-reload mechanism.

Wireframes show through the hull and deck, including winches on inactive rig
variants. A small legend identifies the selected boat and grouped location count:

| Color | Meaning |
| --- | --- |
| Green | Unused native location on fitted supports. |
| Red | Occupied location: native bound rope, active renderer/collider, or visible custom winch. A bound native rope counts even when hidden. |
| Cyan | Unused native location on unfitted supports. |

Coincident locations are grouped using the native inventory's **1 mm**
boat-relative tolerance. Occupied aliases take priority, then unused fitted
supports, then unfitted supports. Hidden spare custom winches are excluded.
**Green does not guarantee allocation:** source category, pairing, support and
reservation requirements still govern actual placement. These traces describe
existing winches, not newly authored fallback definitions or a clearance test.

Use the overlay to avoid existing fittings, then aim at bare structure and press
**F9** to capture a proposed fallback contact point. F9 retains the
`CaptureWinchPosition` config key, existing notifications, capture numbering and
`Winch position capture #N` log format. The new utility name does not require
changing an existing shortcut. Mount orientation, offset, support requirements
and reachability still need authoring and confirmation.

The overlay and capture tool share the same read-only boat-surface picker,
including displaced walking-model ray conversion and world obstruction checks.
A separate diagnostic inventory includes inactive controls without requiring
custom sails or participating in placement reservations. Discovery and cached
shape selection refresh once per second while shown; current transforms and
occupancy are read at camera rendering time. Native meshes supply cached triangle
edges; unreadable or empty meshes use their local bounds, and skinned renderers
use local bounds. These boxes indicate extents, not an exact silhouette.

Only the main camera draws the overlay. Its owned material uses
`Hidden/Internal-Colored` with depth testing/writing disabled; an unavailable
shader or failed material pass leaves the overlay off and reports a diagnostic.
Native materials, renderers, colliders, outlines, bindings and activation states
remain untouched. Menus hide the display; loading, leaving play, losing the boat
or disabling the utility releases the selected boat and drawing resources.
Select the boat again after loading. Camera replacement needs no reattachment.
Disabled/hidden overlays do not scan the boat or draw.

#### Overlay validation (updated 2026-09-28)

- Pinned CSharpier check, Release build, GeometryChecks, AssemblyChecks and
  `git diff --check` passed. The build reported **zero warnings and errors**.
- Executed geometry checks cover hidden native bound-rope precedence, hidden
  custom-clone exclusion, coincident groups, changing occupancy and positions,
  mesh edge deduplication and the twelve-edge bounds fallback. Existing capture
  checks still cover displaced, rotated/scaled walking frames and visual-world
  distance comparison.
- Assembly checks guard the diagnostic boundary against native game-field writes,
  transform/rendering/physics setters, activation/cloning/binding and seat
  reservations, and verify reuse of native occupancy rules. These are structural
  checks, not execution of Unity lifecycle or rendering.
- The user reports that the overlay works in game, but occupied amber/yellow
  locations were too difficult to distinguish from green. Occupied traces and
  their legend now use red. **The new red color still needs visual confirmation.**
  Detailed runtime checks remain: start on Brig and confirm green/red/cyan
  traces, through-hull visibility, movement tracking,
  unchanged interaction outlines, F9 captures, and repeated toggling. Then check
  rig swaps, camera changes, loading/boat removal, and a modded boat with different
  winch geometry. Confirm bounds fallback readability and acceptable frame time.
- Built DLL: `src/bin/Release/netstandard2.0/MoreSailwindSails.dll`, version
  **0.2.1**. No installed game files or saves were replaced during this work.

#### Material constructor review follow-up (2026-09-28)

[PR #27 review](https://github.com/sum-rock/MoreSailwindSails/pull/27#discussion_r4118192217)
flagged the overlay's named material-constructor argument. Inspection of the
installed Unity assembly confirms `Material(Shader shader)` and
`Material(Material source)`; the original code also passed a forced Release
rebuild with zero warnings/errors. The reported compilation failure was not
reproduced. The overlay now passes its shader positionally, avoiding dependence
on the constructor parameter name while preserving overload selection and behavior.
CSharpier, the updated Release build (zero warnings/errors), GeometryChecks,
AssemblyChecks and `git diff --check` passed. Version remains **0.2.1**; live
rendering has not been revalidated, and no installed DLL or save was changed.

### Asset provenance and measurement fixtures

The support table below is retained as measurement provenance for manual fallback
authoring; it no longer defines runtime generated placement strips.

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

`tests/GeometryChecks/FishermansStay/NativeWinchSeats.txt` records installed
option labels, native/SE shipyard groups and prerequisites, control identities,
array correspondence and parent-local poses across all eight boats. These are
reference measurements, **not boat-local fallback vectors**. The inventory was
read from installed assets and SE's serialized part/option metadata; no proprietary
assemblies or extracted asset payloads are committed.

Earlier numerical fixtures (`WinchMeasurements.txt`, rail/surface measurements,
full-native obstruction tables and Shroud trim measurements) remain available for
manual fallback authoring. The old generated-search tests were removed with that
implementation. New checks cover profile/group membership, aliases, atomic claims,
malformed arrays, asymmetry, fallback priority/stability, missing-data recovery,
native reclaim and active-mast resolver contracts. Neither suite establishes
runtime accessibility, rendered support contact or Unity lifecycle behavior.

### Runtime validation

#### Cog halyard group (2026-09-28)

Version remains **0.2.1**. The reported Cog Mk.A failure requested the correct
aft mast, **57**, but its only reef seat was occupied by the native gaff sail.
Installed SE assets confirm the adjacent `winch_reef_midstay2` belongs to source
**58**, outside the previous search. Cog now defines an explicit halyard group
for mast 57 with ordered sources `[57, 58]`, including when the native midstay is
absent. This replaces the earlier additional-source dictionary while preserving
the Cog's seat selection. No fallback positions or native arrays were changed.

- CSharpier check, Release build (zero warnings/errors), GeometryChecks,
  AssemblyChecks and `git diff --check` passed. Executed checks cover source
  order and scope across all profiles, defensive copies, invalid/duplicate groups,
  exact mast matching, explicit source ordering, primary-seat priority,
  retained additional seats, native reclaim, support loss, reservation conflicts
  and retry recovery. Assembly checks cover shared startup/placement source
  lookup and active-mast/source-array validation; they do not execute Unity.
- The user tested the Cog change and reported that it **works perfectly** on
  2026-09-28. The requested test was the reported configuration with midstay2 absent
  and the gaff occupying the primary reef winch. This confirms the reported
  placement/routing issue is resolved in that test; no fresh log was independently
  inspected, and it does not establish every reclaim/support/reload scenario.
- The separate rope-at-boat-origin behavior during complete seat exhaustion
  remains unchanged. Built DLL: `src/bin/Release/netstandard2.0/MoreSailwindSails.dll`.
  No installed game files or saves were replaced.

#### Halyard groups on the remaining boats (2026-09-28)

Version remains **0.2.1**. The same source-group strategy now covers Brig, Junk,
Jong, Sanbuq, Leopard, Shroud and large dhow: **68 groups total**, including the
unchanged Cog group. Sources were checked against all **305** recorded native/SE/
boat-mod rig objects in the installed assets, including reef-array identities,
guide hierarchies, prerequisites and mounting paths. No native arrays or geometry
were modified, and the resolver and reservation behavior remain unchanged.

The earlier reported Brig fixture discrepancy was an audit lookup error: registered
source **70** and unregistered source **74** have the same hierarchy path but are
distinct serialized objects. Source 70 really has mainmast **B1** reef seats; source
74 really has foremast **F1** seats. `NativeWinchSeats.txt` already records both
correctly. The new mainmast groups use 70 and exclude 74; the fixture now explains
this distinction, and a regression check guards it. Audit lookup used asset,
hierarchy and order index to select the exact serialized object. No speculative
index replacement was made to game assets or existing sheet categories.

CSharpier check, Release build (zero warnings/errors), GeometryChecks,
AssemblyChecks and `git diff --check` passed. New executed fixture checks cover
all 68 groups, exact mast/section source order, requested-mast-only behavior for
unlisted masts, registered reef references, distinct added seat identities and
Brig's 70/74 distinction. Existing checks still cover reservation aliases, native
reclaim, stability and retry recovery. Built DLL:
`src/bin/Release/netstandard2.0/MoreSailwindSails.dll`. No installed game files or
saves were replaced.

The new boats still require in-game confirmation. Start on Brig and Jong, then
the other affected boats. Occupy the requested mast's own seats with native sails
and leave associated stays absent; confirm a free stay seat is claimed, correct
halyard routing and usable controls. Include both Jong mainmast positions, mast
variants/topmasts, Shroud's separate stay coils, native reclaim, support changes,
order completion/cancellation and save reloads. Neither suite simulates Unity
Cloth or executes the complete native binding lifecycle.

#### Accepted placement baseline and cleanup

The user accepted the native placement work as valid and complete on
**2026-09-27**, version **0.2.1**. The subsequent architecture cleanup is
implemented; its automated results and separate runtime validation limits are
recorded below.

Evidence for this accepted baseline:

- Release build passed with zero warnings/errors. GeometryChecks, AssemblyChecks,
  CSharpier and diff checks passed. Automated checks cover profile membership,
  identity/alias reservations, atomic pairs, fallback behavior and native wiring;
  they do not execute Unity object lifecycles or measure frame-time cost.
- After geometric clearance was removed, user testing on **Brig** confirmed both
  sheets and the mainmast halyard for Mk.C, using reef index 1
  (`rope_winch_mastB1_reef (1)`). The native sail on custom stay 144 also received
  both sheets and its mizzen halyard. The earlier observed halyard placement
  failure did not recur in this retest.
- On **Jong**, all eight logged owner instances across fitting/recreation received
  both sheets and a halyard. The final recreated configuration contained a Flying
  Sail and two Mk.B staysails; the eight instances were not eight simultaneous sails.
- BepInEx and Unity logs agreed, with no placement exhaustion or binding/placement
  failures for either boat in that session. Installed and local DLL hashes matched.
  All those placements used native seats. The user installed and tested the build;
  automated builds do not install files or modify saves.

Acceptance applies to the implemented redesign. The recorded runtime evidence
covers the configurations above; it does not claim exhaustive testing of every
boat, fallback or lifecycle transition.

For **future placement changes**, start regression testing on Brig and Jong, then
other affected boats. Fit Flying Sails, Mk.A/B/C and native sails on custom stays
alone and together; occupy/free native seats; change mast options; complete/cancel
orders; remove/recreate sails; and reload saves. Confirm reachable independent
sheets, paired movement/recovery, stable fallbacks and working halyards/rope
routing. Include Shroud's fallback and mast-local pins when affected. Record new
observations separately from automated results; this is a regression checklist,
not an outstanding acceptance gate for the completed redesign.

Cleanup implementation on **2026-09-27**, still **0.2.1**:

- Completed changes include cached native discovery and stable claims, shared
  bootstrap-template selection, matching-index sheet pairs, boolean reservation
  acquisition, immutable definitions, support-aware coincident-seat selection and
  removal of obsolete control branches.
- Release build passed with zero warnings and errors; GeometryChecks,
  AssemblyChecks (62 Harmony targets), CSharpier `check` and `git diff --check`
  passed. No README, installed game files or saves changed.
- New executed checks cover defensive copies and read-only wrappers across all
  eight profiles/111 stays; later-source and fallback bootstrap pairs; inactive
  versus active coincident references; retained claims and failed transactions;
  and the production discovery schedule/retention policy. IL checks cover shared
  bootstrap readiness, discovery before startup cloning, live validation, empty
  coordinator guards, controller retention and native mount-array updates.
- A fixed synthetic fixture uses one available pair for **600 frames at 60 Hz**.
  The inspection-derived baseline models the previous per-frame refresh/search
  and reservation recreation. The new side executes the production schedule,
  retention policy and reservation ledger under the same setup:

  | Operation | Previous model | Cleanup fixture |
  | --- | ---: | ---: |
  | Hierarchy-scan requests | 1,200 | 20 |
  | Candidate constructions | 600 | 1 |
  | Reservation entries created | 1,200 | 2 |

  Scan requests in this fixture invoke counters, not Unity hierarchy APIs. These
  results establish scheduling/allocation behavior, **not in-game performance**.
  Actual runtime counter comparison and frame-time profiling remain unmeasured.
- No new game session was run for the cleanup. The accepted Brig/Jong observations
  above belong to the earlier redesign. Retest cleanup on Brig and Jong first,
  then affected boats including Shroud; exercise missing donor templates, mixed
  Flying/Mk.A/B/C/native stay sails, native reclaim, support/array changes,
  complete/cancel, removal/recreation and repeated reloads. Neither suite
  simulates Unity Cloth or proves lifecycle behavior in game.

Built DLL: `src/bin/Release/netstandard2.0/MoreSailwindSails.dll`. This is local
development at **0.2.1**, not a release. README's pre-existing 0.2.0 version text
remains unchanged under the repository's explicit-edit policy.

### Related issues

- [#16](https://github.com/sum-rock/MoreSailwindSails/issues/16): Jong foremast staysail port sheet placement.
- [#21](https://github.com/sum-rock/MoreSailwindSails/issues/21): broader winch placement and capacity investigation.

## Local investigation

For placement diagnostics and F9 instructions, see
[Placement logging](#placement-logging) and
[Capturing proposed winch positions](#capturing-proposed-winch-positions), and
[Viewing winch mounting points](#viewing-winch-mounting-points).

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

To watch the logs live, run these manual commands from the repository root in
separate terminals:

```sh
./scripts/tail-player-log.sh
```

```sh
./scripts/tail-bepinex-log.sh
```

Each script shows the latest **50 lines**, then follows new output with `tail -F`,
including when a game restart truncates or replaces the log. Missing files are
retried. Paths use the default Steam installation beneath `$HOME`. Press
**Ctrl+C** to stop. These commands only display logs; they do not archive them.

Capture logs before restarting a freeze. Distinguish other mods' exceptions and
suspected causes from confirmed evidence. Prefer `rg`/`rg --files`; inspect
screenshots with the local image viewer. Temporary tools may exist at
`/tmp/fisherman-inspect/` (ILSpy helper/cache) and
`/tmp/fisherman-assets-env/bin/python` (UnityPy). Inspect their projects,
dependency paths and cached results before use; recreate if absent.
