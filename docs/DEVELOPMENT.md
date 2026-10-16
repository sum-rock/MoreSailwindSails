# MoreSailwindSails development

See [README.md](../README.md) for features and player instructions and
[AGENTS.md](../AGENTS.md) for agent workflow. This guide owns technical
reference, verification procedures and
[validation records](#validation-records); resolved history is in Git.

## Technical reference

| Area                                     | Reference                                                           |
| ---------------------------------------- | ------------------------------------------------------------------- |
| Plugin identity, versions and prefab IDs | [Release and manual installation](#release-and-manual-installation) |
| Directories, namespaces and test layout  | [Source organization](#source-organization)                         |
| Cloth, templates, controls and Harmony   | [Runtime safeguards](#runtime-safeguards)                           |
| Sail-family mechanics                    | [Flying Sail](#flying-sail), [Staysails](#staysails)                |
| Authored stays and boat support          | [Boat profiles and stays](#boat-profiles-and-stays)                 |
| Native seats and manual fallback pairs   | [Winch placement](#winch-placement)                                 |
| Automated checks and observations        | [Validation records](#validation-records)                           |
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
`.config/dotnet-tools.json` pinned. The flake also provides Python 3, ILSpy CLI
and pins Prettier for Markdown and Ruff for Python formatting. Run
`pre-commit install` inside the development shell to enable the CSharpier and
Markdown/Python formatting hooks, which invoke Nix on later commits. Hooks
format staged files; review and stage any formatting changes before retrying the
commit.

After restoring, run from the repository root:

```sh
nix develop -c prettier --check "**/*.md"
nix develop -c ruff format --check .
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

For Python, run `nix develop -c ruff format .` to format or add `--check` for
read-only verification. The `ruff-format` commit hook formats staged Python
files using Ruff's defaults; it does not execute them or run lint fixes.
Exercise it across tracked Python files with
`nix develop -c pre-commit run ruff-format --all-files`.

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
physics or the live shipyard.

## Release and manual installation

Release version is **0.3.0** in `Plugin.PluginVersion` and the project
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

After installation, confirm `MoreSailwindSails 0.3.0 loaded!` and registration
of the intended prefabs in `BepInEx/LogOutput.log`. Compare installed and built
DLL hashes when diagnosing; they are separate files.

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
| `src/BoatConfiguration/`          | Shared boat identity and physical mast ancestry                           |
| `src/Controls/`                   | Native seat discovery, allocation, reservations and owned controls        |
| `src/Compatibility/`              | Shared order text and optional SE/SailInfo integration                    |
| `src/Visuals/`                    | Shared routed rope rendering                                              |
| `src/Utils/`                      | Diagnostics and profiling                                                 |

`BoatConfiguration/` owns shared boat identity and mast ancestry.
`BoatDefinition.cs` and `BoatCatalog.cs` live at its top level; the twelve
per-boat files live under `BoatConfiguration/Boats/`, with matching namespaces.
Per-boat configuration and its definition types live with their consumers:

- `Stays/FishermansStay/Configuration/`: ordered stay groups, endpoints,
  prerequisites, permanent mount IDs and attachment measurements.
- `Sails/FishermansFlyingSail/Configuration/`: ordered physical support pairs.
- `Controls/Configuration/`: measured sheet pairs and mast-specific halyard
  fallback seats for sails on Fisherman's Stays.

Each feature configuration references its shared `BoatDefinition` through
`Boat`. Each owner has its own catalog; a shared boat entry does not enable any
feature. Catalogs share exact boat-name matching and trailing `(Clone)`
normalization. Keep one file per boat inside each configuration directory’s
`Boats/` subdirectory, with matching `.Configuration.Boats` namespaces. Catalogs
and definition types stay directly in `Configuration/`. New sail families own
only the configuration they need; fisherman’s staysails bind to registered stays
and use shared mast ancestry for the shared-luff collision exemption without
another per-boat profile. Preserve authored ordering, IDs and values when moving
configuration, since stay option positions are saved.

Native integration belongs in each feature's `*SailwindPatches.cs`; staysail
angle reporting uses `StaysailSailInfoPatches.cs`. Registration follows optional
SE initialization and precedes All Sails inventory caching.

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
- Keep custom sails in native control binding and height ordering. Flying Sails
  use their physical mast arrays at `mastOrder`; staysails use the registered
  Fisherman's Stay arrays. Routing postfixes restore custom guides and travel.
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

Geometry and ownership checks pass.

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

The native shipyard checker ignores sail contacts from a staysail's side but can
still count the same contact from a square sail's side. The staysail integration
filters that incoming square-sail contact when the other checker belongs to a
Fisherman's Staysail (Mk.A/B/C) on the same boat and its forward physical mast.
Base and topmast sections share this exemption; root and child checker colliders
both resolve through their registered sail. This exempts the pair's sail-to-sail
contact, rather than changing the luff or collision strips. Sails on the
aft/other masts, other sail families, unknown supports and solid obstructions
retain their native checks. No existing collision counters or saved fitting data
are rewritten.

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
- Bind the reef controller through the stay mount's allocated winch. Route its
  halyard through active aft guides (with **5 cm** separation) and a leaf below
  the top-aft bone. Keep independent lower sheets; no fore halyard or decorative
  upper sheets. Bind directly to the stay's resolved endpoints and guide;
  availability comes from the active mount and the stay's existing prerequisite
  and guide checks. Rigging does not resolve or recheck mast ancestry. Removal
  and preview guards protect the mount and the stay's required parts, retaining
  the current binding during removal previews. Controls reconstruct from the
  native save slot without migration.
- Optional SailInfo integration (inspected against **1.2.1**) reports actual
  mast-relative sheet angle without clamping the label. Keep scope limited to
  custom staysails and preserve settings/force readouts.

## Boat profiles and stays

Shared boat definitions author physical mast ancestry and provide `Sections` and
`Base`. `FishermansStayDefinition` owns endpoints, active guides, prerequisites,
exclusions and permanent mount IDs; `FlyingSailDefinition` owns support pairs.
`BoatControlDefinition` provides authored sheet pairs and halyard fallbacks.
Each boat's feature configuration references its shared boat definition.

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
vectors, prerequisites and measured fallback positions live in each profile,
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
The baked rope follows the same authored endpoints in the visual and walking
models; native donor mesh bounds do not participate in fitting.

### Kakam and Dhow

[KakamStays.cs](../src/Stays/FishermansStay/Configuration/Boats/KakamStays.cs)
and [DhowStays.cs](../src/Stays/FishermansStay/Configuration/Boats/DhowStays.cs)
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

Dhow and Kakam discover donor stays through the aft physical mast's dependency
paths. Kakam also retains the [aft fallback placements](#kakam-aft-controls).

### Large dhow

[LargeDhowStays.cs](../src/Stays/FishermansStay/Configuration/Boats/LargeDhowStays.cs)
profiles native boat **30** (Baghala). Fourteen stays cover adjacent mast pairs.
Topmasts **3/5** require bases **2/4** and retain lower-mainmast guides.
Fore/main endpoints use the rendered foremast tip, not the higher capsule tip,
to avoid floating attachments.

### Gloriana (OldChronian)

[GlorianaStays.cs](../src/Stays/FishermansStay/Configuration/Boats/GlorianaStays.cs)
profiles boat **182** from OldChronian **0.6.0**. Mounts **128–130** cover
fore/main and both mizzen variants. The upper mizzen requires its lower section;
the lower stay excludes the upper section. Native stays provide templates
without needing to be fitted.

Native stay **6** depends on mainmast **2** and upper mizzen **4**, which
requires lower mizzen **3**. It can supply controls to either custom mast pair
when sail-less; the shared claim ledger prevents conflicts. Mainmast **2** and
lower mizzen **3** retain [measured reef fallbacks](#gloriana-reef-fallbacks).
There are no authored sheet fallbacks.

### Chronian (OldChronian)

[ChronianStays.cs](../src/Stays/FishermansStay/Configuration/Boats/ChronianStays.cs)
profiles boat **187** from OldChronian **0.6.0**. Mount **128** joins Foremast
Top **3** to Mainmast Top **5** below the rendered wood tips. Mount **129**
joins Mainmast **4** to Mizzenmast Top **7** along Mizzen Royalstay **14**'s
extended centerline and excludes that stay, with reciprocal shipyard conflict
help. Both use prototype **12** and aligned aft guides. Optional T’gallants do
not disable them; manual fallbacks are null.

### Caelanor (OldChronian)

[CaelanorStays.cs](../src/Stays/FishermansStay/Configuration/Boats/CaelanorStays.cs)
profiles boat **192** from OldChronian **0.6.0**. Mount **128** joins Foremast
Top **2** to Mainmast Top **6**, requiring **1/2/5/6**. It uses the aft mainmast
position; the Midmast alternative has no stay option. Endpoints sit below
rendered wood tips, excluding offset poles. Optional T’gallants do not move or
disable the stay. When native positions exhaust, the mainmast **5** has a reef
fallback from F9 capture **#5** at `(0.204692, 1.884204, -1.520411)`, normal
`(0.965926, 0.018054, 0.258188)`. It clones `reefWinch[0]`
(`halyard_main_main1`); the installed coiled-rope mesh rear bound **-0.130912
m** at scale **0.60000134** supplies a **0.07854738 m** mounting offset.

The latest F9 captures **#1/#2** on `CAE_trim` replace the earlier sheet
contacts and supply **MainmastFallback**. Average the absolute X positions and
the common Y/Z values to place the contacts at
`(±2.4498375, 2.416566, -4.761331)`. Port/starboard normals are
`(±0.0654635, 0.995183, 0.072977)`, with positive normal X on port (negative
contact X). Both sides use the existing shared sheet template and placement
calculation. These are authored fallback data only, retaining native priority,
aft eligibility and reservations. The new contacts lie aft of mainmast **5**'s
installed boat-local origin (**Z ≈ -2.306270**); the previous contacts at **Z =
-2.096566** were excluded by the aft-only rule.

### Authored stay geometry and bake

[assets/stays/stay.blend](../assets/stays/stay.blend) provides a straight rope
and two matching metal bands used by every Fisherman's Stay. Runtime
construction creates one decorative rope and two instances of the baked collar.
Each stay owns its two meshes and releases them during teardown or registration
rollback.

The rope is **1 m** long and **0.02 m** in diameter. Each band has a **0.42 m**
outside diameter, approximately **0.4074 m** inside diameter, **0.10 m** axial
height and **0.0063 m** radial wall thickness. Collar radii describe the mesh's
vertex rings. Preserve the named `Rope`, `ForestayCollar` and `AftstayCollar`
objects at one metre per Blender unit. The rope runs along object-local X;
collars surround object-local Z. Camera and light objects are ignored.

From the repository root with Blender **5.1.1**:

```sh
blender --background --factory-startup --disable-autoexec assets/stays/stay.blend --python-exit-code 1 --python tools/bake_stay.py
blender --background --factory-startup --disable-autoexec assets/stays/stay.blend --python-exit-code 1 --python tools/bake_stay.py -- --check
blender --background --factory-startup --disable-autoexec assets/stays/stay.blend --python-exit-code 1 --python tests/BlenderChecks/check_stay_bake.py
```

The baker never saves the asset. It evaluates enabled collar Solidify modifiers,
bakes positive object scales and discards presentation translation/rotation.
Other modifiers, parents, constraints, animation and shape keys are unsupported.
The existing flat sail baker is unsuitable for these closed meshes, unapplied
scales and modifiers, so the stay has a separate exporter.

Canonical rope positions map scaled `(X, Y, Z)` to `(Y, -Z, X)`, centered
transversely with length along negative Z and the aft end at zero. Collars map
to `(X, -Y, Z)`, centered around their openings. Both maps reverse handedness;
triangle winding is reversed and normals use inverse scale before conversion.
Triangulation preserves UV seams and authored corner normals, including the
current flat shading. Both collars must export identical data; the output
contains one reusable collar. Materials are not exported by Blender.

`FishermansStayBakedMesh.g.cs` supplies `CreateRope()` and `CreateCollar()` with
fresh position, normal, UV and index arrays, plus measured rope length/radius
and collar inner/outer radii and half-height. Output is deterministic and
requires no Blender installation or asset bundle to build the plugin. Append
`-- --output /tmp/stay.g.cs` for a separate output. `--check` compares without
writing, including when output is missing or stale. Check in generated data
alongside authoring changes.

### Stay placement and native materials

The per-boat stay definitions' `ForePoint` and `AftPoint` remain the exact rope
endpoints. Only rope length scales; its authored diameter remains unchanged.
Each band follows its supporting mast's capsule axis, but uses its authored
`ForeCollarRadius` or `AftCollarRadius` instead of the oversized collision
radius. `ForeCollarOffset` and `AftCollarOffset` seat bands on the visible shaft
without moving the rope endpoints or sail guides. The rope extends to the
authored endpoints, including the portions inside masts.

Collar measurements come from the installed native and boat-mod meshes in mast
local space. Circular shaft sections identify the center and upper shaft extent;
the nearest surface around that center excludes detached rigging and adjacent
spars in the same mesh. Radii at the seated band centers include a 1 mm overlap.
Masthead bands sit at least one band half-height below the shaft tip; Gloriana's
forward band sits below its square cap. These remain circular, cylindrical
bands, so taper and irregular cross-sections are approximate fits. Runtime
placement only applies the authored radius and offset, retaining the band's
axial height. There is no runtime mesh scan, native collar extraction or
boat-specific fitting path. Changing band height requires reviewing the authored
masthead offsets.

The mount remains at the aft endpoint with negative Z toward the forward mast.
Boat up supplies the roll reference. An empty walking root maps through
`BoatRefs.boatModel` into `BoatRefs.walkCol` and supplies `Mast.walkColMast` and
`BoatPartOption.walkColObject`. Native sail fitting parents its collision
checker to this frame. Installed Dhow and Brig models have a rotated import
frame; using the rigidbody root directly would misplace that checker.

The stay creates no colliders or walking mesh copies. Its rope and bands do not
block players/items or restrict other sails' shipyard clearance. Physical mast
capsules still supply cloth collision and collar axes; sail collision checkers,
wind-shadow colliders and winch interaction colliders remain separate.

The rope reuses the configured native stay's root renderer material. Bands reuse
`metal2`, inspected on native lantern **114** (`114 lantern M`, `level24` object
**10909**): its root renderer references material **87** in
`sharedassets1.assets`. This material has no separate texture map. Lookup caches
the loaded shared material by its exact name; no native materials are modified
or destroyed. Missing materials use the existing registration rollback. Native
stay donors still supply controls, sheet anchors, price, mass and starting sail
color. Mount IDs, option order, support prerequisites and saved sail attachments
remain unchanged.

## Winch placement

Flying Sails use native physical-mast binding. All sails on Fisherman's Stays
use mount-owned cloned controls, with runtime placement claims shared per boat.

### Placement and ownership

- Native `Mast.UpdateControllerAttachments` sorts all sails by height and binds
  Flying Sail reef/left/right controllers at `mastOrder`. Adding or moving a
  sail can reassign controls. No custom source search, fallback, capacity audit
  or shortage handling applies to Flying Sails; record actual shortages in game.
- A Fisherman's Stay remains the saved mounting `Mast`, with one sail at order
  zero. Its reef/left/right arrays expose the owned clones for custom or vanilla
  sails. The sail rigs retain custom guides, geometry and travel limits, without
  owning another set of winches or filtering the native sail list.
- Resolve the stay's aft section to its physical base mast. Enumerate
  `BoatCustomParts.availableParts` and each `partOptions` array in registration
  order, including inactive options. Donors must have `Mast.onlyStaysails`, be
  outside our stay registry and carry no sails. Follow `requires` transitively
  to the aft physical mast, tracking visited options to terminate cycles.
- Within each eligible stay, inspect arrays in order. Sheets require complete
  left/right entries at the same index; centre controls are not candidates.
  Reefing controls are selected separately. A donor need not be installed, but
  each control's mounting parent, containing mast/option and the requested mast
  must be active in the hierarchy. An active mast or stay implies active
  supports, both at sea and during shipyard previews; do not traverse authored
  mast ancestry to recheck activity.
- Bound ropes block borrowing even when hidden, as do active native renderers or
  colliders. Check shared references and all coincident aliases. Claim sheet
  pairs atomically and reefing positions individually; reference identity and
  positions within **1 mm** conflict, including authored fallback positions.
- Reefing falls back only to the authored seat for that aft physical mast.
  Sheets try complete authored pairs moving aft in boat coordinates (**-Z**).
  Both contacts must be at or aft of the mast; their forwardmost contact orders
  pairs along that axis. Skip unavailable templates, supports, occupied seats or
  claims and continue aft. Preserve both authored poses; do not mirror, move or
  rank them by distance. Native positions precede fallbacks for new allocations.
- Startup clones use the configured native stay donor's sheet assets and aft
  section's reef asset without claiming a placement. Native startup receives
  initialized mount arrays even when no placement is available.
- Retain valid placements, including fallbacks when native positions free. Donor
  sails, changed dependencies/bindings, lost support or changed poses invalidate
  borrowed positions. Prepare replacement pairs before retiring old clones;
  update mount arrays and preserve sail controllers. Exhaustion hides affected
  controls, releases claims and retries every second, preserving the installed
  sail and its saved mount identity.

Native poses and measured fallbacks are trusted; there is no clearance search.
Claims are transient and do not affect save identities. During complete
exhaustion, ropes can still appear at the boat origin: this remains a known
visual limitation.

### Dependency and mounting audit

The **2026-10-15** read-only audit used installed native `level24` and local mod
bundles under `BepInEx/bkp/` (currently disabled): SE's
`shipyard_expansion.assets`, Shattered Seas' `veil piercer`, and OldChronian's
`gloriana`, `aelasyl` and `caelanor`. The decompiled native assembly hash
matched the installed `Assembly-CSharp.dll`. No game files or saves were
changed.

Registered native stays and the modded boat options expose separate control
references whose mounting parents can remain present when the source stay is
inactive. Check the actual mounting objects with `activeInHierarchy`, which
already includes Unity parent activity. Shared identities in Cog's stays
**51/65** and shared positions across mast pairs require the same occupancy
checks and ledger as different variants.

OldChronian's dependencies identify both endpoint groups and transitively
include lower sections: Gloriana stay **6 → 2,4 → 3**, Chronian stay **12 → 3,5
→ 2,4**, and Caelanor stay **14 → 6 → 5**. SE adds native part references from
`SE_PartOptionData.requiresVanilla1/2` during `AssetTools` initialization, so
discovery reads live registered options after mod setup and refreshes when
rigging changes; serialized SE dependencies alone are incomplete.

Shroud's **24** requires tall fore/main masts **5/7** and **25** requires tall
main/mizzen masts **7/9**. Its default stays **16/17/18/19** have empty
`requires` lists, and the inspected Shattered Seas assembly does not populate
those lists. They are excluded by the specified dependency rule. Short main
**8** and mizzen **10** therefore have no matching donor stays in this bundle;
controls without an eligible authored fallback remain unavailable. No guessed
endpoint or hardcoded source exception was added.

Sources can now belong to either endpoint of a native stay associated with the
aft mast. This changes the former forward-mast sheet categories. The audit
records references and dependency paths.

### Controller lifetime during cleanup

Final `OwnedWinch.Dispose()` releases claims and destroys owned winches without
reparenting controllers, making teardown independent of sibling callback order.
Controllers under destroyed mounts disappear with them; controllers already
moved elsewhere remain untouched.

Broken-control cleanup uses `Retire()` to preserve sibling controllers and
detach the retiring control before disposal. Template adoption detaches before
replacing and rebinding a clone. Support loss, inactivity and exhaustion hide
controls for reuse; a missing owner triggers final disposal. Native binding
reparents controllers beneath the winch, so the binding invalidation hook also
detaches them from hidden placement mounts and requests a retry. This preserves
controller activity when native binding runs again during exhaustion.

The coordinator reuses separate control and group snapshot lists during each
tick. It captures groups again between release and refresh so each pass sees
current membership, while callbacks can safely mutate the source lists. A
`finally` clears both buffers, retaining capacity without retaining removed
objects. Final teardown uses its own independent snapshot.

### Inventory lifetime and inspection counters

Each coordinator owns one inventory and mast/seat lookup per boat. Lifecycle
postfixes invalidate discovery after mast activation, native control binding,
part refresh and save customization, ordered after SE. Array snapshots detect
in-place changes; a one-second sweep catches delayed or inactive parts.
Occupancy and support checks remain live, and pose changes rebuild alias lookup.

Alternative placement searches run on initial allocation, invalidation or
exhaustion retry. Retained placements check only the selected donor's live
dependencies and sails, using the same eligibility predicate as discovery. A
reference scan preserves membership in the current option inventory without
traversing unrelated donors. Seat occupancy, source arrays, support and pose
checks remain live; dependency results are not cached. Empty coordinators skip
discovery. Per-boat `HierarchyScans`, `CandidateBuilds` and `EntriesCreated`
counters measure inspection/allocation activity, not frame time or total managed
allocations.

### Gloriana reef fallbacks

[GlorianaControls.cs](../src/Controls/Configuration/Boats/GlorianaControls.cs)
defines one fallback each for mainmast **2** and lower mizzen **3**, from F9
captures on **2026-10-04** and **2026-10-05**. Exact contacts, normals, mounting
depths and templates live in the profile. Native seats take priority for new
allocations.

Fallbacks reserve `halyard-fallback/<requested mast>` for one owner. A valid
fallback remains stable when a native seat frees. Requested support, template
identity and pose must remain valid; support loss releases the claim and hides
controls for retry.

### Sanbuq reef fallbacks

[SanbuqControls.cs](../src/Controls/Configuration/Boats/SanbuqControls.cs)
defines fallbacks for base masts **10/11/55/69**, measured with F9 on
**2026-10-07**. Each uses the requested mast's `reefWinch[0]` after native
sources exhaust. Contacts, normals and mesh-derived mounting depths live in the
profile. Requests from topmast stays resolve to their physical base.

### Manual sheet fallbacks

Each authored fallback side specifies only its contact point, surface normal and
support requirements. Both sides on every boat use one shared native sheet
model: **Kakam mast 5, left angle winch 0**. Discovery includes inactive boats
and masts, caches the donor while it remains usable, and retries at most once
per second when it is missing or destroyed. Discovery never activates, binds or
claims the donor; a bound native rope does not prevent cloning it.

`SheetFallbackTemplate` owns the installed model's fixed boat-relative reference
rotation, unit size and **0.044704 m** mounting depth. The model's local +Z is
aligned to the authored surface normal; placement does not inherit donor wheel
input or boat motion. The contact is offset along that normal by the shared
depth. Shroud therefore retains its captured contacts but changes from its old
model/depth to the standard Kakam size and mounting frame.

Null, incomplete or non-finite pairs are unavailable as a whole. Fallback
occupancy and claims use the same native-priority rules as borrowed positions;
exhaustion is reported once until recovery. A missing shared donor leaves
fallback controls unavailable through the existing hide/retry path. Native
allocations and halyard templates are unaffected; no saved placement data or
migration is introduced.

Authored pairs are **Shroud / ForemastFallback**,
[Kakam / MainmastFallback](#kakam-aft-controls), and
[Caelanor / MainmastFallback](#caelanor-oldchronian). Their names preserve
diagnostic identity and no longer select a forward-mast category. Eligibility
follows their positions aft of the requested mast. All other boats have no
authored sheet pair. Future additions require labeled F9 captures.

### Kakam aft controls

[KakamControls.cs](../src/Controls/Configuration/Boats/KakamControls.cs) retains
its **2026-10-07** F9 measurements. The shared sheet template supplies the
symmetric, parallel aft pair. The pair can serve a stay whose aft mast lies
forward of both contacts after native stay positions exhaust. Mizzen **7**
retains its measured reef fallback. Kakam's captured contacts and mounting
depths are unchanged.

### Placement logging

Read `BepInEx/LogOutput.log`; paths are under
[Local investigation](#local-investigation).

| Message                                        | Meaning                                                                    |
| ---------------------------------------------- | -------------------------------------------------------------------------- |
| `Placed native winch`                          | Initial successful placement; inspect `origin=native/fallback`.            |
| `No free native winch pair/seat`               | Exhausted or unsupported; hidden controls retry.                           |
| `Unmatched native sheet pair`                  | Missing, null or unmatched native array entries were skipped.              |
| `Winch reassigned after native/support change` | A previously valid placement became unavailable or moved.                  |
| `Winch successfully placed after retry`        | Placement recovered after an exhaustion or binding failure.                |
| `Winch binding/placement failed`               | Clone/binding exception; claims release and controllers survive for retry. |

Sheet messages identify boat and owner instances, forward and requested aft
masts, selected source rig and array index or fallback name, pair identity,
origin, both poses and both templates. Halyard placement messages identify the
requested active mast, source rig and native array index. Exhaustion separates
`nativeUnavailable`, `reserved`, `missingSupports` and `fallbackBlocked`.
Success is reported after placement and showing both controls; routine refreshes
and repeated failed retries remain silent. Missing or invalid fallback pairs
remain unavailable.

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
**Green does not guarantee allocation:** dependency, pairing, support and
reservation rules still apply. Use the overlay to find bare structure for
[F9 captures](#capturing-proposed-winch-positions).

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
- `tests/GeometryChecks/Controls/HalyardMounts.txt` records the historical
  source association audit; it no longer defines runtime discovery.
- `tests/GeometryChecks/FishermansStay/NativeRigAvailability.txt` records native
  mast IDs and candidate stays.
- Earlier rail/surface fixtures remain references for manual fallback authoring.

Identify assets by serialized identity as well as hierarchy path: Brig source
**70** is a registered mainmast reef source, while **74** is an unregistered
foremast source with the same path. Measurements do not authorize generated
placement searches.

## Validation records

### Automated baseline and limits

The **2026-10-16** stay winch update retains plugin/runtime version **0.3.0**.
Release built with zero warnings/errors; GeometryChecks and AssemblyChecks
passed. Executed snapshot checks cover source mutation, fresh captures, empty
captures and **0 allocated bytes across 1,000 warmed copies** on the .NET test
host. Structural checks cover separate release/refresh snapshots, cleanup,
selected-donor inventory membership, shared live eligibility and retained seat
validation. These checks do not measure Unity frame time or Mono allocations.

The **2026-10-16** authored stay integration retains plugin/runtime version
**0.3.0**. Release built with zero warnings/errors; GeometryChecks passed,
including baked rope endpoints across **103** configurations, fixed rope
cross-section and authored collar sizing. AssemblyChecks passed, including baked
construction without stay colliders, retained walking-model frame, lantern
material reuse, owned mesh cleanup and removal of legacy donor geometry helpers.
The updated asset was rebaked with a **1 m** reference rope and **0.10 m** band
height. Blender **5.1.1** regression and freshness checks passed for Solidify
thickness, UV seams, flat shading, canonical dimensions, malformed input
rejection and source-file preservation. The existing dimension-driven fitting
consumes the new bake without additional runtime logic. Release and both .NET
check suites passed again with the updated measurements. The collar fit revision
adds measured radii and seating offsets for all **206** bands across those
configurations. The Dhow aft band's inner radius is **0.08083 m**, replacing the
**0.20 m** capsule radius; the measured wood radius is **0.08183 m**. Release,
GeometryChecks and AssemblyChecks passed, including Dhow centering, complete
Brig masthead-band seating and rejection of capsule-radius sizing in the visual.

The Caelanor captured-control fallbacks retain plugin/runtime version **0.3.0**.
Release build passed with zero warnings/errors, both check suites passed
(including 57 Harmony targets and installed SailInfo signatures), and pinned
formatting/diff checks passed. Placement code is unchanged. The sheet
normalization checks cover mirrored contacts, normals and offset positions, with
aft eligibility checked against the installed mainmast origin; the reef profile
is checked against the recorded mainmast template array. Runtime logs confirmed
the reef fallback placed successfully, but the earlier sheet contacts failed aft
eligibility.

The shared-luff square-sail collision fix retains plugin/runtime version
**0.3.0**. Release builds passed with zero warnings/errors. GeometryChecks
passed, including forward-mast compatibility across **103** authored stays, both
base/topmast directions, and aft/unknown-mast exclusions. AssemblyChecks passed
against the installed references, including **57** Harmony targets and SailInfo
signatures. Pinned CSharpier, Prettier, Ruff and diff checks passed.

The **2026-10-16** staysail binding and control activity simplifications retain
plugin/runtime version **0.3.0**. Release built with zero warnings/errors, and
GeometryChecks and AssemblyChecks passed against installed references, including
SailInfo and **57** Harmony targets. Checks confirm prerequisite coverage of
measured endpoint ancestors across all **103** stay variants, binding through
stay availability without ancestry lookup, and removal/preview dependency
wiring. Shared control activity checks use the mount's hierarchy state without
authored ancestry or inventory lookups; the structural regression check passed.

The **2026-10-15** winch placement simplification retains plugin/runtime version
**0.3.0**. A Release build passed with zero warnings/errors, GeometryChecks
passed, and AssemblyChecks passed against installed references, including **56**
Harmony targets. Optional SailInfo signature checks were skipped because its DLL
was absent. New checks cover dependency cycles/transitive paths, complete aft
fallback ordering, coincident claims, native reclaim, exhaustion recovery and
native binding/mount ownership. Pinned CSharpier, Prettier and diff checks pass.
The shared sheet-template change also passed the Release build with zero
warnings/errors and both suites, including the now-installed SailInfo signature
checks. Its checks cover shared mounting depth on both boats, the fixed
reference frame, invalid surface normals, and structural donor
discovery/validation paths.

The **2026-10-14** configuration ownership refactor, including the per-boat
`Configuration/Boats/` organization and shared `BoatConfiguration`
naming/layout, passed a Release build with zero warnings/errors and both .NET
check suites against installed references. The optional SailInfo signature check
was skipped because its DLL was absent. Configuration comparisons retained all
authored values and ordering across the twelve boats; catalog checks cover exact
names, clone suffixes and unknown boats, and stay checks cover duplicate mount
IDs across groups.

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
its prefabs are outside the supported save compatibility scope.

### Optional Shipyard Expansion

SE is optional in **0.3.0**. The plugin has no SE assembly reference; its soft
dependency preserves load order. Native `Sail.LoadScale` supplies new-sail
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
to continue. Installed-target inspection and automated checks pass. On
**2026-10-12**, the user reported white sails reverting to the donor atlas on
shipyard exit and during texture cycling; the subsequent texture patch addresses
that report.

Missing SE-only rigs retain unavailable option slots so later saved selections
do not shift. The installation guard rejects those entries. A native Sailwind
**0.39** asset audit identified 29 candidate stays: Brig 6, Junk 4, Jong 3,
Sanbuq 2 and Baghala 14. Cog, Kakam and Dhow's current stays require SE-added
parts. Mod-added boats still require their providing mods.

MSS expands native mast selectors to at least 256 and pages parts beyond the
native eight rows. Full arrays retain option indices and order processing.
Paging follows boat/category changes and is skipped when SE provides navigation.
Removing SE from an SE-equipped save is outside the supported migration scope:
part selections are positional and SE stores additional sail state.

### Confirmed game observations

These observations cover specific configurations, not all boats or later
changes.

- **2026-10-12:** the user accepted Mk.A's baked shape and initial white
  appearance, and confirmed removal of rotation, flip and forced-uniform scaling
  restrictions.
- **2026-10-04:** an MSS 0.2.1 / SE 0.11.1 to MSS 0.2.2 / SE 0.12.1 upgrade
  succeeded for one reported save.
- **2026-09-28:** Cog mast 57 placement/routing worked with midstay2 absent and
  the gaff occupying the primary reef winch. The overlay also worked.
- **2026-09-27:** the user accepted the native placement redesign on Brig/Jong.
  Inspected logs showed no exhaustion/binding failures and installed/local DLL
  hashes matched. This predates the architecture cleanup.

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
cover accounting and transitions.

### Installed references and logs

Game directory: `/home/august/.local/share/Steam/steamapps/common/Sailwind`.
Inspect `Sailwind_Data/Managed/Assembly-CSharp.dll`, Unity assemblies,
`BepInEx/core/` (including HarmonyX), and installed dependencies under
`BepInEx/plugins/`: ShipyardExpansion/SE_Bridge, NANDFixes, AllSailsAllShipyards
and SailInfo. Keep debugging dependencies out of the plugin and asset inspection
read-only. The installed mod DLL is distinct from build output.

#### Local decompiled game reference

Maintainers can manually run the Bash wrapper from the repository root to create
or refresh `/home/august/Documents/SailwindGame`:

```sh
./scripts/decompile-sailwind.sh
```

The wrapper locates the repository's Nix flake and launches the Python script,
forwarding all arguments. It can also be invoked by absolute path from another
directory; relative argument paths remain relative to the caller's directory.
The pinned Nix shell supplies Python 3, .NET 8 and ILSpy CLI through
`pkgs.ilspycmd`. `flake.lock` pins the decompiler package along with the other
development tools. The script finds `ilspycmd` on `PATH` and records its full
version output without parsing or validating it. It does not install tools.
Existing `.tools/` directories from earlier runs are unused and left intact.
Initial Nix downloads may require network access. Override the default paths
when needed:

```sh
./scripts/decompile-sailwind.sh \
  --game-dir "/path/to/Sailwind" \
  --output-dir "/path/to/local/SailwindGame"
```

The output directory must be separate from the game and repository. It contains
`source/Assembly-CSharp/` and `source/Assembly-CSharp-firstpass/`, with
generated C# files grouped by namespace and project files for navigation. Unity
libraries in the installed Managed directory resolve dependencies; installed
mods and separate game assets are not extracted. Generated code is reference
material, not a replacement game build. It cannot recover original comments or
scene and prefab configuration; ambiguous behavior still requires IL or asset
inspection.

`manifest.json` records the input paths and SHA-256 hashes, UTC extraction time,
ILSpy version, source-file counts and exact command argument lists.
`decompile.log` records commands and diagnostics. Command output paths in these
records refer to the temporary staging directory used for that extraction; the
manifest's relative output directories identify the published files.

Rerun the same command after a game update. Each refresh stages both assemblies
and verifies nonempty C# output and unchanged input hashes before publishing.
Handled extraction or publication failures preserve the previous snapshot and
report the staging directory containing diagnostics and any recovery files.
Failed staging directories remain available for manual inspection and cleanup. A
per-directory lock prevents concurrent refreshes. Only generated `source/`,
`manifest.json` and `decompile.log` are replaced; existing snapshot paths
require this script's ownership marker in the manifest. Keep personal notes
outside `source/`. Other files in the destination are preserved.

After a successful run, review `decompile.log` for warnings and compare the
manifest's hashes with the installed inputs:

```sh
sha256sum "$HOME/.local/share/Steam/steamapps/common/Sailwind/Sailwind_Data/Managed/Assembly-CSharp.dll" \
  "$HOME/.local/share/Steam/steamapps/common/Sailwind/Sailwind_Data/Managed/Assembly-CSharp-firstpass.dll"
rg -n 'class .*Sail|class .*Shipyard|class .*Winch|class .*Save' \
  /home/august/Documents/SailwindGame/source
```

Implementation validation covers Bash and Python syntax, formatting, static
review of subprocess/path/rollback handling and a Nix-shell `ilspycmd --version`
check (the pinned package reports **9.1.0.0**). Extraction and refresh recovery
remain unverified until a maintainer runs the script manually. Agents must not
execute files in `scripts/`. This tooling does not change plugin behavior.

#### Game logs

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
