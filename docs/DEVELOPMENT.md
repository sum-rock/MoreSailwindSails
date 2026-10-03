# MoreSailwindSails development

See [README.md](../README.md) for features and player instructions and
[AGENTS.md](../AGENTS.md) for agent workflow. This guide owns technical
reference, verification procedures and
[current validation status](#runtime-validation); resolved history is in Git.

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

For **0.3.0-dev**, keep `Plugin.PluginVersion`, the project `<Version>`, the
README development version and startup example consistent. Preserve GUID
`com.august.moresailwindsails`, assembly `MoreSailwindSails.dll`, display
name/namespace `MoreSailwindSails` and prefab IDs **400** (Flying Sail),
**401/402/403** (Mk.A/B/C). Distribute only the plugin DLL.

The following scripts are **manual maintainer workflows**. Agents must not
execute files from `scripts/` or use that directory as their working directory.
Builds/checks do not install the plugin, change saves or publish a release.

- With Sailwind closed, `./scripts/install-local.sh` copies the built Release
  DLL into the default game's plugin directory. An optional game-directory
  argument selects another installation. It does not build the DLL.
- After merging release changes to `master`, `./scripts/tag-release.sh` requires
  a clean checkout, switches to `master`, fetches/fast-forwards and requires it
  to match `origin/master`. It checks matching versions and tag availability,
  shows the tag and commit, then asks `Are you sure? [y/N]` before tagging. Only
  `y` or `yes` (case-insensitive) continues; empty input, other responses or EOF
  cancel with exit status 1. Confirmation happens after the branch switch and
  fetch/fast-forward. Once confirmed, it creates/pushes annotated `v<version>`,
  rebuilds the Release DLL, then creates a GitHub release with that DLL and
  generated notes. It requires Nix and an authenticated `gh`. A later
  build/publish failure can leave the pushed tag.

After manual installation, confirm `MoreSailwindSails 0.3.0-dev loaded!` in
`BepInEx/LogOutput.log`. Flying Sail registration uses donor **110**, prefab
**400** and **825** vertices; staysails register **401/402/403**. Check the
installed DLL separately from build output when diagnosing.

## Source organization

The solution includes the plugin and both check projects. Feature namespaces
follow their directories under `MoreSailwindSails`; Harmony patches live in each
feature's `Patches/` directory and `.Patches` namespace.

| Location                                    | Responsibility                                                                                                                    |
| ------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `src/Plugin.cs`                             | Identity, dependencies, Harmony discovery and optional SailInfo integrations                                                      |
| `src/Sails/FishermansFlyingSail/`           | Mast-mounted sail registration, rig, geometry, tension, billow and aerodynamics                                                   |
| `src/Sails/FishermansStaysail/`             | Family prefab builder, rig, fixed head, edge fitting and reefing; `MkA/`, `MkB/`, `MkC/` supply cuts and identities               |
| `src/Sails/Spritsail/`                      | Category identity, family catalog, balance rules, shipyard browsing and optional All Sails integration                            |
| `src/Sails/Spritsail/LooseFootedSpritsail/` | Shared loose-footed rig, geometry and patches; `MkA/` and `MkB/` supply shape and identity                                        |
| `src/Sails/Spritsail/BoomedSpritsail/`      | Independent boom-supported rig, native single-sheet controls and lifting-boom deployment; `MkA/` and `MkB/` supply companion cuts |
| `src/Stays/FishermansStay/`                 | Independent mounts, registration, previews, controls and save compatibility                                                       |
| `src/BoatRigs/`                             | One class per boat owns supports, stays, mast ancestry, sheet categories and fallbacks                                            |
| `src/Controls/`                             | Native seat discovery, separate sheet/halyard resolvers, atomic reservations and owned control cloning                            |
| `src/Utils/`                                | Optional, read-only in-game diagnostic tools and their geometry helpers                                                           |

Shared boat-rig definitions and the catalog live in `src/BoatRigs/Definitions/`,
with one type per file. They retain the `MoreSailwindSails.BoatRigs` namespace.

In boat profile files, use named arguments for every supplied domain constructor
argument, including explicit null fallbacks and boolean flags. Keep conventional
`Vector3(x, y, z)` coordinates positional. Preserve authored values and ordering
when changing argument style.

Add future sail families under their own `src/Sails/<Family>/` directory. Keep
existing families independently editable;
[shared-helper extraction is deferred](CLEANUP.md). Runtime mesh/object labels
use the family or mark prefix; preserve donor hierarchy names.

Spritsails keep shared type mechanics under `src/Sails/Spritsail/<Type>/`.
`LooseFootedSpritsail/` owns its rig, patches, prefab builder and geometry;
`MkA/` and `MkB/` contain only cut and identity definitions. Sprit/socket
geometry and loose-footed rigid-sprit deployment remain shared at the Spritsails
family level. `BoomedSpritsail/` owns coordinated boom/sprit deployment with a
fixed tack and rigid foot; it reuses family spar, fitting, collar, obstruction
and furled visuals. Broader cross-family helper extraction remains deferred.

Both check suites mirror feature directories and namespaces under
`MoreSailwindSails.Tests.<Suite>.<Feature>`. Staysail behavior is parameterized
by `BehaviorCases` at family level; cut/identity checks belong under `MkA/`,
`MkB/` and `MkC/`. Root `Program.cs` files arrange execution. Shared Harmony/IL
helpers live in `tests/AssemblyChecks/Shared/`; measurement fixtures stay with
features. Geometry projects link source files directly and assembly checks
resolve full type names: update both when moving or renaming code.

## Runtime safeguards

- **Live Cloth topology stays fixed.** Shape by moving existing bones; no mesh
  swaps, bind-pose changes, Cloth rebuilding or solver resets on tacks. The
  mirrored-mesh experiment passed geometry checks but detached/reset in game.
  Initialization and existing furl/render refreshes serve separate purposes.
- Construct templates under inactive parents with fresh Cloth for new topology.
  Preserve the donor Animator as SE's scaling reference and the hierarchy
  expected by `SailShadowCol`. Template-owned and instance-owned meshes have
  distinct lifetimes. Never mirror collider transforms using negative scale.
- Rope endpoints are independent leaves: native `RopeEffect` calls `LookAt` on
  them and must not rotate skin bones. Preserve coupled edge fitting and finite
  fallbacks.
- Keep corner control, appearance and propulsion separate. Aerodynamic frames
  follow posed corners; force patches are scoped to custom sails. Do not fake
  SailInfo values or alter vanilla forces. Retain donor wind-cloth response
  unless evidence warrants a change.
- Filter custom sails only during native control binding and restore the full
  list in a finalizer. Capacity, collision, overlap and saves must see all
  sails; custom controls cannot depend on native mast sail order.
- Resolve authored, connected **active** mast sections/guides; registration and
  previews can precede activation. Protect occupied stays and support chains.
- Clamp fisherman sail travel to **±40°** and spritsail travel to **±89°** after
  `JibAngleMaster.Update` adds sway, preserving tighter collision, prefab, sweep
  and restored limits without snapping transforms.
- Each family retains its iterative order-text guard before NANDFixes. HarmonyX
  runs later prefixes even after `false`: append wrapped lines to the native
  list and consume input so later prefixes cannot recurse on it.
- New sails use native white palette **11** and SE plain texture **0**. Preserve
  saved colors, recoloring and the hidden color-reference renderer; scope plain
  texture/selector/material guards to custom sails. Never change donor/shared
  assets. The SE compatibility patch seeds the native plain texture before
  catalog discovery: numeric saved selections and SE's fixed option lists
  require plain at zero. Never reorder a catalog after texture indices have been
  assigned.

### Sailwind 0.39 audio

Donor 110's wind-center object carries `SailFlapAudio`, which searches only its
parent and grandparent for `Sail`. Both families place it beneath their pivot
frame during inactive construction, preserving its initial world pose. Posed
aerodynamic refreshes continue updating its center/orientation. Retain native
clips, unmute delay and snap initialization; no native audio methods are
patched.

### SailInfo hover names (issue #17)

Optional integration inspected against installed **SailInfo 1.2.1** supplies
`Fisherman's Flying Sail` and `Fisherman's Staysail` for custom sheet and
halyard hover labels, without mark or size. Standard mode normally reads
`Sail.sailName`; Historical and Simple Positional modes otherwise generate
generic names. All three enabled modes now use the custom sail's family name.
Shipyard names retain their mark and size details; `Sail.sailName` is not
modified. SailInfo retains its name-off setting, HUD formatting, halyard suffix,
numeric readouts and vanilla sail naming. No mast location or sheet-side text is
added.

`Compatibility.Patches.SailInfoNamesPatch` independently patches the
parameterless string-returning `SailInfo.WinchInfoSail.SailName()` method,
validating its instance `Sail sailComponent` field through reflection. The
prefix recognizes either custom family's rig and returns its family name,
bypassing SailInfo's positional-name cache. Missing/destroyed references, empty
names and other sails fall through. Absent SailInfo is silently skipped; an
incompatible naming API logs one warning during startup and skips this
integration. There is no direct SailInfo assembly reference. The existing
staysail angle patch remains separate.

## Flying Sail

- Fits a physical mast under **Other**, with active aft support, native mast
  save slots and normal vertical-space/overlap rules. Hoists from deck; partial
  hoists use a procedural renderer, full deployment uses Cloth, and striking
  hides cloth and parks ropes. Only the four corners are pinned.
- Base width is donor 110's `installHeight / 3`; installation height comes from
  the new luff. The isosceles trapezoid has luff `2 × width`, head rising
  **20°** aft, foot falling **20°** aft and aft edge about `2.728 × width`.
  Derive edge budgets, area, bounds and shadow samples from this cut. New
  selections use `SailScaler.SetScaleAbs(1f, 1f)` after SE initialization;
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

The installed Sailwind 0.39 donor `3d rope jib sheet` uses renderer **4478** and
mesh **1475** (`cloth_rope`) in `sharedassets24.assets`. Its unreadable source
has **162 vertices, normals and tangents**, with zero UVs or compressed UV data.
The `rope static` material uses `Standard` without assigned textures. Missing
source UVs are valid here; baked counts still need runtime confirmation.

Knot construction requires and preserves one native normal per vertex. Complete
UV arrays retain their selected vertex mapping and generated tangents. Zero UVs
are accepted only for `Standard` with no assigned texture properties; that path
leaves UVs and tangents absent and skips tangent generation. Partial UV arrays,
mismatched normals and UV-less textured or unsupported materials omit knots
safely. Do not generate substitute UVs or normals or modify shared donor assets.

Each template attempt logs donor/mesh identity, source count/readability, baked
channel counts, material compatibility and the selected policy. Failures
identify the incompatible channel and available diagnostic context.

## Spritsails category

This section records the current spritsail development scope. Spritsails now use
mod-owned `SailCategory` value **6** and a **Spritsails** shipyard entry. The
compiled game enum remains unchanged. Category code and its catalog live in
`src/Sails/Spritsail/`; make-specific mechanics stay in the type/mark directory.
Registration checks the native enum and foreign prefabs before claiming the
value. Behavior patches require both category 6 and a registered family prefab
ID, so failed registration does not apply spritsail rules to a conflicting mod.
Future makes must validate, initialize and register through the family catalog;
registration rollback removes the candidate from the catalog.

### Balance and physics defaults

| Setting           | Development value                                                                                                            |
| ----------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| Price             | `GetSailArea() × 9 × 1.29`, between equal-area gaff (1.25) and junk (1.33); boomed marks multiply this by **1.10**.          |
| Propulsion        | `0.75`, the native junk reduction, applied once inside `Sail.ApplyForce`.                                                    |
| Boat mass         | `GetRealSailPower() × 40`, matching gaff/junk; boomed marks multiply this by **1.20**.                                       |
| Mast height       | Native gaff-style extended-height policy (false); Mk.A retains its geometry-derived fitting dimensions.                      |
| Overlap           | Spritsails map to gaff only while evaluating the native square/gaff vertical-overlap exception, in both installation orders. |
| Shadow colliders  | Ordinary triggers; no staysail collider exception.                                                                           |
| Mk.A rigidbody    | Mass `0.1`, angular drag `1`; provisional handling settings, separate from boat mass.                                        |
| Spritsail scaling | Explicit SE uniform scaling, no shipyard rotation and no jib flipping.                                                       |
| Initial placement | Scaled sail height above the mast base, placing the lower edge at the bottom rather than hanging from the mast top.          |

Tune `[Spritsails] AppliedForceMultiplier` in the BepInEx configuration. Finite,
nonnegative values are accepted, including zero; invalid values use `0.75`. The
setting is read during propulsion, so a configuration-manager change can take
effect immediately; editing the file requires the normal configuration
reload/restart. This does not modify sail area, price, boat mass, other sail
families or vanilla forces. Both applied force components and the native final
force report use the scaled propulsion-local power.

Mk.A retains its wind-angle response, posed aerodynamic frame and exposed-area
reefing calculation. Junk's `0.75` category multiplier does not imply copying a
junk donor's upwind efficiency or introducing a new aerodynamic model.

### Installed-code and donor audit

Native `UseExtendedMastHeight` only admits square/lateen categories (subject to
`junkType`); category 6 already gets the gaff result. `Sail.Start` only applies
its special mass/drag override to staysails, so Mk.A's explicit settings
survive. `SailShadowCol.Awake` makes non-staysail colliders triggers; Mk.A also
initializes its shadow box as a trigger before activation.
`Mast.TopsailsApplyLowestAngle` selects square sails; Spritsails do not join
that coordination. Native mast compatibility excludes square-only and stay-only
supports, with Mk.A's existing active-mast/boat checks narrowing the result
further.

Native shipyard collision checks retain their ordinary non-staysail path. The
square coexistence exception concerns vertical installation ranges, not a
blanket exemption from physical sail/spar obstruction or trim limits. The
overlap transpiler substitutes category reads only in `CheckSailOverlap` and
does not change live `Sail.category`. The force transpiler inserts one local
multiplier after `GetRealSailPower`; both transformations reject an unexpected
native instruction pattern instead of silently missing their integration.

SE `SailScaler.Awake` previously selected a rotatable transform through `Other`.
The category now explicitly clears `rotatablePart` and selects uniform scaling
and non-flippable behavior. SE hides both rotation buttons and its `SetAngle`
method returns before changing transforms when this target is null. Sheet trim
and mast-aligned rig posing remain independent of shipyard rotation.
Category-specific lateen/junklateen installation conversions and staysail-only
rope flipping do not apply. Texture selection remains on the existing custom
appearance path; SailInfo already identifies Mk.A by component.

Read-only installed asset inspection compared the following templates:

| Donor                                      | Useful components                                                                                                         | Reason for selection or rejection                                                                     |
| ------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| **110**, brig jib, `sharedassets15.assets` | Two `RopeControllerSailAngleJib`, `JibAngleMaster`, reef controller, Animator, Cloth/WindCloth, hinge, audio and shadows. | Retained: supplies the required independent clew sheets.                                              |
| **15**, full gaff, `sharedassets1.assets`  | One ordinary angle controller, reef controller, Animator, Cloth/WindCloth, hinge, audio and shadows.                      | Would require reconstructing paired sheets; retained only as the existing timber-material source.     |
| **21**, small junk, `sharedassets9.assets` | One ordinary angle controller, reef controller, Animator, Cloth/WindCloth, hinge, audio and shadows.                      | No component advantage for Mk.A's controls; its category force reduction is reproduced independently. |

The gaff/junk asset rigidbodies both use mass `1`, angular drag `0.5`. The jib
asset uses mass `1`, angular drag approximately `0.1`, but native staysail
startup changes those to `0.1` and `1`. Mk.A explicitly keeps the latter as its
initial paired-sheet handling baseline. No donorless reconstruction, new art,
asset bundle or separate dynamic spar rigidbody was needed. Templates remain
inactive during construction; live Cloth topology and donor assets are
preserved.

### Browsing and validation

The menu clones the native Other button and fits seven rows inside the original
six-row footprint. Initialization is per UI instance. Spritsails no longer
appear in Other. Without All Sails, a family-only pager uses the native button
capacity and current shipyard's available prefabs. With All Sails, the adapter
populates its typed category cache with filtered 12-entry pages from its
complete catalog, clamps page state and lets its existing rendering/navigation
run. Selection and reopening reset the spritsail page; other categories keep
their normal browsing. All Sails remains optional with no DLL reference.

All registered spritsails start at the bottom of the selected mast. A final
family-level `AddNewSail` postfix runs after SE and make-specific sizing,
setting the head's installation coordinate to `GetScaledHeight()`. Setting that
coordinate to zero would put the sail's foot below the mast. Native
`MoveHeldSail(0)` then refreshes position, attachments, sail order and collision
checks. This policy applies only when adding a new sail; ordinary vertical
adjustment remains available. Mk.A no longer moves its head to `mast.mastHeight`
during initialization.

Category checks cover price bounds, force input validation, mass, both overlap
orders, empty/boundary pagination, native IL transformations, registration
ownership/rollback, scaler wiring and the installed optional paging contract.
The category implementation passed a Release build with zero warnings/errors,
GeometryChecks and AssemblyChecks (**94 Harmony targets**). Placement checks
also inspect SE's no-target rotation guards and category-wide initialization
order. Pinned CSharpier, Markdown formatting and diff checks are recorded
separately at handoff. Runtime layout, pointer interaction, previews, hinge
response and Cloth still require observation on Brig, then Sanbuq. Compare
propulsion under controlled wind, heading, sail area and deployment. This
development-only prototype has no save-migration requirement; no existing saves
or installed DLLs were changed.

## Loose-footed spritsail prototype

This is an **unvalidated in-game prototype** on the **0.3.0-dev** development
baseline, not a new release. Prefabs **404/405** identify **Loose-footed
Spritsail Mk.A/Mk.B**; IDs 400–403 and stay mounts 128–255 retain their
identities. Fit it directly to a physical mast through **Spritsails** on **Brig
or Sanbuq**. Other boats remain gated until the first runtime validation passes.
No aft mast or Fisherman's Stay is required.

### Construction and controls

- The family owns its procedural cloth, shadow and blunt-ended sprit meshes. The
  jib at **110** supplies the paired sheet controllers, native cloth settings,
  audio, paint and scaling hierarchy. No live mesh or bind-pose replacement
  occurs during tacking or deployment.
- Installed gaff **15**, `15 SAIL A gaff full` in `sharedassets1.assets`,
  supplies only the `boom_gaff_top` renderer's `medi_small_paint` material.
  Read-only asset inspection found its `ReefEffectAnimUniversal` clip `reef`, an
  override Animator, a separate furled renderer and a single angle controller.
  Native assembly inspection confirms animation time `1 - currentUnroll`. The
  prototype retains the jib's two independent sheet controllers and supplies its
  own deployment pose instead of copying gaff animation or boom behavior. No
  Unity editor, asset bundle or redistributed game assets are needed.
- Base width is donor 110's installation height divided by three. The luff is
  **1.6 × width**. Mk.A's revised, squarer cut places the peak **1.0 × width**
  aft, above the clew, and **tan(15°) × width ≈ 0.268 × width** above the
  throat. This gives a **105° interior throat angle** between the downward luff
  and head chord in the flat, fully deployed outline, preserved by uniform
  scaling. The free foot still rises **0.08 × width** toward the clew. Projected
  area is approximately **1.693975 × width²**; price and propulsion coefficients
  remain unchanged, with native area calculations following the new mesh. The
  complete luff, peak and clew are pinned; the remaining head, leech and foot
  can flex. Full deployment uses coupled foot/leech fitting; the rest mesh
  supplies spare fabric through its camber.
- Exactly three native controls operate the sail: port clew sheet, starboard
  clew sheet and coordinated deployment. The reef purchase passes over the
  carrying mast's active guide to a separate attachment **90% along the sprit**.
  Pulling raises the tip toward the mast; easing lets the sail spread. The winch
  allocator retains native-first pairs, authored fallbacks and retry behavior.
- The heel sits in a **fixed mast pocket one-quarter up the deployed luff**. It
  lies on the stationary luff/hinge axis, so neither reefing nor tacking moves
  the socket. The rigid sprit pivots from its working angle (approximately **34°
  from the mast** at uniform scale) to parallel with the mast. Its length is
  derived once per fitted geometry, including nonuniform scaling. The old moving
  heel and deck datum have been removed.
- The throat and upper three-quarters of the luff remain fixed. The lower luff
  gathers upward toward the socket with visible folds; the clew moves inward and
  upward. Loose foot/leech length budgets constrain the clew: for a wide,
  shallow cut the struck clew can rest above the socket rather than stretching
  the leech. The peak stays lashed beside the tip throughout; there is no
  peak-capture or release phase.
- Below **2% native unroll**, the brig gaff **119** native furled-cloth mesh and
  its rope bindings remain visible against the mast with zero propulsion. From
  **2–98%**, the existing skinned panel handles gathering; at **98%** and above,
  the initialized Cloth solver handles the set sail. The deployment is
  reversible and changes bone positions, not topology. Luff ties follow the
  gathered skin during partial reefing. When struck, the native mesh supplies
  its own bindings and the procedural lines hide. The upper purchase remains
  visible when its native winch is available.
- Installation requires the mast guide to sit at least **5 cm above the fully
  raised purchase attachment**. A too-high or too-wide installation reports
  `SPRIT HOIST REQUIRES A HIGHER MAST GUIDE`; lower or resize the sail, or
  select a taller supported mast. Renderer bounds include all sampled folded
  poses and the raised peak. The nine existing spar collision samples now pivot
  around the fixed heel.
- Force orientation follows the posed corners. Native force already multiplies
  by unroll, so a family-scoped prefix temporarily substitutes the posed
  exposed-area fraction for that calculation. Area uses the unpleated outline
  projected onto the sail plane, excluding folds and bundle thickness. A
  finalizer restores the saved deployment value even after an exception.
  Optional SailInfo supplies the family name; its existing `Halyard` suffix
  still labels the reef control.

### Fitting, compatibility and validation

Only authored connected active mast sections can supply the upper guide. The
selected guide's ancestry is protected against removal and retained through
shipyard previews. Native mast save slots hold the sail and its deployment;
controls reconstruct on load without a new save schema. Registration rejects an
occupied 404 rather than replacing another mod's sail.

The spritsail's control filter wraps the existing families' filters and restores
the original full mast list last. Sheet travel is bounded to **±89°** after
native sway and retains tighter collision limits. The collision checker sweeps
inscribed sail strips and **nine sampled sprit deployment poses**. These samples
are a prototype clearance approximation, not continuous rigidbody collision. Do
not infer guaranteed clearance between samples or from automated checks.

New GeometryChecks exercise the cut, triangle winding, skin weights, full-luff
pins, free-foot travel, fixed spar length under uniform/nonuniform scaling,
reversible deployment, high-installed small sails, monotonic exposed area, peak
attachment, both tacks and coupled edge budgets. New AssemblyChecks cover
registration order/identity, single-mast ancestry, shared control allocation,
mixed-family filter ordering, fixed live topology, force-state restoration,
order-text protection and gaff material ownership. They inspect lifecycle
structure; they do not execute Unity construction, destruction or Cloth.

Development validation on **2026-10-01** passed a Release build with zero
warnings/errors, GeometryChecks and AssemblyChecks (**84 Harmony targets**).
Pinned CSharpier and Markdown formatting checks and `git diff --check` also
passed. Subsequent user observation confirmed the thicker, blunt sprit looked
good, but identified the moving mast fitting and downward gathering as
incorrect. The fixed-pocket/upward-gathering revision supersedes that motion;
its runtime behavior remains pending validation.

Start runtime validation on **Brig**, then **Sanbuq**:

The revised 105° Mk.A cut passed a Release build with zero warnings/errors and
both check suites. Added geometry assertions cover the throat angle, retained
luff/foot dimensions, full-width peak, revised sprit angle, quarter-luff heel,
projected area and finite inscribed collision strips. Existing deployment,
tacking and tension checks also pass. These results do not validate the new
outline's live Cloth or spar clearance; inspect both during the following
checks.

1. Fit at several heights/sizes, including a mast with a Flying Sail or native
   gaff already installed. Check native winches retain priority and exhausted
   controls recover when seats become free.
2. Hoist/lower repeatedly and reverse at partial reefing. Inspect the fixed
   pocket, upright struck sprit, visible bundle, gathered luff ties, upper
   purchase and peak lashing. Check for jumps at the procedural/Cloth
   transition.
3. Tack at full and partial deployment and at both sheet limits. Look for
   persistent spar penetration, fabric inversion, detachment and force/audio
   discontinuities. Solver quality and the sprit's interaction with the cloth
   remain unproven.
4. Preview/cancel mast changes, try removing occupied supports, refit, recolor,
   save/reload and remove the sail. Check full mast lists and all three controls
   after each transition. Verify vanilla and other custom sails.

Do not broaden boat support or treat this as release-ready until the two boats
pass those observations. No game files or saves were changed during development.

## Staysails

All marks fit registered Fisherman's Stays. Register after SE and before All
Sails caches its inventory. Each mark's `FishermansStaysailShape` supplies
geometry and owned-asset prefixes for template and instance creation; do not
reintroduce separate geometry/prefix arguments. The family builder owns donor
**110** and template slope **20°**; first binding creates the owned mesh for the
actual stay slope before enabling Cloth. Mesh and bind poses then stay fixed.

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
- Select tacks from apparent wind projected on
  `Cross(mastAxis, neutralAftDirection)`. A **±0.6 m/s** deadband retains the
  previous side; indeterminate initialization defaults positive. Smooth
  transitions. The old 85% sheet-following policy and inward trim are removed;
  `FishermansStaysailEdgeFit` retains support bow and coupled lower-corner
  fitting without moving the fixed head.
- Rounded billow uses a spanwise sine with `(1-v)*(1+0.75*v)` vertical taper and
  a **12%-width** head peak. Interior travel is capped near **60%** of local
  camber plus foot/leech allowances and clew taper. Opposite-tack skin tests did
  not reproduce Sanbuq's reported asymmetry; excess travel was a suspected
  contributor, not a confirmed Cloth-level cause.
- Reef **upward** using the brig jib's native `reef` clip/controller and
  `furled__sail_cloth_jib` mesh from `sharedassets15.assets`. Sample an
  inactive, stripped animation hierarchy at `1 - currentUnroll`, with no live
  donor scripts or colliders. Preserve animation binding paths and donor
  Animator; disable the original reef component so it cannot compete with custom
  rendering.
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
its support and BoatRig profile were removed after in-game compatibility
problems. Profile gates reject Flying Sail fitting and skip Fisherman's Stay
registration on Leopard; no save migration is provided. Future compatibility
work is tracked in
[issue #33](https://github.com/sum-rock/MoreSailwindSails/issues/33).

The **93** variants prefer **70°** between the aft spar's downward axis and
stay. If that intersects above the connected forward spar, use its physical
masthead and a steeper stay. Preserve physical fore/aft ordering, exclude higher
aft topmasts from lower variants and store endpoints rather than infer them at
runtime.

Append groups/options after SE initializes customization; never reorder save
slots. Reserve mount IDs **128–255** and expand capacity to **256** without
shrinking larger arrays. Missing old-save/cancellation entries restore **None**.
Validate profiles and occupied IDs before construction; roll back a boat's new
stays on failure. Protect occupied stays/supports during invalid previews and
restore preview state in a finalizer. Normalize stay and walking geometry
independently to the same endpoints; donor bounds may differ from `mastHeight`.

### Large dhow

`LargeDhow.cs` uses installed native `BOAT dhow large (30)`. Its **24** native
mast combinations cover two foremast choices, two main positions with optional
matching topmasts, and three mizzen choices. Ten Flying Sail supports and
fourteen stays cover adjacent pairs: eight fore/main masthead fallbacks and six
main/mizzen 70° variants. Topmast IDs **3/5** require bases **2/4** and retain
lower-mainmast halyard guides on the overlapping section. Main/mizzen stays
attach to the lower mainmast whether its topmast is fitted or absent. Native
control arrays include raked spars and topmast seats physically mounted on their
lower sections.

Fore/main stays meet the **rendered** foremast end ring at local z **2.223295**.
The capsule tip at z **2.29** is 6.7 cm higher and produced the reported
floating attachment. All eight fore/main variants use the corrected, slightly
steeper slope; aft guide heights and save IDs stay unchanged.

### Stay collars on Sanbuq and Large Dhow

These boats fit their native forward and aft collars independently of the rope
body. The **26 Sanbuq** and **14 Large Dhow** variants seat each collar around
its supporting spar, keeping its full height below the rendered tip. The visible
rope ends at the facing collar surfaces; logical mount endpoints, stay angles,
sail fitting, control anchors and saved option IDs are unchanged. Other boats
retain their existing geometry path.

Sanbuq's separate native coils supply both attachments, reusing the forward coil
when a donor lacks an aft coil. Its walking representation uses the same coil
source even when the native walking donor omits collars. SE's non-readable
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

The native winch placement redesign is **accepted as valid and complete**. It
supports the seven boats in [Boat profiles and stays](#boat-profiles-and-stays)
and applies to Flying Sails, all three staysail cuts and native sails on
Fisherman's Stays. The follow-up architecture cleanup is implemented.
[Runtime validation](#runtime-validation) separates accepted Brig/Jong evidence
from later changes and remaining in-game uncertainty.

### Placement and ownership

Family rigging remains separate; placement, bootstrap-template selection,
cloning and allocation are shared. Boat profiles provide authored data, native
inventory discovers controls, and separate sheet/halyard resolvers select seats.
The boat-owned reservation ledger and clone coordinator manage allocation and
control lifetime.

Each boat profile authors physical mast categories separately from ordered
native sheet-source rigs. Sources include physical variants, their topmasts and
verified associated native stays. Lookup uses IDs and authored ancestry, never
display-name parsing. Definitions defensively copy collection inputs into
read-only wrappers; the catalog is cached. Categories store ordered source IDs
directly, with pairing only at matching native array indices. Native and
installed SE shipyard group metadata distinguishes variants from simultaneous
mast positions: Jong has separate `MainmastA`/`MainmastB` categories; Cog's
raked foremast belongs to its separate foremast group.

The forward physical mast selects the sheet category (`References.Fore` for
custom stays, `Pair.Fore` for Flying Sails). `SheetingWinchPlacementResolver`
searches complete native left/right pairs in authored source order and native
array order. It discovers inactive source rigs through the boat hierarchy
without relying on the startup `BoatRefs.masts` array. Missing or unmatched
array entries are skipped with a diagnostic. Centre sheets never become pair
candidates.

Native poses preserve both sides' position, orientation and asymmetry. Shared
references and coincident seats are deduplicated; the first supported whole pair
or halyard wins over an earlier unsupported equivalent. All aliases still
participate in occupancy checks. A bound native rope blocks borrowing even while
hidden, as does an active renderer or collider. Mounting ancestors and requested
physical supports must be active. Native poses and manually authored fallback
positions are trusted: there are no radius, proximity or geometric clearance
checks. Authors must choose fallback positions that avoid native fittings and
other fallback positions.

`WinchReservations` atomically claims both sheet seats by identity and aliases.
Halyards share the same boat-owned ledger. No failed sheet claim can consume
only one side. The coordinator registers both sheet bindings before allocation,
excludes all owned clones from native discovery and keeps unused startup
variants unclaimed. Native-control suppression prevents custom-stay mount
controls and sail-owned controls from competing for the same sail.

Boat profiles may define `HalyardWinchGroup(mast, sources)` entries through
`halyardGroups`. Each group maps one exact requested mast ID to a complete
ordered list of native rigs whose reef arrays supply seats. Lookup does not
inherit groups across mast variants or ancestry, and there is no category-wide
or proximity search. An unconfigured mast uses only its own reef array. All
authored groups search the requested mast first, then verified associated native
stay sources. For example, **Cog mast 57 (mizzen mast 2)** uses sources **[57,
58]**: its own seats first, then **58 (midstay 2-2)**, whose
`winch_reef_midstay2` is mounted beside the mizzen's own reef winch. **Cog mast
8 (original mizzen)** uses **[8, 51, 65]**: its own seat, then the shared
`winch_reef_midstay1` referenced by midstays **51 (2-1)** and **65 (1-1)**.
These two references add only one distinct seat. The source stay need not be
installed, but the requested physical mast and the selected winch's mounting
support must be active. Existing native occupancy, aliases, reservations and
stable-placement rules apply. Startup, allocation and retained-placement
validation share this source mapping; source-array changes and native reclaim
still invalidate a borrowed seat.

The installed support audit is recorded in
`tests/GeometryChecks/Controls/HalyardMounts.txt`; native identities and
parent-local poses remain in `FishermansStay/NativeWinchSeats.txt` under the
same test project. Groups are authored separately for each exact requested
section, including native stay controls on lower supports used by matching
topmasts. They do not borrow the ordinary reef arrays of other physical mast
variants or topmasts. Shared stay-seat references are intentional and use the
existing identity/alias reservations.

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
and does not reserve seats; halyard templates follow the requested mast's
authored group order, or use that mast alone when no group is configured. A
missing template reports its category or requested mast and preserves the
existing registration rollback. Native startup still receives initialized
control arrays: installed `Mast.UpdateControllerAttachments` indexes them
directly and calls `GPButtonRopeWinch.AttachToController`.

The selected placement later supplies its exact clone templates. Clones
initialize inactive with owned handles and fresh outlines; native objects and
bindings remain untouched. Template changes prepare both replacement sheet
clones before retiring either old clone, retain sail-owned controllers and
update custom-mount winch arrays. Placement moves the parent mount and preserves
wheel-local input rotation.

Validate current source-array slots, mounting support, native occupancy, aliases
and poses before constructing alternatives. A valid placement retains its ledger
entries and clones; parent mounting poses and visibility still update. Native
reclaim, lost support or a changed boat-relative pose invalidates the entire
pair. Keep a valid current pair stable, including a fallback when native seats
later free. Exhaustion hides both sheets, preserves controllers and retries
every **one second**; it does not roll back stays or remove saved sails. Removal
and inactive owners release claims. All placement state is transient; GUID,
prefab/stay IDs, save ordering and saved geometry are unchanged. The separate
rope-at-boat-origin behavior during complete seat exhaustion remains unchanged.

### Controller lifetime during cleanup

Final `OwnedWinch.Dispose()` releases group claims and destroys owned winches
without reparenting their controllers, including during sibling suspension.
Boat, stay and sail destruction all use this path, so cleanup does not depend on
their `OnDestroy` callback order. Controllers still beneath a destroyed mount
are removed with that hierarchy; controllers already moved elsewhere are left
alone.

Live reconciliation and its rollback use `Retire()`: preserve sibling
controllers, detach the retiring control explicitly, then dispose its winch.
Explicit detachment also protects a retiring control whose group slot already
holds its replacement. Template adoption detaches before destroying the old
clone and rebinding. Ordinary support loss, inactive owners and seat exhaustion
continue preserving controllers while hiding controls for later reuse. A missing
owner triggers final disposal; a broken winch with a surviving owner uses
retirement.

### Inventory lifetime and inspection counters

The coordinator owns one native inventory, mast lookup and seat/alias lookup per
boat. Its empty `LateUpdate` returns without discovery. Groups already released
skip repeated suspension. Alternative searches run only on first allocation,
invalidation or the existing one-second exhaustion retry.

Discovery includes inactive hierarchy objects and does not rely on
`BoatRefs.masts`. The installed-assembly inspection established these hooks:

- `Mast.Awake` registers masts; `OnEnable` and `OnDisable` update winch
  visibility.
- `BoatCustomParts.RefreshParts` and `RefreshPartsWithOrder` call
  `BoatPart.SetOptionEnabled` for ordinary and previewed options.
- SE's `Patch.PartsPatch.Adder` runs during `SaveableBoatCustomization.Awake`,
  expands the native mast array and dispatches boat-specific part installation.

Postfixes invalidate discovery at those six native methods, with ordering after
Shipyard Expansion. Native control-array contents are also compared with
snapshots, including in-place edits. A one-second discovery sweep catches
delayed/inactive parts that missed lifecycle hooks and incomplete startup
inventories. Dirty inventory refreshes before allocation. Native
rope/renderer/collider occupancy and support activation remain live checks;
boat-relative pose changes rebuild the shared seat/alias lookup. Wheel-local
input rotation remains separate from the cached mounting frame.

For runtime inspection, `NativeWinchSeats.HierarchyScans` counts hierarchy API
calls, `CandidateBuilds` counts native candidate-construction attempts, and
`WinchReservations.EntriesCreated` counts newly allocated ledger entries. These
internal counters are per boat and do not log every frame. They are not elapsed
frame-time or total managed-allocation measurements.

### Halyards and Shroud belaying pins

`HalyardWinchPlacementResolver` uses the concrete requested active mast's
authored group, preserving native array order within each source. Flying Sails
request their mounting mast; Mk.A/B/C request the aft base; native sails on
custom stays request that stay's existing aft halyard source. Topmast seats on
an active lower support retain their native association. There is no inferred
alternative-mast search, generated offset or manual halyard fallback.

Shroud uses these same resolvers. Its short mainmast has **five** ordinary reef
entries and its tall mainmast **seven**. Each keeps its own ordinary array
first, then adds sources **16/17/24** from the mainmast's separate jib reef
coils. Foremast and mizzen groups likewise add only their associated stay coils.
This does not restore the former twelve-pin pool across physical variants.
Native reclaim and stable reservations follow the same rules as on every other
boat; capacity depends on the authored sources, their live mounting support and
occupancy.

### Manual sheet fallbacks

Every category names a nullable port/starboard fallback pair. Native pairs take
priority on a new allocation. Each fallback side specifies its own contact
point, normal, mounting offset, compatible template and support requirements; a
captured surface point alone is not a finished mounting pose. Never reflect one
side or generate positions along a rail or mast.

Only **Shroud / ForemastFallback** is currently populated, using its previously
measured forward trim pair. All other category fallbacks remain explicitly null,
including Jong's separate mainmast fallbacks. This is supported configuration,
not unfinished redesign work. Any future fallback additions require labeled F9
captures and visual/reachability confirmation before authoring. Missing vectors
do not block native placement.

Null, incomplete or non-finite fallbacks are unavailable as a whole. When native
pairs exhaust, missing fallback data produces one contextual error per
definition and boat/category during that boat instance's lifetime. Retries
remain quiet. A valid fallback already reserved by another owner produces
ordinary exhaustion diagnostics. No default origin placement, partial pair,
generated strip or rotated mast search remains.

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

**LogFallbackWinchPlacement** is an on-demand capture tool in `src/Utils`,
**F9** by default. Preserve its `CaptureWinchPosition` config key, capture
numbering and log format for compatibility. Load the boat, close menus, aim the
centre of the screen at bare mounting structure within **10 m**, and press the
key once. An on-screen notification confirms the numbered capture.
`BepInEx/LogOutput.log` records `Winch position capture #N` with boat identity,
boat-relative surface `position` and `normal`, collider object path/type,
hit-model identity and distance. Record capture numbers and intended roles (for
example forward port/starboard sheets). Captures do not place winches or modify
saves.

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
still need authoring; a captured surface point is not automatically a winch
pivot.

### Viewing winch mounting points

**WinchMountOverlay** is an optional diagnostic utility in `src/Utils`. Enable
it with the game closed in `BepInEx/config/com.august.moresailwindsails.cfg`:

```ini
[Diagnostics]
EnableWinchMountOverlay = true
ToggleWinchMountOverlay = F8
CaptureWinchPosition = F9
```

`EnableWinchMountOverlay` defaults to **false**. With it enabled, load a boat,
close menus, aim at a boat surface within **10 m**, and press **F8**. The
overlay stays attached to that boat as you move around. Press **F8** again to
hide it; to inspect a different boat, hide it first, then aim and toggle again.
The shortcut is configurable; `None` disables the shortcut. Setting the enable
option back to false removes the overlay. Normal BepInEx config loading applies;
editing the file outside the game is not a live-reload mechanism.

Wireframes show through the hull and deck, including winches on inactive rig
variants. A small legend identifies the selected boat and grouped location
count:

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
including displaced walking-model ray conversion and world obstruction checks. A
separate diagnostic inventory includes inactive controls without requiring
custom sails or participating in placement reservations. Discovery and cached
shape selection refresh once per second while shown; current transforms and
occupancy are read at camera rendering time. Native meshes supply cached
triangle edges; unreadable or empty meshes use their local bounds, and skinned
renderers use local bounds. These boxes indicate extents, not an exact
silhouette.

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
import-parent transforms when comparing measurements in boat coordinates.
Confirm dependencies against installed assemblies. Fixtures contain numeric
measurements only, never meshes, textures or assemblies.

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
These are reference measurements, **not boat-local fallback vectors**. The
inventory was read from installed assets and SE's serialized part/option
metadata; no proprietary assemblies or extracted asset payloads are committed.

`NativeWinchSeats.txt` distinguishes Brig's registered source **70** (mainmast
**B1** reef seats) from unregistered source **74** (foremast **F1** seats).
Their hierarchy paths match, but they are distinct serialized objects. Mainmast
halyard groups use 70 and exclude 74; identify audit objects by asset, hierarchy
and order index rather than path alone.

Earlier numeric fixtures (`WinchMeasurements.txt`, rail/surface measurements,
full-native obstruction tables and Shroud trim measurements) remain available
for manual fallback authoring. They do not reinstate generated placement
searches.

### Runtime validation

This is the recorded validation status for **0.2.1 preparation**, through
**2026-09-29**. The results below come from implementation checks and user
tests; they are not new test runs performed during documentation consolidation.

#### Recorded automated results

The documented changes passed pinned CSharpier checking, Release builds with
zero warnings/errors, GeometryChecks, AssemblyChecks and `git diff --check`. The
latest recorded assembly run covered **62 Harmony targets**. Current catalog
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
requests from **1,200 to 20**, candidate builds from **600 to 1**, and
reservation entries from **1,200 to 2**, relative to an inspection-derived model
of the old behavior. Scan requests invoke counters, not Unity hierarchy APIs.
This establishes scheduling/allocation behavior; runtime counter comparison and
frame-time profiling remain unmeasured.

Neither suite simulates Unity Cloth or proves live rendering, control binding,
destruction, shipyard or save/reload behavior. The installed-asset collar audit
also does not establish visual acceptance.

#### Confirmed game observations

- **Placement baseline, 2026-09-27:** the user accepted the redesign as valid
  and complete. On Brig, Mk.C received both sheets and its mainmast halyard at
  reef index 1 (`rope_winch_mastB1_reef (1)`); a native sail on custom stay 144
  received both sheets and its mizzen halyard. On Jong, all eight logged owner
  instances across fitting/recreation received sheets and a halyard; the final
  configuration was a Flying Sail and two Mk.B staysails, not eight simultaneous
  sails. BepInEx and Unity logs agreed, with no exhaustion or binding/placement
  failures; installed/local DLL hashes matched. All observed placements used
  native seats.
- **Cog mast 57, 2026-09-28:** the user reported the placement/routing fix
  worked with midstay2 absent and the gaff occupying the primary reef winch. No
  fresh log was independently inspected; reclaim, support changes and reloads
  were not all established by that test.
- **Overlay, 2026-09-28:** the user reported it worked, but occupied
  amber/yellow locations were hard to distinguish from green. Occupied traces
  now use red; the new color still needs visual confirmation.

These observations apply to the tested configurations. The accepted Brig/Jong
session predates the architecture cleanup, for which no dedicated game session
was recorded. It does not establish every boat, fallback or lifecycle
transition. Cog mast 57 confirmation does not validate the later mast 8 change.
Earlier 0.2.0 placement failures are a regression baseline, not evidence that
those failures recur on other boats in 0.2.1.

### Related issues

- [#16](https://github.com/sum-rock/MoreSailwindSails/issues/16): Jong foremast
  staysail port sheet placement.
- [#21](https://github.com/sum-rock/MoreSailwindSails/issues/21): broader winch
  placement and capacity investigation.

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

Each script shows the latest **50 lines**, then follows new output with
`tail -F`, including when a game restart truncates or replaces the log. Missing
files are retried. Paths use the default Steam installation beneath `$HOME`.
Press **Ctrl+C** to stop. These commands only display logs; they do not archive
them.

Capture logs before restarting a freeze. Distinguish other mods' exceptions and
suspected causes from confirmed evidence. Prefer `rg`/`rg --files`; inspect
screenshots with the local image viewer. Temporary tools may exist at
`/tmp/fisherman-inspect/` (ILSpy helper/cache) and
`/tmp/fisherman-assets-env/bin/python` (UnityPy). Inspect their projects,
dependency paths and cached results before use; recreate if absent.

### Spritsail travel validation

Spritsails share a ±89° envelope, matching the installed native
`ShipyardSailColChecker` constructor defaults. The native collision sweep uses
5° steps and can narrow either tack independently; sheet tension further
restricts hinge movement. The family patch clamps the final hinge after native
sway without widening collision limits or resetting Cloth. Fisherman sails
retain their separate ±40° policy. The jib donor and paired sheets remain in
use.

Geometry checks cover both tacks through ±89° during reefing and full
deployment, asymmetric obstruction limits, tighter sheet limits and crossed
endpoints. In-game validation remains pending: start on Brig, then Sanbuq,
checking eased travel, each sheet, collision stops, tacking and reefing at wide
angles. Automated checks do not simulate Unity Cloth or establish observed
collision behavior.

### Shared sprit and snotter visuals

All spritsail makes use the family-level `SpritsailSpar`, `SpritsailSnotter` and
`SpritsailSpritGeometry` components. Mk.A supplies its existing heel/tip pose
and carrying mast. Sprits are 3% thicker at the middle; end radii are 85% of the
middle radius, with separate flat-cap vertices for sharp end normals. Collision
sweep thickness includes the same 1.03 multiplier.

The mast attachment now follows the user's traditional wooden mast/sprit-joint
reference in shape: curved cheek plates and a lower cradle, a mast band, and
raised bolt heads. All fitting surfaces now use the loaded native `mast_metal`
material, or an owned near-black fallback, to resemble iron. The family-level
fitting remains separate from the spar and sail so a later asset can replace its
generated geometry. The decorative fitting has no collider or separate
rigidbody.

`SpritsailMastSurface` caches the mast's readable rendered mesh and ray-tests
its surface at socket height. This replaces the oversized collision-capsule
radius that could leave the fitting floating. Sanbuq's known non-readable
topmast 80 uses its existing authored timber taper; unknown/unreadable surfaces
log a warning and retain a capsule fallback. The heel now sits **1.1 sprit radii
plus 5 mm** off that surface, replacing the former minimum 8 cm/2.2-radius gap.
The fitting back seats against the timber while the sprit pivots in its recess.

`SpritsailFurledVisual` shares brig gaff **119**'s `furled__sail_cloth_back`
mesh, inspected in installed `sharedassets15.assets`. It retains both submeshes
(cloth and `rope static`), UVs and authored cross-section proportions. The long
axis is fitted alongside the upright sprit, with the inward face against the
mast. Only the cloth material slot follows spritsail recoloring. It is a
separate struck-only renderer; live Cloth and bind poses are untouched. No game
assets are redistributed.

`SpritsailDeployment` and `SpritsailDeploymentPose` define the shared
fixed-socket motion, persistent peak lashing, loose edge budgets and projected
exposed area. Makes supply their scaled corners and can supply an explicit
working socket; Mk.A uses the default quarter-luff position on its fixed hinge
line. Its `LooseFootedSpritsailGathering` poses existing skin bones into folds
independently of the shared rigid-sprit calculation.

Each pocket owns its generated mesh and disposes it on destruction; native
materials are shared read-only. Checks cover the retained sprit profile, cap
winding, pocket surfaces, fixed socket/throat, mast rake, both tacks, constant
length, upward gathering, edge budgets, purchase movement and zero struck area.
The Release build and both suites pass; neither suite executes Unity Cloth. The
user reported the fixed-pivot motion improved, but rejected the procedural
struck bundle and reported that the socket floated off the mast. This revision
replaces those visuals; its appearance still requires validation.

Runtime validation remains pending on Brig, then Sanbuq: inspect pocket seating,
upper guide reach, peak attachment, visible folds/bundle, partial-reef
reversals, fully deployed transitions and clearance throughout ±89° travel.

The latest visual revision adds shared `SpritsailRopeCollar` three-turn coils
using native rope materials. An upper coil follows the 90%-height halyard
purchase and the sprit's tapered radius; the visible purchase meets its outward
surface. Seven evenly spaced luff ties connect to mast collars, following the
posed luff during reefing. Like the Sanbuq fisherman stay attachments, these
collars use rendered mast dimensions rather than collision capsule dimensions.
Their geometry is generated independently of the stays; no native meshes or
materials are modified. Luff collars hide when fully struck, leaving the brig
gaff bundle's authored bindings visible.

The user reports the iron fitting and rope additions look great, but the mast
collars are too prominent. Mast collar rope diameter is now reduced by 20%; coil
spacing and surface clearance follow the thinner rope. The upper sprit coil and
other rigging retain their previous thickness. Validate this revision on Brig,
then Sanbuq: iron appearance, coil seating and rope thickness, luff tie spacing,
gathering, both tacks and fully struck transitions. Automated checks cannot
establish these rendered results.

### Loose-footed spritsail sheet flex

`LooseFootedSpritsailFlex` lives above the make directories and supplies shared
sheet loading, smoothing, lower-panel weights, edge fitting and collision
bounds. Mk.A applies it after its ordinary deployed/gathered bone pose. Other
sail families retain their existing mechanics.

The lower peak–clew–tack triangle curves toward the sheet winches, with squared
clew barycentric weight fading to zero along the peak-to-tack diagonal. The
upper panel, luff, peak and sprit retain their existing pose and camber. Maximum
clew travel starts at 15% of scaled foot length and fades with deployment; fully
struck visuals stay unchanged. Native sheet paid-out/routed lengths supply a
visual load estimate, ramping over the final 10% of slack. Both loaded sheets
combine by direction and load; unavailable sheets contribute nothing. The pull
smooths at 5/s, and missing bindings relax toward the ordinary pose.

The fit limits the sampled curved foot and leech to their lengths in the current
undeformed pose, including existing camber and reef folds. It searches bounded
inward compensation to make room for curvature, then reduces travel where
needed. No additional edge length is introduced. This is a controlled visual
model, not a new cloth-force solver. Existing cloth solver travel is unchanged;
only skin bones move. Sheet endpoint leaves follow the fitted clew, and the
existing aerodynamic frame follows the resulting corners. Force multipliers and
reef-area calculations are unchanged.

Separate lower-triangle collision strips conservatively include maximum flex and
camber; upper collision strips retain their previous geometry. Culling bounds
include flex throughout deployment. Shared helpers own no native assets and
introduce no saved fields, configuration settings or public APIs. Version
remains **0.3.0-dev** (runtime metadata **0.3.0**).

Automated checks cover sheet slack, opposing pulls, fixed upper corners and
diagonal, curved edge budgets, useful transverse movement, both tacks, reef
states, scaling, collision coverage and finite fallbacks. Assembly checks guard
shared-helper integration and prohibit Cloth rebuilding during flex. Release
build and both check suites pass; they do not simulate Unity Cloth.

In-game validation is pending on Brig, then Sanbuq: tighten/ease each sheet,
load both sheets, tack at wide angles, reverse partial reefing and fully strike.
Confirm a softer lower panel with a firm upper sail, continuous rope attachment,
no transition jumps, detachment or clipping, and acceptable collision clearance.
The 15% limit and sheet-load estimate remain visual tuning starting points.

### Sprit obstruction on one tack

The shared `SpritsailObstruction` component supplies one smoothed state for
propulsion and procedural camber. Mk.A declares starboard as its affected tack;
future makes can reverse that declaration for port-mounted sprits. Installed
`Sail.GetApparentWind` IL returns `Wind.currentWind - shipRigidbody.velocity`,
and `WindCloth.Update` applies that vector as acceleration. Consequently,
negative boat-local X airflow arrives from starboard. Classification uses the
boat frame, independently of the rotating sail, with a 0.035 normalized lateral
deadband (approximately two degrees). The previous target persists within the
deadband; calm clears it. Changes blend over half a second. Unbound, invalid,
loading, disabled and fully struck states reset the effect.

`[Spritsails] ObstructedTackForceMultiplier` defaults to **0.90** (10% less
propulsion on the affected tack). Values range from 0 to 1; non-finite values
use 0.90. A value of 1 disables the additional force penalty without disabling
visual shaping. The existing category-scoped propulsion hook multiplies this
factor exactly once alongside `AppliedForceMultiplier`. Clear tack or absent
runtime state supplies a neutral multiplier. Force never samples Cloth shape.

On the affected tack, existing camber is reduced by up to 50% along the sprit's
projected centerline, fading smoothly to no reduction at 15% of sail width. The
same mask applies to deployed and partially reefed poses before sheet flex; reef
folds, attachments and the struck bundle are retained. Cloth constraints, mesh
topology, collision setup and native wind acceleration stay unchanged. This is
an appearance approximation and does not guarantee contact clearance. The
existing larger collision/culling envelopes remain conservative.

The user confirmed that the earlier lower-panel wrinkle disappears when the
sheet is eased and accepted that tension-dependent behavior. This new local
camber restriction still needs in-game validation on Brig, then Sanbuq: compare
both tacks, ease/tighten sheets, cross the centerline, reef and strike. Verify
starboard alone loses the configured force after settling and appears flatter
near the sprit. Initial 10% force loss and 50% local camber reduction are tuning
values, not measured aerodynamic claims.

Release and both automated suites cover tack reversal, deadband, smoothing,
force limits, local shaping, reef-fold preservation, installed wind convention
and shared-state wiring. Neither suite simulates Unity Cloth. Version remains
**0.3.0-dev** with runtime metadata **0.3.0**.

### Loose-footed spritsail marks

Mk.A retains prefab **404** and its original 105-degree, full-span head. Mk.B
uses prefab **405**. Its base cut has a **135-degree throat** and a straight
foot **1.25 times** its head; the final cut stretches that shape **30%
horizontally**, keeping every corner height unchanged. The resulting throat
angle and edge ratio therefore differ from the base cut. Both retain the same
luff and tack/clew heights; Mk.B shortens the head's horizontal reach and raises
the peak. Curved camber arc length is not the measurement for this ratio.

Marks supply identity and shape only. Both use the same inactive-template
builder, donor 110, mesh ownership, rig, controls, gathering, sheet flex,
starboard obstruction, fitting rules, family price/force rules and struck
visuals. Names include Mk.A/Mk.B in both shipyard and optional SailInfo HUD.
Registration remains independent and idempotent per mark, with occupied-slot
protection and per-candidate rollback. IDs 400–403 are unchanged.

The shared panel geometry derives row camber from actual head reach. Collision
strips clip against each convex outline, including Mk.B's sloping leech; lower
flex bounds follow the actual peak-to-tack diagonal and leech. Panel-strip
references are stored explicitly so refresh cannot overwrite interleaved flex or
spar colliders. Existing live Cloth topology stays fixed during tacks.

Checks retain the original Mk.A geometry regression, exercise shared flex and
obstruction across both marks, and verify Mk.B's edge ratio, throat, skinning,
collision fit and fixed-heel reefing. Release and both suites pass; Unity Cloth
and the new shipyard entries still need in-game validation on Brig then Sanbuq,
including scaling, tacking, reefing and save/reload. No installed game files or
saves are changed by the build. Version remains **0.3.0-dev** (runtime
**0.3.0**).

The earlier report of zero port-tack efficiency could not be reproduced by the
user; no speculative force or HUD fix was applied.

### Boomed spritsail companions

**Boomed Spritsail Mk.A/Mk.B**, prefabs **406/407**, accompany the existing
loose-footed marks on the **0.3.0-dev** baseline (runtime **0.3.0**). Their
fully deployed corner coordinates and default dimensions match their respective
companions, including Mk.B's 30% horizontal stretch. The boomed type owns its
mesh, skin, deployment, fitting and patches independently; mark directories
supply identity and cut only. Availability remains limited to **Brig and
Sanbuq** through **Spritsails**. IDs 400–405 and stay IDs 128–255 are unchanged.

Installed gaff **15**, `15 SAIL A gaff full` (`full gaff`), supplies the
inactive construction template, ordinary `RopeControllerSailAngle`, reef
controller, Animator, readable Cloth mesh and shadow/audio hierarchy. A
read-only installed-asset audit confirmed its single `angleControllerMid`,
`limitBoth` setting and native two-segment sheet route. The brig jib **110**
supplies the companion sizing baseline, upwind efficiency and sail amplifier;
the gaff's serialized Cloth wind response is retained. Procedural cloth, shadow
and spar meshes are template-owned. The boom shares the owned spar mesh and
read-only gaff timber material; no native assets are modified or redistributed.

Exactly **two native controls** operate each sail: one boom sheet and one
coordinated deployment control. The sheet routes through the mast's native
`midRopeAtt` (or its native winch fallback) to a fresh endpoint leaf beneath the
clew bone, at the boom tip. Native `Mast.UpdateControllerAttachments` binds
`midAngleWinch` and `reefWinch` by native mast order without a category check.
Boomed sails therefore remain in the native binding list and use native capacity
and placement; they do not reserve paired sheets or create custom winches.
Existing loose-footed and Fisherman control filters continue restoring full
lists. Native single-sheet trim constrains both tacks; a boomed-only postfix
bounds the final ordinary controller limits to ±89° and tighter collision stops.

The rigid boom pivots at the **fixed tack**. The complete foot is straight and
pinned to it, with panel camber and solver travel fading to zero at the foot;
there is no loose-footed sheet flex. Reefing raises the boom and sprit toward
upright while the cloth gathers against the mast. The sprit retains its fixed
quarter-luff socket and side lashing; the throat and entire luff remain fixed.
Both spars retain their lengths, with separate fixed-pivot arcs driven by the
same native deployment value. Head and leech chords may slacken but do not
stretch. Interior folds vanish at the attachments. Existing family obstruction
shaping acts on the panel, and propulsion uses the posed projected area,
reaching zero when struck. The native mast-side bundle represents the struck
cloth, spanning from the fixed tack to the raised sprit tip.

The deployment purchase remains at 90% of the sprit, through an authored active
upper mast guide; the boom lift is coordinated without another player control.
Original gaff topping-lift and reef visuals are suppressed on the clone, while
the native sheet remains visible even when struck. Mast ties, iron fitting and
upper purchase use the existing family visuals. Supporting active mast ancestry
is protected through shipyard previews and removal. Native save fields retain
prefab IDs, scale, color, sheet setting and deployment; no new save schema or
configuration is introduced.

Fitting includes separate **nine-pose sprit and boom sweeps**, plus panel strips
that include camber and Cloth travel. Culling bounds include gathered poses and
the raised spars. These sampled sweeps are a clearance approximation, not proof
of continuous collision coverage. Fresh Cloth is constructed only on the
inactive template; live tacks and reefing move existing bones. Existing render
state transitions handle deployed Cloth, procedural reefing and the struck
bundle.

Automated validation covers companion cuts, straight pinned feet, rest skin
reconstruction, fixed pivots, rigid lengths, head/leech budgets, both tacks,
scaling, reversible reefing, monotonic exposed area and finite fallbacks.
Assembly checks cover new IDs and rollback wiring, installed gaff binding,
separate boom/sprit sweeps, unchanged live topology, force-state restoration,
NANDFixes order guards and mark-specific SailInfo names. Release and both suites
pass; these checks do **not** execute Unity construction, Cloth or rendering.

In-game validation is pending: begin on **Brig**, then **Sanbuq**. Fit and
resize both marks, compare the companion cuts, tighten/ease the single sheet,
tack at wide angles and test asymmetric collision stops. Reverse partial reefing
and fully strike/redeploy; inspect the lifting boom, mast gathering, sprit
pocket, cloth/boom attachment, native sheet route and folded bundle for clipping
or jumps. Check recoloring, mixed native/custom sail installations, occupied
native controls, support-removal previews and save/reload of sheet and reef
settings. No installed DLL or save is replaced by the build.

#### Boomed collision initialization correction

The user reported both boomed marks remained at **checking collision...** and
could not be installed. The inspected Unity log records
`ShipyardSailColChecker.Awake` throwing `NullReferenceException` at IL offset
`0x00f5` during sail cloning; the installed DLL matched the preceding build.
BepInEx did not include this native exception. Installed IL confirms that Awake
walks every direct child, adds its sub-checker and kinematic body, then
unconditionally marks that child's collider as a trigger.

The boomed builder removed the gaff's collider components but left their empty
GameObjects behind. It now removes entire donor collision objects on the
inactive clone. All 24 panel strips and both nine-pose spar sweeps are created
and posed before activation, allowing native Awake to initialize every child.
Runtime scaling/placement only updates those existing shapes. Previously the
sweep objects were added after Awake and missed native collision reporting.

Regression checks inspect the installed Awake contract, whole-object donor
removal, inactive sweep construction and the absence of runtime shape creation.
Release and both check suites pass; Unity startup, completed collision checks
and successful installation still need an in-game retest of both marks on Brig,
then Sanbuq. This corrects the initialization failure without bypassing
collision checks or forcing installation readiness. Version remains
**0.3.0-dev**.

#### Boomed deployment winch direction

The user reported that deploying required cranking against force. Gaff 15's
serialized reef controller has `reverseReefing = true`, which maps hauling in to
deployment. Boomed templates now explicitly set it to **false** before
activation: paying out/letting fly increases deployment, while hauling in raises
the boom and sprit to reef against load. The native controller retains its
**25** weight resistance and **1.2** wind-load multiplier. Native winch input
limits slow loaded hauling; quick release remains the faster deployment action.
No additional speed multiplier or shared winch mutation is introduced.

Regression checks cover the template override, installed length-to-unroll
mapping and retained native weight/wind resistance. Release and both suites
pass; live input direction, relative speed and partial-reef reversals still need
confirmation on Brig, then Sanbuq. Version remains **0.3.0-dev**.

#### Boomed price and boat weight

Both boomed marks (406/407) cost **10% more** and contribute **20% more boat
mass** than their previous family baseline. The premiums scale with native sail
area and real sail power, respectively. Loose-footed marks keep their existing
values. This changes carried boat weight only; sail Rigidbody mass, boom
movement, propulsion tuning and winch resistance are unchanged.

Validation covers both marks across multiple sizes, zero size and IDs outside
the boomed pair. Release build (zero warnings/errors), both check suites,
CSharpier, Prettier and diff checks passed. In-game price and boat loading
confirmation remains pending; start on Brig, then Sanbuq.

#### Shipyard spritsail description

The category description now covers separate loose-footed sheets, the boomed
single sheet, and upward reefing with cloth gathering at the mast. It removes
the obsolete moving-snotter hoist description. It states that **starboard tack
performs worse than port tack**, matching both rigs' `StarboardAffected = true`
setting. Propulsion and tack classification are unchanged.

#### Spritsail architecture cleanup

Loose-footed templates now construct and pose all nine spar-sweep colliders
before native `ShipyardSailColChecker.Awake` initializes child reporting.
Runtime refresh only updates existing colliders, matching the boomed lifecycle
fix.

Both types share family-level shipyard insertion patches calling
`SpritsailCatalog` directly; unused type forwarding methods are removed. The
native Awake postfix and document-opening fallback retain their timing and
priority. Type-specific rig mechanics remain independently editable.

Mast-surface queries now retain one cache slot per luff tie while sharing one
read-only mesh snapshot per surface instance. Position and direction are keyed
in mast-local space; direction magnitude includes transform scale. Changed
queries miss the cache, and changing masts replaces the surface and its slots.
The existing single-slot socket query and capsule/authored Sanbuq fallbacks are
preserved. No frame-time improvement has been measured in game.

Regression coverage includes construction-time loose-footed sweeps, no runtime
collider creation, seven independent cached queries and invalidation by
position, direction or scale, family-level insertion wiring, and agreement
between the starboard rig setting and shipyard description. Release build (zero
warnings/errors), both check suites, CSharpier, Prettier and diff checks passed.
Runtime validation remains: Brig first, then Sanbuq; fit/resize both types near
spar obstructions, check collars during reefing and mast changes, and reopen
shipyard documents with mixed native/custom sails. No live Cloth topology, save
IDs, balance settings or installed game files are changed. Version remains
**0.3.0-dev**.
