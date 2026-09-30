# MoreSailwindSails development

See [README.md](../README.md) for features and player instructions and
[AGENTS.md](../AGENTS.md) for agent workflow. This guide owns technical reference,
verification procedures and [current validation status](#runtime-validation);
resolved history is in Git.

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
| `src/Plugin.cs`                   | Identity, dependencies, Harmony discovery and optional SailInfo integrations                                                        |
| `src/Sails/FishermansFlyingSail/` | Mast-mounted sail registration, rig, geometry, tension, billow and aerodynamics                                                     |
| `src/Sails/FishermansStaysail/`   | Family prefab builder, rig, fixed head, edge fitting and reefing; `MkA/`, `MkB/`, `MkC/` supply cuts and identities                 |
| `src/Stays/FishermansStay/`       | Independent mounts, registration, previews, controls and save compatibility                                                         |
| `src/BoatRigs/`                   | One class per boat owns supports, stays, mast ancestry, sheet categories and fallbacks                                              |
| `src/Controls/`                   | Native seat discovery, separate sheet/halyard resolvers, atomic reservations and owned control cloning                              |
| `src/Utils/`                      | Optional, read-only in-game diagnostic tools and their geometry helpers                                                             |

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

### SailInfo hover names (issue #17)

Optional integration inspected against installed **SailInfo 1.2.1** supplies
`Fisherman's Flying Sail` and `Fisherman's Staysail` for custom sheet and halyard
hover labels, without mark or size. Standard mode normally reads `Sail.sailName`;
Historical and Simple Positional modes otherwise generate
generic names. All three enabled modes now use the custom sail's family name.
Shipyard names retain their mark and size details; `Sail.sailName` is not modified.
SailInfo retains its name-off setting, HUD formatting, halyard suffix, numeric
readouts and vanilla sail naming. No mast location or sheet-side text is added.

`Compatibility.Patches.SailInfoNamesPatch` independently patches the parameterless
string-returning `SailInfo.WinchInfoSail.SailName()` method, validating its instance
`Sail sailComponent` field through reflection. The prefix recognizes either
custom family's rig and returns its family name, bypassing SailInfo's
positional-name cache. Missing/destroyed references, empty names and other sails
fall through. Absent SailInfo is silently skipped; an incompatible naming API
logs one warning during startup and skips this integration. There is no direct
SailInfo assembly reference. The existing staysail angle patch remains separate.

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

#### Corner-knot mesh channels

The installed Sailwind 0.39 donor `3d rope jib sheet` uses renderer **4478**
and mesh **1475** (`cloth_rope`) in `sharedassets24.assets`. Its unreadable
source has **162 vertices, normals and tangents**, with zero UVs or compressed
UV data. The `rope static` material uses `Standard` without assigned textures.
Missing source UVs are valid here; baked counts still need runtime confirmation.

Knot construction requires and preserves one native normal per vertex. Complete
UV arrays retain their selected vertex mapping and generated tangents. Zero UVs
are accepted only for `Standard` with no assigned texture properties; that path
leaves UVs and tangents absent and skips tangent generation. Partial UV arrays,
mismatched normals and UV-less textured or unsupported materials omit knots
safely. Do not generate substitute UVs or normals or modify shared donor assets.

Each template attempt logs donor/mesh identity, source count/readability, baked
channel counts, material compatibility and the selected policy. Failures identify
the incompatible channel and available diagnostic context.

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
| Shroud                     |           2 |             8 |                          2 |
| Large dhow (Sailwind 0.39) |           2 |            14 |                          8 |

These seven supported boats provide **93** variants. **Leopard is unsupported:**
its support and BoatRig profile were removed after in-game compatibility problems.
Profile gates reject Flying Sail fitting and skip Fisherman's Stay registration
on Leopard; no save migration is provided. Future compatibility work is tracked
in [issue #33](https://github.com/sum-rock/MoreSailwindSails/issues/33).

The **93** variants prefer **70°** between the aft spar's downward axis and stay.
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

### Stay collars on Sanbuq and Large Dhow

These boats fit their native forward and aft collars independently of the rope
body. The **26 Sanbuq** and **14 Large Dhow** variants seat each
collar around its supporting spar, keeping its full height below the rendered
tip. The visible rope ends at the facing collar surfaces; logical mount
endpoints, stay angles, sail fitting, control anchors and saved option IDs are
unchanged. Other boats retain their existing geometry path.

Sanbuq's separate native coils supply both attachments, reusing the forward
coil when a donor lacks an aft coil. Its walking representation uses the same
coil source even when the native walking donor omits collars. SE's non-readable
rope-body meshes remain shared and are fitted using their own bounds. Large
Dhow's combined meshes are separated by position-welded connectivity into the
long rope and compact endpoint regions; the original seam indices, submeshes,
materials and mesh channels survive in owned copies. Only copied geometry is
changed. Each representation retains its own rope bounds and coordinate frame;
new walking collars use the walking geometry's layer.

Collar frames come from the native ring geometry. Radius fitting uses the
rendered supporting mesh, not the capsule tip or radius. The sole unreadable
support is SE's Sanbuq mizzen topmast **80**, `mizzen_topmast_sanbuq`:
`Sanbuq.cs` records its measured wooden-spar taper, excluding attached holders.
The authored taper agrees with the installed mesh at the affected collar
heights. Static seating calculations are cached per collar; preview refreshes
reapply the mast frame. Unexpected donor geometry rejects registration through
the existing rollback. Generated meshes are owned by the stay and destroyed on
rollback or teardown; shared donor assets and live Cloth are untouched.

## Winch placement

The native winch placement redesign is **accepted as valid and complete**.
It supports the seven boats in [Boat profiles and stays](#boat-profiles-and-stays)
and applies to Flying Sails, all three staysail cuts and native sails on
Fisherman's Stays. The follow-up architecture cleanup is implemented.
[Runtime validation](#runtime-validation) separates accepted Brig/Jong evidence
from later changes and remaining in-game uncertainty.

### Placement and ownership

Family rigging remains separate; placement, bootstrap-template selection, cloning
and allocation are shared. Boat profiles provide authored data, native inventory
discovers controls, and separate sheet/halyard resolvers select seats. The
boat-owned reservation ledger and clone coordinator manage allocation and
control lifetime.

Each boat profile authors physical mast categories separately from ordered native
sheet-source rigs. Sources include physical variants, their topmasts and verified
associated native stays. Lookup uses IDs and authored ancestry, never display-name
parsing. Definitions defensively copy collection inputs into read-only wrappers;
the catalog is cached. Categories store ordered source IDs directly, with pairing
only at matching native array indices. Native and installed SE shipyard group
metadata distinguishes variants from simultaneous mast positions: Jong has
separate `MainmastA`/`MainmastB` categories; Cog's raked foremast belongs to its
separate foremast group.

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
hidden, as does an active renderer or collider. Mounting ancestors and requested
physical supports must be active. Native poses and manually authored fallback positions are trusted:
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
reef winch. **Cog mast 8 (original mizzen)** uses **[8, 51, 65]**: its own seat,
then the shared `winch_reef_midstay1` referenced by midstays **51 (2-1)** and
**65 (1-1)**. These two references add only one distinct seat. The source stay need
not be installed, but the requested physical mast and the selected winch's mounting
support must be active.
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

| Boat       | Halyard groups |
| ---------- | -------------: |
| Brig       |             14 |
| Junk       |              9 |
| Jong       |              7 |
| Sanbuq     |             13 |
| Cog        |              2 |
| Shroud     |              6 |
| Large dhow |              9 |

The supported catalog has **60 halyard groups**.

Unlisted masts retain their original lookup, including Jong's raked foremast and
unsupported bermuda variants. Cog covers both ordinary mizzen variants; the
other six supported profiles cover their audited stay reef seats.

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
inactive with owned handles and fresh outlines; native objects and bindings
remain untouched.
Template changes prepare both replacement sheet clones before retiring either
old clone, retain sail-owned controllers and update custom-mount winch arrays.
Placement moves the parent mount and preserves wheel-local input rotation.

Validate current source-array slots, mounting support, native occupancy, aliases
and poses before constructing alternatives. A valid placement retains its ledger
entries and clones; parent mounting poses and visibility still update. Native
reclaim, lost support or a changed boat-relative pose invalidates the entire pair.
Keep a valid current pair stable, including a fallback when native seats later
free. Exhaustion hides both sheets, preserves controllers and retries every
**one second**; it does not roll back stays or remove saved sails. Removal and inactive owners release
claims. All placement state is transient; GUID, prefab/stay IDs, save ordering and
saved geometry are unchanged.
The separate rope-at-boat-origin behavior during complete seat exhaustion remains
unchanged.

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

| Message                                        | Meaning                                                                         |
| ---------------------------------------------- | ------------------------------------------------------------------------------- |
| `Placed native winch`                          | Initial successful placement; inspect `origin=native/fallback`.                 |
| `No free native winch pair/seat`               | Exhausted or unsupported; hidden controls retry.                                |
| `Missing or invalid sheet fallback`            | Required fallback is unrecorded or invalid; error is limited per boat/category. |
| `Unmatched native sheet pair`                  | Missing, null or unmatched native array entries were skipped.                   |
| `Winch reassigned after native/support change` | A previously valid placement became unavailable or moved.                       |
| `Winch successfully placed after retry`        | Placement recovered after an exhaustion or binding failure.                     |
| `Winch binding/placement failed`               | Clone/binding exception; claims release and controllers survive for retry.      |

Sheet messages identify boat and owner instances, forward mast, category, selected
source rig and array indices, pair identity, origin, both poses and both templates.
Halyard placement messages identify the requested active mast, source rig and
native array index.
Exhaustion separates `nativeUnavailable`, `reserved`, `missingSupports` and
`fallbackBlocked`. Success is reported after placement and showing both controls;
routine refreshes and repeated failed retries remain silent. Missing-data errors
identify the fallback name and missing/invalid side.

### Capturing proposed winch positions

**LogFallbackWinchPlacement** is an on-demand capture tool in `src/Utils`,
**F9** by default. Preserve its `CaptureWinchPosition` config key, capture
numbering and log format for compatibility. Load the
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

**WinchMountOverlay** is an optional diagnostic utility in `src/Utils`.
Enable it with the game
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

| Color | Meaning                                                                                                                               |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Green | Unused native location on fitted supports.                                                                                            |
| Red   | Occupied location: native bound rope, active renderer/collider, or visible custom winch. A bound native rope counts even when hidden. |
| Cyan  | Unused native location on unfitted supports.                                                                                          |

Coincident locations are grouped using the native inventory's **1 mm**
boat-relative tolerance. Occupied aliases take priority, then unused fitted
supports, then unfitted supports. Hidden spare custom winches are excluded.
**Green does not guarantee allocation:** source category, pairing, support and
reservation requirements still govern actual placement. These traces describe
existing winches, not newly authored fallback definitions or a clearance test.

Use the overlay to avoid existing fittings, then use
[F9 captures](#capturing-proposed-winch-positions) on bare structure to propose
fallback contact points. Mount orientation, offset, support requirements and
reachability still need authoring and confirmation.

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

### Asset provenance and measurement fixtures

The support table below records measurement provenance for manual fallback
authoring; it does not define runtime generated placement strips.

Installed references for supported boats include `Sailwind_Data/level24`, SE's
`shipyard_expansion.assets` and `ShatteredSeasExpansion/veil piercer`. Include
import-parent transforms when comparing measurements in boat coordinates. Confirm
dependencies against installed assemblies. Fixtures contain numeric measurements
only, never meshes, textures or assemblies.

| Boat       | Permanent support references                                                                                       |
| ---------- | ------------------------------------------------------------------------------------------------------------------ |
| Brig       | `medi medium new/structure_container/trim_006` rail caps; omit bevels/bends and stair opening                      |
| Sanbuq     | `structure/Cube_013` forward/raised aft caps; exclude lower trim                                                   |
| Junk       | `structure/trim_001` caps, `Cube_035` handrails, `Cube_032` transverse reef beam                                   |
| Jong       | `structure/trim_010` forward/middle/aft caps                                                                       |
| Cog        | `structure/trim_001` aft caps                                                                                      |
| Shroud     | `Clipper_Upper_Trim` fixed forward sheet points and aft strips; native mainmast/mizzen side-pin seats for halyards |
| Large dhow | `Cube_001` and `Cube_008` lower/sloped/raised caps and inner aft rail faces                                        |

`tests/GeometryChecks/FishermansStay/NativeWinchSeats.txt` records installed
option labels, native/SE shipyard groups and prerequisites, control identities,
array correspondence and parent-local poses across the seven supported boats.
These are reference measurements, **not boat-local fallback vectors**. The inventory
was read from installed assets and SE's serialized part/option metadata; no
proprietary assemblies or extracted asset payloads are committed.

`NativeWinchSeats.txt` distinguishes Brig's registered source **70** (mainmast
**B1** reef seats) from unregistered source **74** (foremast **F1** seats). Their
hierarchy paths match, but they are distinct serialized objects. Mainmast halyard
groups use 70 and exclude 74; identify audit objects by asset, hierarchy and order
index rather than path alone.

Earlier numeric fixtures (`WinchMeasurements.txt`, rail/surface measurements,
full-native obstruction tables and Shroud trim measurements) remain available for
manual fallback authoring. They do not reinstate generated placement searches.

### Runtime validation

This is the recorded validation status for **0.2.1 preparation**, through
**2026-09-29**. The results below come from implementation checks and user tests;
they are not new test runs performed during documentation consolidation.

#### Recorded automated results

The documented changes passed pinned CSharpier checking, Release builds with
zero warnings/errors, GeometryChecks, AssemblyChecks and `git diff --check`.
The latest recorded assembly run covered **62 Harmony targets**. Current catalog
coverage is seven supported boats, **93 stays** and **60 halyard groups**.

| Area                  | Recorded coverage                                                                                                                                                                                                                                                                                                                                           |
| --------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Placement and cleanup | Profile membership and immutable definitions; native identities/aliases; matching-index atomic pairs; fallback priority/stability; bootstrap templates; active supports; native reclaim; retained claims, retry and discovery scheduling. Assembly checks cover startup order, control retention and native mount-array updates.                            |
| Halyard groups        | Exact requested-mast/source order, own-seat priority, registered reef references, shared Cog 51/65 identity and duplicate-claim prevention, Brig 70/74 distinction, support loss and recovery.                                                                                                                                                              |
| Leopard exclusion     | Rejection of base and repeated-clone names and the remaining supported catalog.                                                                                                                                                                                                                                                                             |
| Controller teardown   | Preservation policy, retirement/adoption ordering, reconciliation cleanup and destruction entrypoints; structural checks do not execute Unity destruction or exception recovery.                                                                                                                                                                            |
| SailInfo names        | Compatible and incompatible reflection contracts, family-only labels, empty-name fallback, startup and installed HUD wiring; no live HUD rendering.                                                                                                                                                                                                         |
| Corner knots          | Optional-UV/material policy, malformed channels, reordered mapping, unchanged source arrays, inactive construction and cleanup; no Unity baking or rendering.                                                                                                                                                                                               |
| Stay collars          | Mesh region/channel preservation, native ring frames, rendered-tip/taper fitting, rope continuity and displaced walking frames; structural rollback/teardown checks. A read-only installed-asset audit passed **160 cases**: both ends of all **40** affected variants in visual and walking representations, including Sanbuq topmast 80's authored taper. |
| Overlay and captures  | Occupancy/alias precedence, hidden-clone exclusion, mesh edges/bounds fallback, displaced walking-frame ray conversion, and structural checks that diagnostics do not mutate game state or reserve seats.                                                                                                                                                   |

The cleanup's synthetic **600-frame / 60 Hz** single-pair fixture reduced scan
requests from **1,200 to 20**, candidate builds from **600 to 1**, and reservation
entries from **1,200 to 2**, relative to an inspection-derived model of the old
behavior. Scan requests invoke counters, not Unity hierarchy APIs. This establishes
scheduling/allocation behavior; runtime counter comparison and frame-time profiling
remain unmeasured.

Neither suite simulates Unity Cloth or proves live rendering, control binding,
destruction, shipyard or save/reload behavior. The installed-asset collar audit
also does not establish visual acceptance.

#### Confirmed game observations

- **Placement baseline, 2026-09-27:** the user accepted the redesign as valid and
  complete. On Brig, Mk.C received both sheets and its mainmast halyard at reef
  index 1 (`rope_winch_mastB1_reef (1)`); a native sail on custom stay 144 received
  both sheets and its mizzen halyard. On Jong, all eight logged owner instances
  across fitting/recreation received sheets and a halyard; the final configuration
  was a Flying Sail and two Mk.B staysails, not eight simultaneous sails.
  BepInEx and Unity logs agreed, with no exhaustion or binding/placement failures;
  installed/local DLL hashes matched. All observed placements used native seats.
- **Cog mast 57, 2026-09-28:** the user reported the placement/routing fix worked
  with midstay2 absent and the gaff occupying the primary reef winch. No fresh log
  was independently inspected; reclaim, support changes and reloads were not all
  established by that test.
- **Overlay, 2026-09-28:** the user reported it worked, but occupied amber/yellow
  locations were hard to distinguish from green. Occupied traces now use red;
  the new color still needs visual confirmation.

These observations apply to the tested configurations. The accepted Brig/Jong
session predates the architecture cleanup, for which no dedicated game session
was recorded. It does not establish every boat, fallback or lifecycle transition. Cog mast 57 confirmation does not validate the
later mast 8 change. Earlier 0.2.0 placement failures are a regression baseline,
not evidence that those failures recur on other boats in 0.2.1.

### Related issues

- [#16](https://github.com/sum-rock/MoreSailwindSails/issues/16): Jong foremast staysail port sheet placement.
- [#21](https://github.com/sum-rock/MoreSailwindSails/issues/21): broader winch placement and capacity investigation.

## Local investigation

For placement diagnostics, see [Placement logging](#placement-logging),
[Capturing proposed winch positions](#capturing-proposed-winch-positions) and
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
