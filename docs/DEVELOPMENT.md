# MoreSailwindSails development

See [README.md](../README.md) for features and player instructions and
[AGENTS.md](../AGENTS.md) for agent workflow. This guide owns technical
reference, verification procedures and
[current validation status](#runtime-validation); resolved history is in Git.

## Technical reference

| Area                                     | Reference                                                           |
| ---------------------------------------- | ------------------------------------------------------------------- |
| Plugin identity, versions and prefab IDs | [Release and manual installation](#release-and-manual-installation) |
| Directories, namespaces and test layout  | [Source organization](#source-organization)                         |
| Cloth, templates, controls and Harmony   | [Runtime safeguards](#runtime-safeguards)                           |
| Sail-family mechanics                    | [Flying Sail](#flying-sail), [Staysails](#staysails)                |
| Authored stays and boat support          | [Boat profiles and stays](#boat-profiles-and-stays)                 |
| Native seats and manual fallback pairs   | [Winch placement](#winch-placement)                                 |
| Game checks and observed behavior        | [Runtime validation](#runtime-validation)                           |
| Profiling, installed references and logs | [Local investigation](#local-investigation)                         |

## Build and automated checks

The pinned Nix flake supports **x86_64 Linux** with .NET 8; the plugin targets
`netstandard2.0`. Install Sailwind and BepInEx 5 locally. Shipyard Expansion is
optional. The build references installed game, BepInEx, HarmonyX and Unity
assemblies; no Unity editor or separate asset bundle is required.

From the repository root, restore a fresh checkout:

```sh
nix develop
dotnet tool restore
dotnet restore
```

Initial Nix/NuGet restores need network access. Keep `flake.lock` and
`.config/dotnet-tools.json` pinned. The flake also pins Prettier for Markdown.
Run `pre-commit install` inside the development shell to enable the CSharpier
and Markdown formatting hooks, which invoke Nix on later commits. Hooks format
staged files; review and stage any formatting changes before retrying the
commit.

After restoring, run from the repository root:

```sh
nix develop -c prettier --check "**/*.md"
nix develop -c bash -c 'dotnet csharpier check . && dotnet build -c Release --no-restore && dotnet run --project tests/GeometryChecks -c Release --no-restore && dotnet run --project tests/AssemblyChecks -c Release --no-restore'
git diff --check
```

Use `dotnet csharpier format .` inside the development shell to fix formatting;
`check` does not rewrite files. Output:
`src/bin/Release/netstandard2.0/MoreSailwindSails.dll`.

For Markdown, run `nix develop -c prettier --write "**/*.md"` to format or use
`--check` for read-only verification. `.prettierrc.json` sets an 80-column prose
wrapping target and preserves embedded code formatting. `.prettierignore`
excludes build outputs. To exercise the Markdown hook across tracked files, run
`nix develop -c pre-commit run prettier-markdown --all-files`.

The default game directory is `~/.local/share/Steam/steamapps/common/Sailwind`.
For another installation, pass `-p:SailwindDir="/path/to/Sailwind"` to the
plugin and test builds. AssemblyChecks also needs the path as a runtime
argument:

```sh
nix develop -c dotnet build -c Release -p:SailwindDir="/path/to/Sailwind"
nix develop -c dotnet run --project tests/AssemblyChecks -c Release -p:SailwindDir="/path/to/Sailwind" -- "/path/to/Sailwind"
```

- **GeometryChecks** executes pure calculations: geometry/skinning, coupled
  tension, wind frames, hoist/reef poses, collision/travel bounds, text
  wrapping, authored profiles, winch placement and older shipyard snapshots. All
  three staysail cuts share a fixed-head, sheet, reef and mirrored-skin behavior
  matrix.
- **AssemblyChecks** inspects installed signatures, Harmony wiring/order, native
  list restoration, appearance, registration/save capacity and lifecycle
  structure. Use direct IL decoding; Harmony native patch stubs failed in this
  environment. Structural checks do not execute control exception recovery or
  Unity prefabs.

Neither suite simulates Unity Cloth, rendering, audio initialization, hinge
physics or the live shipyard. Passing checks do not establish in-game behavior.

## Release and manual installation

Development version is **0.3.0-dev** in `Plugin.PluginVersion` and the project
`<Version>`; BepInEx requires numeric runtime metadata **0.3.0**. Keep release
versions and release documentation aligned. README changes require an explicit
request. Preserve GUID `com.august.moresailwindsails`, assembly
`MoreSailwindSails.dll`, display name/namespace `MoreSailwindSails`, and these
IDs:

| Identity                      | IDs             |
| ----------------------------- | --------------- |
| Flying Sail                   | **400**         |
| Fisherman's Staysail Mk.A/B/C | **401/402/403** |
| Fisherman's Stay mounts       | **128–255**     |

Distribute only the plugin DLL. Builds/checks do not install it, alter saves or
publish releases. The scripts below are **manual maintainer workflows**; agents
must never execute files from `scripts/` or use it as their working directory.

- `scripts/install-local.sh` requires an existing Release DLL and a valid
  BepInEx plugin directory, **rebuilds** the plugin, then copies it into that
  directory. Close Sailwind first. Its optional argument selects a different
  installation. The copy uses that argument, but the build uses the project's
  default `SailwindDir`; it does not forward the destination as a build
  property.
- `scripts/tag-release.sh` requires a clean checkout, switches to `master`,
  fetches/fast-forwards and requires equality with the fetched remote head. It
  checks matching plugin/project versions and tag availability, then requests
  explicit confirmation before creating/pushing annotated `v<version>`,
  rebuilding Release and publishing via authenticated `gh`. Branch
  switching/fetching happen before confirmation. Build/publication failure can
  leave a pushed tag.

After installation, confirm `MoreSailwindSails 0.3.0-dev loaded!` and
registration of the intended prefabs in `BepInEx/LogOutput.log`. Compare
installed and built DLL hashes when diagnosing; they are separate files.

## Source organization

Feature directories and namespaces follow `MoreSailwindSails`. Use named
arguments where practical and one class per file with a responsibility comment.
Stay/sail patches are grouped by integration in each feature's `Patches/`
directory, with file and class responsibility comments. Preserve Harmony class
names, namespaces and ordering when moving patches.

| Location                          | Responsibility                                                            |
| --------------------------------- | ------------------------------------------------------------------------- |
| `src/Plugin.cs`                   | Identity, configuration, Harmony discovery and optional integrations      |
| `src/Sails/FishermansFlyingSail/` | Flying Sail prefab, rig, geometry and aerodynamics                        |
| `src/Sails/FishermansStaysail/`   | Shared staysail mechanics; `MkA/`, `MkB/`, `MkC/` own cuts and identities |
| `src/Stays/FishermansStay/`       | Mount registration, previews, shipyard navigation and save compatibility  |
| `src/BoatRigs/`                   | Per-boat supports, stays, ancestry, control sources and fallbacks         |
| `src/Controls/`                   | Native seat discovery, allocation, reservations and owned controls        |
| `src/Compatibility/`              | Shared order text and optional SE/SailInfo integration                    |
| `src/Visuals/`                    | Shared routed rope rendering                                              |
| `src/Utils/`                      | Diagnostics and profiling                                                 |

Shared profile types live in `BoatRigs/Definitions/` with the `BoatRigs`
namespace. Native integration belongs in each feature's `*SailwindPatches.cs`;
staysail angle reporting uses `StaysailSailInfoPatches.cs`. Registration follows
optional SE initialization and precedes All Sails inventory caching.

Keep sail-family mechanics independently editable. Add new families under
`src/Sails/<Family>/` and preserve donor hierarchy names required by native
components.

Both check suites mirror feature directories. Family-level staysail behavior
uses `BehaviorCases`; cut-specific checks belong under `MkA/`, `MkB/`, `MkC/`.
GeometryChecks links source files directly; AssemblyChecks resolves full type
names and keeps Harmony/IL helpers under `Shared/`. Update both suites when
moving or renaming code.

## Runtime safeguards

- **Live Cloth topology stays fixed.** Shape by moving existing bones; no mesh
  swaps, bind-pose changes, Cloth rebuilding or solver resets on tacks. The
  mirrored-mesh experiment passed geometry checks but detached/reset in game.
  Initialization and existing furl/render refreshes serve separate purposes.
- Construct templates under inactive parents with fresh Cloth for new topology.
  Preserve the donor Animator for optional SE scaling and the hierarchy expected
  by `SailShadowCol`. Template-owned and instance-owned meshes have distinct
  lifetimes. Never mirror collider transforms using negative scale.
- Rope endpoints are independent leaves: native `RopeEffect` calls `LookAt` on
  them and must not rotate skin bones. Preserve coupled edge fitting and finite
  fallbacks.
- Keep corner control, appearance and propulsion separate. Aerodynamic frames
  follow posed corners; force patches are scoped to custom sails. Do not fake
  SailInfo values or alter vanilla forces. Retain donor wind-cloth response
  unless evidence warrants a change.
- Filter sails with custom paired controls only during native control binding
  and restore the full list in a finalizer. Capacity, collision, overlap and
  saves must see all sails.
- Fisherman families resolve authored, connected **active** mast
  sections/guides. Registration and previews can precede activation; protect
  occupied stays and actual support parts.
- Clamp fisherman sail travel to **±40°** after native controller updates,
  preserving tighter limits without snapping transforms. Paired sheets use
  `JibAngleMaster.Update`.
- One shared order-text guard covers Flying Sail, all staysail marks and stay
  parts before NANDFixes. Installed **1.4.4** still recursively submits long
  arrow prefixes. Preserve the 45-character limit, existing name boundaries and
  explicit line breaks. HarmonyX runs later prefixes even after `false`: append
  wrapped lines to the native list and consume input so later prefixes cannot
  recurse on it. Keep this wrapping active without NANDFixes too.
- New sails use native white palette **11**. Both Fisherman sail families use
  the built-in white texture on their private recolored materials. Preserve
  saved colors, native recoloring and the hidden color-reference renderer. SE
  uses its blank white selection for these prefabs when its supported texture
  API is present; see
  [Optional Shipyard Expansion](#optional-shipyard-expansion). Never change
  donor/shared assets.

Both Fisherman prefab builders validate donor index **110**, `Sail`, `Cloth` and
its `SkinnedMeshRenderer`, plus destination-slot availability. They do not
require the donor's exact name/category or deployed mesh data: authored meshes
replace that geometry. Registration logs report only the constructed sail's
area, avoiding native `GetSailArea` reads of donor vertices/triangles.
Component-specific rig checks still validate the machinery used during
construction. Flying Sail knot mesh checks remain because knot construction
consumes the native mesh through `BakeMesh`.

### Native wind-shadow samples

Both families use nine center-plane vertices without triangles for
`SailShadowCol`. Installed Sailwind 0.39 inspection confirmed that `CheckShadow`
reads vertices and casts upwind rays; a separate `BoxCollider` receives them.
Preserve sample positions, bounds and collider setup. This mesh controls wind
shielding, not visible shadows.

### Sailwind 0.39 audio

`SailFlapAudio` searches only its parent and grandparent for `Sail`. Keep the
donor wind-center beneath the pivot frame during inactive construction and
preserve its world pose. Aerodynamic refreshes update its center/orientation.
Retain native clips, unmute delay and snap initialization.

### SailInfo hover names

Optional reflection-based integration, inspected against SailInfo **1.2.1**,
supplies grouped Flying Sail/Staysail names in Standard, Historical and Simple
Positional modes. It preserves settings, suffixes, numeric readouts and vanilla
names. Missing SailInfo is skipped; an incompatible API warns once. Staysail
angle reporting remains a separate patch.

## Custom rope rendering

Flying Sail sheets, shared spans and fixed ties follow `Settings.clothRopes`.
`RoutedRope` uses six-sided tubes with the native rope material and shadow
settings when enabled, and flat lines otherwise. Staysails retain native rope
rendering.

Each rope owns one reusable mesh, receives the final posed route and compensates
for its parent transform. Invalid routes are not uploaded; missing donor
materials fall back to flat lines and retry. Mode changes show one
representation, disabling hides both, and destruction releases only owned
geometry. Native assets and Cloth remain untouched.

Geometry and ownership checks pass; rendering, runtime switching and performance
still need game validation. See the [runtime checklist](#runtime-checklist).

## Flying Sail

- `FishermansFlyingSailPrefab` owns template registration and shipyard inventory
  integration, following the `FishermansStaysailPrefab` naming convention.
- Fits a physical mast under **Other**, with active aft support, native mast
  save slots and normal vertical-space/overlap rules. Hoists from deck; partial
  hoists use a procedural renderer, full deployment uses Cloth, and striking
  hides cloth and parks ropes. Only the four corners are pinned.
- Base width is donor 110's `installHeight / 3`; installation height comes from
  the luff. The isosceles trapezoid has luff `2 × width`, head rising **20°**
  aft, foot falling **20°** aft and aft edge about `2.728 × width`. Derive edge
  budgets, area, bounds and shadow samples from this cut. New selections use
  native `Sail.LoadScale(y: 1f, z: 1f)` after optional SE initialization;
  preserve saved sizes.
- Retain **85%** upper-corner sheeting response with coupled foot/leech fitting.
  The attempted 60% response caused in-game creases and was reverted.
- Pivot around the offset luff. Two fixed **0.4572 m (18-inch)** ties hold its
  corners beyond the mast capsule surface; they do not scale. Mast ends follow
  hoisting height; ties hide when struck, unsupported, loading or disabled.
- The luff arches mastward on both tacks, capped at **0.2286 m (9 inches)**.
  Shrinking reduces it with the smallest scale; enlargement retains the cap.
  Scale-dependent Cloth coefficients reserve at least one-quarter of the mast
  gap beyond arch plus travel. Update coefficients during fitting/scaling; tack
  changes move bones only.
- The transverse rest section is a circular arc of depth `0.20 × width`,
  retained from head to foot. Twelve shaping intervals use the existing **24 ×
  32** mesh. Interior travel stays near local camber with edge freedom. Bending
  stiffness is **0.15**, stretching **0.99**, native WindCloth damping
  **0.08–0.45**.
- Collision uses thin neutral trapezoid strips swept around the offset luff,
  separate from billow bounds. Retain the first strip; include mast radius/tie
  gap in span checks and compare each head to its own active guide. Restore the
  aligned neutral rotation after sweeping; keep native obstruction rules.

### Flying Sail authoring and bake

The editable [Blender asset](../assets/flying_sail/fishermans_flying_sail.blend)
is a flat 4.6-unit-wide cut with a 9.2-unit luff and a **24 × 32** quad grid.
Blender X points aft, Z up and Y is zero; the forward head is the origin,
transforms are identity and normals face −Y. UVs form one proportional planar
island with a 5% margin.

From the repository root with Blender **5.1.1**:

```sh
blender --background --factory-startup --python-exit-code 1 --python tools/generate_flying_sail.py
blender --background assets/flying_sail/fishermans_flying_sail.blend --python-exit-code 1 --python tools/bake_flying_sail.py
blender --background assets/flying_sail/fishermans_flying_sail.blend --python-exit-code 1 --python tests/BlenderChecks/check_flying_sail_bake.py
```

Generation replaces the asset; append `-- --output /tmp/flying-sail.blend` to
preserve hand edits. Baking reads without saving the asset and exports
normalized positions, UVs and triangles to `FishermansFlyingSailBakedMesh.g.cs`.
Append `-- --check` to verify freshness or `-- --output /tmp/flying-sail.g.cs`
for a separate output. Include generated C# with asset changes; .NET builds need
no Blender or asset bundle.

The baker rejects incompatible outlines, topology, transforms, modifiers, shape
keys and UVs. Runtime geometry scales the baked cut and adds camber/luff bow;
bones and corners derive from the mesh. UVs remain fixed during live shaping.
Shared authoring utilities live in
[tools/sail_authoring.py](../tools/sail_authoring.py).

### Rope visuals and knots

Head/foot routes join fixed mast ties and sail corners. The shared head span
passes through the aft guide before branching to sheets; lower branches run from
clew to controls. Draw shared spans once and retain native slack-based sag,
materials and widths. A marker-scoped `RopeEffect.LateUpdate` postfix suppresses
the original visuals without changing tension or input. Strike parks branches;
loading, missing supports and disabling hide custom routes.

Each corner uses one native-style knot. Bake the unreadable jib-sheet mesh from
an inactive private renderer copy and isolate its connected knot geometry.
Template ownership covers the generated mesh; knot leaves remain outside fabric
scaling. Missing or incompatible donors warn once and omit knots without
disabling the sail.

#### Corner-knot mesh channels

Preserve native normals and complete UV channels. The installed untextured
`Standard` rope material permits zero UVs; that path omits UVs/tangents and
skips tangent generation. Partial UVs, mismatched normals or UV-less textured
materials are rejected. Do not invent replacement channels or modify shared
donor assets. Template diagnostics report source/baked channel counts and
compatibility.

## Staysails

All marks fit registered Fisherman's Stays. Register after optional SE and
before All Sails caches its inventory. Each mark's `FishermansStaysailShape`
supplies geometry and owned-asset prefixes for template and instance creation;
do not reintroduce separate geometry/prefix arguments. The family builder owns
donor **110** and template slope **20°**; first binding creates the owned mesh
for the actual stay slope before enabling Cloth. Mesh and bind poses then stay
fixed.

Native `ShipyardSailInstaller.InstallSail` calls `MastNotTallEnough`, whose
staysail fit patch runs custom support and geometry validation. There is no
separate staysail `InstallSail` guard duplicating that check. The
`GetInstallError` patch retains specific shipyard error text. Flying Sail keeps
its own installation guard because it does not override `MastNotTallEnough`.

| Cut  | Geometry in the forward-mast frame                                                   |
| ---- | ------------------------------------------------------------------------------------ |
| Mk.A | Original 110° foot cut, sloping downward aft                                         |
| Mk.B | Same head/width, 90° foot cut; deck-parallel on upright masts                        |
| Mk.C | Same head/width, 50% longer luff (`1.5 × width`), foot rising `width × tan(40°)` aft |

Cut construction accepts finite head slopes from **0° to 80°**, with positive
luff and leech geometry. Mk.C keeps its fixed `1.5 × width` luff. All authored
stays rise in their forward-mast frame.

The deployed luff stays on the forward mast and neutral head aligns with the
stay. The native stay slot owns the save; installation coordinate measures
downward from the stay's forward endpoint. Retain **15 cm** head/aft-mast
clearances, spar-length/span fitting and native collision checks. Collision
strips use lowest head/highest foot with **5 cm** edge margins; disable strips
too shallow for margins plus **1 cm** height or clipped below **1 mm** width.
New selections start at **50% width/height** through native `Sail.LoadScale`.
Native scaling and persistence handle installations without SE; when installed,
SE patches those same native APIs. MSS does not force a scaling mode or replace
saved sizes with new-sail defaults.

### Blender authoring meshes

[tools/generate_staysail.py](../tools/generate_staysail.py) creates editable
[Mk.A](../assets/staysails/fishermans_staysail_mka.blend),
[Mk.B](../assets/staysails/fishermans_staysail_mkb.blend) and
[Mk.C](../assets/staysails/fishermans_staysail_mkc.blend) assets. Nominal
dimensions live in
[tools/staysail_authoring.py](../tools/staysail_authoring.py); shared grid, UV
and export utilities live in `tools/sail_authoring.py`.

Each cut is 13.8 units wide with a 20° head slope and **24 × 32** quad grid. A/B
luffs are 13.8 units; C's is 20.7. Blender X points aft, Z up and Y is zero; the
forward head is the origin, transforms are identity and normals face −Y. UVs
form one proportional planar island with a 5% margin. Preview materials are not
exported.

From the repository root with Blender **5.1.1**, using B as an example:

```sh
blender --background --factory-startup --python-exit-code 1 --python tools/generate_staysail.py -- --cut B
blender --background assets/staysails/fishermans_staysail_mkb.blend --python-exit-code 1 --python tools/bake_staysail.py -- --cut B
blender --background assets/staysails/fishermans_staysail_mkb.blend --python-exit-code 1 --python tests/BlenderChecks/check_staysail_bake.py -- --cut B
```

Use matching assets and `--cut A`, `B` or `C`; A is the default. Generation
replaces the selected asset, so add `--output /tmp/staysail.blend` to preserve
hand edits. Baking reads without saving the asset and exports normalized arrays
to the cut's generated C# file. Add `--check` after `--` to verify freshness or
`--output /tmp/staysail.g.cs` for a separate output. Include generated C# with
asset changes; .NET builds need no Blender or asset bundle. Run Blender checks
separately for every affected cut.

The baker validates outlines, row order, connectivity and planar UVs. Changes to
dimensions, topology, transforms, modifiers, shape keys or UV seams require
coordinated rig and bake-validation changes.

Each shape passes its baked arrays to `FishermansStaysailMeshFitting.Fit`, which
scales the cut, fits the head slope, projects UVs from fitted bounds and adds
rest camber. Mesh/UV assignment and tangent calculation occur before Cloth is
enabled; tacking and reefing do not rewrite them. Shared geometry helpers own
skinning, constraints, bone indices, centroids and wind-shadow samples.

Native recoloring creates a private material shared with reefed renderers and
the furled color reference. Both families use plain white textures because the
donor atlas does not match their planar UVs. Optional SE texture handling is
described [below](#optional-shipyard-expansion). IDs, placement/scale
conventions and save formats remain unchanged; live save compatibility still
needs testing.

### Fixed head and reefing

- Upper aft target is **14° × clamped currentUnroll** leeward: **14°/7°/0°** at
  full/half/zero deployment. Rotate the neutral head about the fore-mast axis,
  preserving height/span and independence from lower sheets and wind strength.
  `FishermansStaysailFixedHead.DeployedAngle` owns the shared 14° constant.
- Select tacks from apparent wind projected on
  `Cross(mastAxis, neutralAftDirection)`. A **±0.6 m/s** deadband retains the
  previous side; indeterminate initialization defaults positive. Smooth
  transitions. `FishermansStaysailEdgeFit` retains support bow and coupled
  lower-corner fitting without moving the fixed head.
- Rounded billow uses a spanwise sine with `(1-v)*(1+0.75*v)` vertical taper and
  a **12%-width** head peak. Interior travel is capped near **60%** of local
  camber plus foot/leech allowances and clew taper. Resolve the support frame
  once per bone update so support changes remain visible.
- Reef **upward** using the brig jib's native `reef` clip/controller and
  `furled__sail_cloth_jib` mesh from `sharedassets15.assets`. Sample an
  inactive, stripped animation hierarchy at `1 - currentUnroll`, with no live
  donor scripts or colliders. Preserve animation binding paths and donor
  Animator; disable the original reef component so it cannot compete with custom
  rendering. Only the rope-position channel is normalized for vertical lift.
  Interior camber is applied separately by the bone update.
- The lower corner gathers toward the reefed head angle. Below **4%**
  deployment, show the recolorable native bundle fitted between posed head
  endpoints, returning to neutral at full furl. Above it use the sampled panel
  pose, then initialized Cloth at full deployment. Do not substitute Flying Sail
  hoisting.
- Clone the aft-base reef winch for the native reef controller. Route its
  halyard through active aft guides (with **5 cm** separation) and a leaf below
  the top-aft bone. Keep independent lower sheets; no fore halyard or decorative
  upper sheets. Resolve active aft ancestry without falling back to the fore
  mast; protect both support chains. Controls reconstruct from the native save
  slot without migration.
- Optional SailInfo integration (inspected against **1.2.1**) reports actual
  mast-relative sheet angle without clamping the label. Keep scope limited to
  custom staysails and preserve settings/force readouts.

## Boat profiles and stays

Profiles author physical mast IDs, endpoints, active guides, prerequisites,
exclusions, ancestry and permanent mount IDs. Each boat's `Definition` owns its
data; resolve `Sections`, `Base` and `SheetCategory` through that profile.

| Boat                         | Part groups | Authored stays | Forward-masthead fallbacks |
| ---------------------------- | ----------: | -------------: | -------------------------: |
| Brig                         |           2 |             24 |                          4 |
| Junk                         |           2 |              9 |                          2 |
| Jong                         |           5 |              9 |                          0 |
| Sanbuq                       |           2 |             26 |                          5 |
| Cog                          |           1 |              3 |                          0 |
| Shroud                       |           2 |              8 |                          2 |
| Large dhow (Sailwind 0.39)   |           2 |             14 |                          8 |
| Gloriana (OldChronian 0.6.0) |           2 |              3 |                          1 |
| Chronian (OldChronian 0.6.0) |           2 |              2 |                          0 |
| Caelanor (OldChronian 0.6.0) |           1 |              1 |                          0 |
| Kakam (90)                   |           1 |              2 |                          0 |
| Dhow (10)                    |           1 |              1 |                          0 |

The twelve profiles provide **102** stays across **103** mast-pair
configurations. Fisherman support for OldChronian is profile-based with no hard
mod assembly dependency; Gloriana, Chronian and Caelanor have profiles. Exact
vectors, prerequisites and ordered control-source lists live in each profile,
backed by numeric installed-asset fixtures.

**Leopard has no Fisherman support:** its BoatRig profile was removed after
in-game compatibility problems. Profile gates reject Flying Sail fitting and
skip Fisherman's Stay registration on Leopard; no save migration is provided.
Future compatibility work is tracked in
[issue #33](https://github.com/sum-rock/MoreSailwindSails/issues/33).

Standard stay variants use **70°** between the aft spar's downward axis and
stay. If that intersects above the forward spar, attach to its physical masthead
at a steeper angle. Store endpoints rather than infer them at runtime, retain
physical fore/aft ordering and exclude higher aft sections from lower variants.
Chronian and Caelanor instead use the authored attachment lines below and remain
available beneath optional T’gallants.

`AlignGuideHeightToAftAnchor` moves the owned halyard guide along the aft mast
axis to the attachment height while preserving the native radial offset. Native
guides remain unchanged and must be active. Boat-specific checks own measured
endpoint/slope assertions; shared checks cover guide alignment and fitting.

Append groups/options after native and optional SE customization; never reorder
save slots. Reserve mount IDs **128–255** and expand capacity to **256** without
shrinking larger arrays. Missing old-save/cancellation entries restore **None**.
Keep unavailable variants in their original option slots; missing donors,
required masts or guides disable that variant rather than reject the boat.
Missing excluded parts impose no restriction. Validate geometry and occupied IDs
before construction; roll back a boat's new stays on failure. Protect occupied
stays/supports during invalid previews and restore preview state in a finalizer.
Normalize stay and walking geometry independently to the same endpoints; donor
bounds may differ from `mastHeight`.

### Kakam and Dhow

[Kakam.cs](../src/BoatRigs/Kakam.cs) and [Dhow.cs](../src/BoatRigs/Dhow.cs)
register one main/mizzen part group per boat:

| Boat       | Native name                      | Stay mount    | Physical mast pair | SE donor    |
| ---------- | -------------------------------- | ------------- | ------------------ | ----------- |
| Kakam (90) | `BOAT junk small singleroof(90)` | **128 / 129** | **5 → 7 / 6 → 7**  | **62 / 70** |
| Dhow (10)  | `BOAT dhow small (10)`           | **128**       | **6 or 7 → 51**    | **54**      |

Dhow's short and tall mainmasts share one stay position and menu option.
Registration groups configurations by mount ID; their labels must match. An
empty stay selects the fitted pair and updates its support, guide and control
references. An occupied stay retains its current pair during previews.

All four configurations use the native mizzen guide height and a **70°**
attachment line; donors need not be fitted. Bermuda mounts are staysail-only and
cannot provide physical support. Dhow's raked mast **52** is excluded because
its 70° line produces a negative head slope outside the fitting range.

Dhow sheets use mainmast sources **6/7/54** or mizzen source **51**; halyards
use the requested mast's own seats. Kakam uses the
[aft control placements](#kakam-aft-controls) below.

### Large dhow

[LargeDhow.cs](../src/BoatRigs/LargeDhow.cs) profiles native boat **30**
(Baghala). Fourteen stays cover adjacent mast pairs. Topmasts **3/5** require
bases **2/4** and retain lower-mainmast guides. Fore/main endpoints use the
rendered foremast tip, not the higher capsule tip, to avoid floating
attachments.

### Gloriana (OldChronian)

[Gloriana.cs](../src/BoatRigs/Gloriana.cs) profiles boat **182** from
OldChronian **0.6.0**. Mounts **128–130** cover fore/main and both mizzen
variants. The upper mizzen requires its lower section; the lower stay excludes
the upper section. Native stays provide templates without needing to be fitted.

Lower-mizzen halyards try **3 → 4 → 6**; upper mizzen tries **4 → 6**. These
seats share the permanent mizzen fife rail. Mainmast **2** and lower mizzen
**3** have [measured reef fallbacks](#gloriana-reef-fallbacks); sheet fallbacks
are null.

### Chronian (OldChronian)

[Chronian.cs](../src/BoatRigs/Chronian.cs) profiles boat **187** from
OldChronian **0.6.0**. Mount **128** joins Foremast Top **3** to Mainmast Top
**5** below the rendered wood tips. Mount **129** joins Mainmast **4** to
Mizzenmast Top **7** along Mizzen Royalstay **14**'s extended centerline and
excludes that stay, with reciprocal shipyard conflict help. Both use prototype
**12** and aligned aft guides. Optional T’gallants do not disable them; manual
fallbacks are null.

### Caelanor (OldChronian)

[Caelanor.cs](../src/BoatRigs/Caelanor.cs) profiles boat **192** from
OldChronian **0.6.0**. Mount **128** joins Foremast Top **2** to Mainmast Top
**6**, requiring **1/2/5/6**. It uses the aft mainmast position; the Midmast
alternative has no stay option. Endpoints sit below rendered wood tips,
excluding offset poles. Optional T’gallants do not move or disable the stay;
manual fallbacks are null.

### Stay collars on Sanbuq and Large Dhow

These profiles fit collars independently of the rope body, below the rendered
spar tips. Visible ropes end at collar surfaces; logical endpoints, sail fitting
and saved IDs remain unchanged. Sanbuq reuses native coils; Large Dhow separates
endpoint regions from combined meshes into owned copies.

Fit radius to rendered wood, not capsule bounds. Sanbuq's unreadable SE mizzen
topmast **80** uses the measured taper in its profile. Visual and walking
representations keep separate bounds/frames. Unexpected geometry triggers
registration rollback; teardown destroys only owned meshes.

## Winch placement

Shared controls serve Flying Sails, all staysail cuts and native sails on
Fisherman's Stays. The original placement redesign was accepted in game; later
cleanup and profile changes still need regression testing.

### Placement and ownership

Boat profiles define ordered native sources and measured fallbacks. Native
inventory discovers inactive rigs through the hierarchy, independent of the
startup `BoatRefs.masts` array. Separate sheet/halyard resolvers select seats; a
boat-owned ledger and coordinator manage reservations and clones.

- The forward physical mast selects the sheet category. Search complete
  left/right pairs in source and native-array order; skip unmatched entries and
  never pair centre sheets. Preserve native poses and asymmetry.
- Deduplicate shared/coincident seats while checking all aliases for occupancy.
  Bound native ropes block borrowing even when hidden, as do active renderers or
  colliders. Requested supports and mounting ancestors must be active.
- Claim sheet pairs atomically. Halyards use the same ledger; owned clones are
  excluded from native discovery. Native control suppression prevents mount and
  sail controls from competing for the same sail.
- Halyard groups map an exact requested mast to ordered sources. Unconfigured
  masts use their own reef arrays. Do not infer groups from ancestry, proximity
  or display names. Cog **57** uses **[57, 58]**; Cog **8** uses **[8, 51,
  65]**, with aliases preventing duplicate claims.
- Bootstrap templates follow the same source/fallback order but ignore occupancy
  and reserve nothing. Missing templates trigger registration rollback. Native
  startup must receive initialized control arrays.
- Clone controls inactive with owned handles/outlines. Prepare both replacement
  sheets before retiring old clones, preserve sail-owned controllers and update
  custom-mount arrays. Mount placement must preserve wheel-local input rotation.
- Retain valid placements and clones, including fallbacks when native seats
  become free. Native reclaim, lost support or changed source/pose invalidates
  the pair. Exhaustion hides controls, preserves controllers and retries every
  second; it must not remove stays or saved sails.

Native poses and authored fallbacks are trusted; there is no runtime clearance
search. All reservations are transient. During complete exhaustion, ropes can
still appear at the boat origin: this remains a known visual limitation.

### Controller lifetime during cleanup

Final `OwnedWinch.Dispose()` releases claims and destroys owned winches without
reparenting controllers, making teardown independent of sibling callback order.
Controllers under destroyed mounts disappear with them; controllers already
moved elsewhere remain untouched.

Live reconciliation uses `Retire()` to preserve sibling controllers and detach
the retiring control before disposal. Template adoption detaches before
replacing and rebinding a clone. Support loss, inactivity and exhaustion hide
controls for reuse; a missing owner triggers final disposal.

### Inventory lifetime and inspection counters

Each coordinator owns one inventory and mast/seat lookup per boat. Lifecycle
postfixes invalidate discovery after mast activation, part refresh and save
customization, ordered after SE. Array snapshots detect in-place changes; a
one-second sweep catches delayed or inactive parts. Occupancy and support checks
remain live, and pose changes rebuild alias lookup.

Alternative searches run on initial allocation, invalidation or exhaustion
retry. Empty coordinators skip discovery. Per-boat `HierarchyScans`,
`CandidateBuilds` and `EntriesCreated` counters measure inspection/allocation
activity, not frame time or total managed allocations.

### Halyards and Shroud belaying pins

`HalyardWinchPlacementResolver` uses the concrete requested active mast's
authored group, preserving native array order within each source. Flying Sails
request their mounting mast; staysail Mk.A/B/C request the aft base; native
sails on custom stays request the stay's aft halyard source. Topmast seats on an
active lower support retain their native association. An optional measured
fallback is tried after native sources exhaust; it is scoped to that exact
requested mast. There is no inferred alternative-mast search or generated
placement.

Shroud uses these same resolvers. Its short mainmast has **five** ordinary reef
entries and its tall mainmast **seven**. Each keeps its own ordinary array
first, then adds sources **16/17/24** from the mainmast's separate jib reef
coils. Foremast and mizzen groups likewise add only their associated stay coils.
This does not restore the former twelve-pin pool across physical variants.
Native reclaim and stable reservations follow the same rules as on every other
boat; capacity depends on the authored sources, their live mounting support and
occupancy.

### Gloriana reef fallbacks

[Gloriana.cs](../src/BoatRigs/Gloriana.cs) defines one fallback each for
mainmast **2** and lower mizzen **3**, from F9 captures on **2026-10-04** and
**2026-10-05**. Exact contacts, normals, mounting depths and templates live in
the profile. Native seats take priority for new allocations.

Fallbacks reserve `halyard-fallback/<requested mast>` for one owner. A valid
fallback remains stable when a native seat frees. Requested support, template
identity and pose must remain valid; support loss releases the claim and hides
controls for retry.

### Sanbuq reef fallbacks

[Sanbuq.cs](../src/BoatRigs/Sanbuq.cs) defines fallbacks for base masts
**10/11/55/69**, measured with F9 on **2026-10-07**. Each uses the requested
mast's `reefWinch[0]` after native sources exhaust. Contacts, normals and
mesh-derived mounting depths live in the profile. Topmasts keep their existing
sources. These mounting poses still need visual acceptance.

### Manual sheet fallbacks

Every category names a nullable port/starboard fallback pair. Native pairs take
priority on a new allocation. Each fallback side specifies its own contact
point, normal, mounting offset, compatible template and support requirements; a
captured surface point alone is not a finished mounting pose. Do not generate or
mirror fallback positions at runtime.

Authored pairs exist for **Shroud / ForemastFallback** (forward trim) and
[Kakam / MainmastFallback](#kakam-aft-controls) (aft trim). All other category
fallbacks are null. Future additions require labeled F9 captures and
visual/reachability confirmation. Missing vectors do not block native placement.

Null, incomplete or non-finite fallbacks are unavailable as a whole. When native
pairs exhaust, missing fallback data produces one contextual error per
definition and boat/category during that boat instance's lifetime. Retries
remain quiet. A valid fallback already reserved by another owner produces
ordinary exhaustion diagnostics. No default origin placement, partial pair,
generated strip or rotated mast search remains.

### Kakam aft controls

[Kakam.cs](../src/BoatRigs/Kakam.cs) uses **2026-10-07** F9 measurements.
Mainmast variants **5/6** use the authored aft sheet pair; an empty native
source list excludes forward seats. Mast **5** angle controls supply templates.
Contacts are symmetric and orientations parallel. Mizzen **7** tries its own
reef array before the measured halyard fallback. Exact vectors and mesh-derived
mounting depths live in the profile.

### Placement logging

Read `BepInEx/LogOutput.log`; paths are under
[Local investigation](#local-investigation).

| Message                                        | Meaning                                                                         |
| ---------------------------------------------- | ------------------------------------------------------------------------------- |
| `Placed native winch`                          | Initial successful placement; inspect `origin=native/fallback`.                 |
| `No free native winch pair/seat`               | Exhausted or unsupported; hidden controls retry.                                |
| `Missing or invalid sheet fallback`            | Required fallback is unrecorded or invalid; error is limited per boat/category. |
| `Unmatched native sheet pair`                  | Missing, null or unmatched native array entries were skipped.                   |
| `Winch reassigned after native/support change` | A previously valid placement became unavailable or moved.                       |
| `Winch successfully placed after retry`        | Placement recovered after an exhaustion or binding failure.                     |
| `Winch binding/placement failed`               | Clone/binding exception; claims release and controllers survive for retry.      |

Sheet messages identify boat and owner instances, forward mast, category,
selected source rig and array indices, pair identity, origin, both poses and
both templates. Halyard placement messages identify the requested active mast,
source rig and native array index. Exhaustion separates `nativeUnavailable`,
`reserved`, `missingSupports` and `fallbackBlocked`. Success is reported after
placement and showing both controls; routine refreshes and repeated failed
retries remain silent. Missing-data errors identify the fallback name and
missing/invalid side.

### Capturing proposed winch positions

With the boat loaded and menus closed, aim at bare mounting structure within
**10 m** and press **F9**. `LogFallbackWinchPlacement` displays a numbered
notification and logs the boat identity, boat-relative position/normal, collider
path/type and hit distance to `BepInEx/LogOutput.log`. Record the capture number
and intended role. Captures do not place winches or modify saves.

Configure the shortcut with the game closed in
`BepInEx/config/com.august.moresailwindsails.cfg`; `None` disables it:

```ini
[Diagnostics]
CaptureWinchPosition = F9
```

The read-only picker accounts for displaced walking models, transformed roots
and world obstructions. Captures describe collision surfaces, which may differ
from visible geometry. Confirm the surface against installed meshes or a
screenshot; orientation and mounting depth still need authoring. Preserve the
config key, numbering and log format.

### Viewing winch mounting points

Enable the optional overlay with the game closed in
`BepInEx/config/com.august.moresailwindsails.cfg`:

```ini
[Diagnostics]
EnableWinchMountOverlay = true
ToggleWinchMountOverlay = F8
```

It defaults off. With menus closed, aim at a boat within **10 m** and press
**F8**. The overlay follows that boat; press again to hide it before selecting
another. `None` disables the shortcut. File edits require normal BepInEx config
loading; they do not live-reload.

Wireframes show through the hull, including inactive rig variants:

| Color | Meaning                                                                           |
| ----- | --------------------------------------------------------------------------------- |
| Green | Unused native location on fitted supports                                         |
| Red   | Occupied by a native bound rope, active renderer/collider or visible custom winch |
| Cyan  | Unused native location on unfitted supports                                       |

Coincident locations use a **1 mm** tolerance; occupied aliases take priority.
Bound native ropes count even when hidden; hidden spare custom controls do not.
**Green does not guarantee allocation:** pairing, category, support and
reservation rules still apply. Use the overlay to find bare structure for
[F9 captures](#capturing-proposed-winch-positions), then confirm fallback
orientation, depth and reachability.

The overlay has a separate read-only inventory and reserves no seats. Discovery
refreshes once per second while shown; occupancy and transforms update at
rendering. Unreadable meshes and skinned renderers use approximate bounds. Menus
hide it; loading, losing the boat or disabling releases its resources. Reselect
after loading. Hidden overlays do not scan.

### Asset provenance and measurement fixtures

Use installed assets as the reference: native `level24`, SE's
`shipyard_expansion.assets`, Shattered Seas' `veil piercer`, and OldChronian's
boat bundles. Include import-parent transforms in boat-coordinate measurements.
Commit numeric fixtures only, never extracted meshes, textures or assemblies.

- `tests/GeometryChecks/FishermansStay/NativeWinchSeats.txt` records control
  identities, array correspondence, parent-local poses and shipyard groups.
- `tests/GeometryChecks/Controls/HalyardMounts.txt` records support eligibility.
- `tests/GeometryChecks/FishermansStay/NativeRigAvailability.txt` records native
  mast IDs and candidate stays.
- Earlier rail/surface fixtures remain references for manual fallback authoring.

Identify assets by serialized identity as well as hierarchy path: Brig source
**70** is a registered mainmast reef source, while **74** is an unregistered
foremast source with the same path. Measurements do not authorize generated
placement searches or establish in-game acceptance.

## Runtime validation

### Automated baseline and limits

The latest recorded **2026-10-12** Release builds passed with zero warnings or
errors, and both .NET suites passed against installed references and an isolated
game/BepInEx reference directory without plugins. Checks covered **58** native
Harmony targets and **29** native candidate stays. Optional SailInfo signatures
passed with installed references and were skipped in the isolated run.

Blender **5.1.1** checks passed for Flying Sail and all three staysail assets:
deterministic output, bake freshness and rejection of incompatible edits.
Subsequent C# cleanup did not change assets or generated data; Blender checks
were not rerun for that cleanup. Pinned CSharpier, Prettier and diff checks were
also recorded as passing.

GeometryChecks covers cuts, fitting, controls, profiles and placement policies;
AssemblyChecks covers native contracts, Harmony wiring, registration, ownership
and save structure. These checks do not execute Unity Cloth, rendering, audio,
interactive shipyard flows or live save restoration. Synthetic placement
counters and static collar audits are not runtime performance or visual
acceptance.

The experimental sail family has been removed from this branch. Saves containing
its prefabs are outside supported runtime validation. Remaining 0.3.0 checks
include baked Flying Sail/Mk.B/Mk.C appearance, SE texture persistence, native
paging/scaling, profile changes, rope rendering and the simplified F7 toggle.

### Optional Shipyard Expansion

SE is optional in **0.3.0-dev**. The plugin has no SE assembly reference; its
soft dependency preserves load order. Native `Sail.LoadScale` supplies new-sail
defaults (50% staysails, 100% Flying Sail) and save restoration. SE can patch
the same APIs when installed; MSS adds no scaler or replacement save format.

`ShipyardExpansionTexturesPatch` discovers the loaded
`SailTextureChanger.UpdateMaterial` through reflection. For either Fisherman
family, it replaces the instance's allowed textures with SE's first blank-white
entry and selects it before material application. This covers cycling and saved
texture restoration, normalizing older atlas choices to white while preserving
recoloring. Native defaults use `Texture2D.whiteTexture`. Donor choices and the
global SE catalog remain unchanged.

Missing SE is skipped. Patch/API failures warn once and allow SE's own behavior
to continue. Installed-target inspection and automated checks pass, but live
texture persistence is unconfirmed: on **2026-10-12**, the user reported white
sails reverting to the donor atlas on shipyard exit and during texture cycling.
The subsequent patch has no recorded in-game acceptance.

Missing SE-only rigs retain unavailable option slots so later saved selections
do not shift. The installation guard rejects those entries. A native Sailwind
**0.39** asset audit identified 29 candidate stays: Brig 6, Junk 4, Jong 3,
Sanbuq 2 and Baghala 14. Cog, Kakam and Dhow's current stays require SE-added
parts. Mod-added boats still require their providing mods. Asset availability
does not establish live fitting.

MSS expands native mast selectors to at least 256 and pages parts beyond the
native eight rows. Full arrays retain option indices and order processing.
Paging follows boat/category changes and is skipped when SE provides navigation.
Native scaling, partial-profile registration and paging still need game checks.

Validate native+MSS and SE+MSS separately. Removing SE from an SE-equipped save
is outside the supported migration scope: part selections are positional and SE
stores additional sail state. Keep each test save's mod configuration stable.

### Confirmed game observations

These observations cover specific configurations, not all boats or later
changes.

- **2026-10-12:** the user accepted Mk.A's baked shape and initial white
  appearance, and confirmed removal of rotation, flip and forced-uniform scaling
  restrictions. Boat/cut and save/reload coverage were not fully specified. This
  does not validate the later SE texture fix or native scaling/paging.
- **2026-10-04:** an MSS 0.2.1 / SE 0.11.1 to MSS 0.2.2 / SE 0.12.1 upgrade
  succeeded for one reported save; 0.3.0 compatibility remains unconfirmed.
- **2026-09-28:** Cog mast 57 placement/routing worked with midstay2 absent and
  the gaff occupying the primary reef winch. This does not cover later mast 8,
  reclaim or reload changes. The overlay also worked; its later red occupied
  color has no recorded visual confirmation.
- **2026-09-27:** the user accepted the native placement redesign on Brig/Jong.
  Inspected logs showed no exhaustion/binding failures and installed/local DLL
  hashes matched. This predates the architecture cleanup.

### Runtime checklist

Start on **Brig**, then the affected boats, in separate native+MSS and SE+MSS
configurations. Verify Flying Sail and all staysail cuts alongside native sails.
Load pre-change saves with resized/recolored sails and check another
save/reload.

For 0.3.0, cover menu navigation beyond eight part groups, category/boat
changes, valid options after unavailable entries, and rejection of unavailable
orders. Check initial sizes, resizing, white-only SE texture cycling and
persistence through order completion, re-entry and reload, including older atlas
selections. Check original donor sails and SE's own pages for regressions.
Validate order text with NANDFixes enabled/disabled, invalid staysail fitting
and wind shielding with one sail behind another.

For Flying Sail ropes, compare shading/thickness against native ropes on Brig
and Sanbuq. Toggle 3D ropes both ways and check knots, shared spans, resize,
tacking, strike, support loss/recovery, preview/cancel and reload. Check the F7
profiling toggle and overlay colors separately.

For Kakam and Dhow, check registration, collars, controls, preview/cancel and
save/reload on both supported mainmast variants. Verify Dhow's single stay
option follows an empty mast-pair change and protects an occupied pair. Check
Kakam's aft sheet pair and mizzen halyard fallback, plus Sanbuq's four new
halyard fallbacks, for seating, reachability and rope routing. These changes
have no recorded visual acceptance.

For OldChronian, apply the common checklist below plus these profile-specific
checks. Current profile and collider revisions have no recorded runtime
acceptance:

- **Gloriana:** cover both mizzen variants, native-stay removal, collar
  appearance, free auxiliary mizzen halyard seats, and both measured reef
  fallbacks' seating, reachability and operation.
- **Chronian:** check both authored stays, optional T’gallants and reciprocal
  red conflict help for mount 129 versus Mizzen Royalstay 14.
- **Caelanor:** use foremast/aft-mainmast with and without optional T’gallants;
  verify occupied controls and absence of a stay for the Midmast alternative.

1. Fit and resize all affected cuts at several heights, with mixed native/custom
   sails. Check collision completion, trim stops, recoloring and reopening
   documents.
2. Exercise deployment, partial reversals and full lowering/hoisting
   transitions. Check gathering, rope continuity and staysail fixed-head
   behavior.
3. Tack at full/partial deployment and both trim limits. Inspect inversion,
   detachment, force/audio discontinuities, collar seating and sheet tension.
4. Test native winch priority, complete-pair exhaustion/recovery, native
   reclaim, and controller retention. Cover both Cog mizzen variants and
   Shroud's separate fitted support groups when changing allocation code. Check
   fallback reachability where an authored fallback exists.
5. Preview/cancel mast changes, reject occupied-support removal, refit, remove,
   save/reload and unload the boat. Check full mast lists, control bindings and
   native/custom sail behavior. Do not claim persistence or teardown correctness
   from structural checks alone.
6. For visual/diagnostic changes, verify optional SailInfo modes, knots,
   collars, overlay occupancy/colors and F9 captures in game. Measure
   counters/frame time before claiming a runtime performance improvement.

### Placement issue references

- [#16](https://github.com/sum-rock/MoreSailwindSails/issues/16): Jong foremast
  staysail port sheet placement.
- [#21](https://github.com/sum-rock/MoreSailwindSails/issues/21): broader winch
  placement and capacity investigation.

## Local investigation

For placement diagnostics, see [Placement logging](#placement-logging),
[Capturing proposed winch positions](#capturing-proposed-winch-positions) and
[Viewing winch mounting points](#viewing-winch-mounting-points).

### Performance profiling

The profiler in `src/Utils/Profiling/` measures sail CPU work and whole-game
frame statistics. Enable it with the game closed in
`BepInEx/config/com.august.moresailwindsails.cfg`:

```ini
[Diagnostics]
EnableProfiling = true
ToggleProfiling = F7
ProfileTargets = All
```

Captures start OFF; **F7** toggles NORMAL/OFF. Configuration changes stop
capture. Targets are `Rig`, `Frame`, `Shape`, `Aerodynamics` and `Ropes`; `All`
selects every category, while an empty value records only frame statistics.
Unknown names warn and are ignored.

`[PerformanceProfile]` reports ten-second windows and partial windows at capture
transitions. CPU rows show calls, milliseconds per frame and maximum duration.
Timings include nested work: do not add them together. They exclude Unity Cloth
and GPU completion. FPS, frame times and GC counts describe the whole game.

Long tick intervals correlate collections with hitches; they do not measure GC
pause duration. The installed Mono runtime lacks scoped allocation accounting
and reports `allocations=unavailable`. Warmed collection uses reusable buffers;
reporting/configuration parsing allocate outside timed scopes. Automated checks
cover accounting and transitions; the simplified F7 toggle needs a game check.

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

For manual live monitoring, maintainers can run `./scripts/tail-player-log.sh`
and `./scripts/tail-bepinex-log.sh` from the repository root in separate
terminals. Each displays 5,000 lines and follows the default Steam log with
`tail -F`; Ctrl+C stops it. Agents must not execute these scripts.

Capture logs before restarting a freeze. Distinguish other mods' exceptions and
suspected causes from confirmed evidence. Prefer `rg`/`rg --files`; inspect
screenshots with the local image viewer. Temporary IL/asset inspectors under
`/tmp` are disposable, not repository dependencies. Verify their code, assembly
paths and cache freshness before reuse; inspect installed assemblies/assets as
the behavioral reference rather than assuming upstream source matches.
