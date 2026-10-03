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
| Loose-footed Spritsail Mk.A/B | **404/405**     |
| Boomed Spritsail Mk.A/B       | **406/407**     |
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

Use named arguments where practical, especially profile constructors, explicit
null fallbacks and flags. Keep one class per file with a responsibility comment.
Preserve authored values and ordering when changing argument style.

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
- Filter sails with custom paired controls only during native control binding
  and restore the full list in a finalizer. Capacity, collision, overlap and
  saves must see all sails. Boomed spritsails use native single-sheet binding
  and must remain in the native list.
- Resolve authored, connected **active** mast sections/guides; registration and
  previews can precede activation. Protect occupied stays and support chains.
- Clamp fisherman sail travel to **±40°** and spritsail travel to **±89°** after
  native controller updates, preserving tighter limits without snapping
  transforms. Paired sheets use `JibAngleMaster.Update`; boomed spritsails use
  the ordinary angle controller's `LateUpdate`.
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

The donor wind-center object carries `SailFlapAudio`, which searches only its
parent and grandparent for `Sail`. Custom rigs keep it beneath their pivot frame
during inactive construction, preserving its initial world pose. Aerodynamic
refreshes update its center/orientation. Retain native clips, unmute delay and
snap initialization; no native audio methods are patched.

### SailInfo hover names

Optional integration, inspected against **SailInfo 1.2.1**, supplies grouped
`Fisherman's Flying Sail` / `Fisherman's Staysail` labels and mark-specific
loose-footed/boomed spritsail labels. Standard, Historical and Simple Positional
modes use these names. SailInfo retains name-off settings, HUD formatting,
halyard suffix and numeric readouts; vanilla names and `Sail.sailName` are
unchanged.

`Compatibility.Patches.SailInfoNamesPatch` validates the parameterless
string-returning `WinchInfoSail.SailName()` and instance `Sail sailComponent`
field through reflection, then recognizes custom rig components. Missing or
empty sail references fall through. Absent SailInfo is silently skipped; an
incompatible API warns once and skips integration. There is no direct assembly
reference. The staysail-specific angle patch remains separate.

## Flying Sail

- Fits a physical mast under **Other**, with active aft support, native mast
  save slots and normal vertical-space/overlap rules. Hoists from deck; partial
  hoists use a procedural renderer, full deployment uses Cloth, and striking
  hides cloth and parks ropes. Only the four corners are pinned.
- Base width is donor 110's `installHeight / 3`; installation height comes from
  the luff. The isosceles trapezoid has luff `2 × width`, head rising **20°**
  aft, foot falling **20°** aft and aft edge about `2.728 × width`. Derive edge
  budgets, area, bounds and shadow samples from this cut. New selections use
  `SailScaler.SetScaleAbs(1f, 1f)` after SE initialization; preserve saved
  sizes.
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

Spritsails use mod-owned `SailCategory` **6**; the native enum is unchanged. The
family catalog validates native/foreign category conflicts and prefab ownership
before registration, and rolls back failed candidates. Family balance patches
require both category 6 and a registered prefab ID. Type-specific patches
recognize their rig component. Mark definitions supply cut and identity;
loose-footed and boomed mechanics remain independently editable.

All four marks currently fit physical masts on **Brig and Sanbuq only**. They
need connected active mast sections and an upper guide, not an aft mast or a
Fisherman's Stay. Other boats remain gated pending runtime validation.

### Balance and physics defaults

| Setting                | Current value                                                                    |
| ---------------------- | -------------------------------------------------------------------------------- |
| Loose-footed price     | `GetSailArea() × 9 × 1.29`; between equal-area gaff (1.25) and junk (1.33)       |
| Boomed price           | Loose-footed formula multiplied by **1.10**                                      |
| Loose-footed boat mass | `GetRealSailPower() × 40`, matching gaff/junk                                    |
| Boomed boat mass       | Loose-footed formula multiplied by **1.20**                                      |
| Propulsion             | `[Spritsails] AppliedForceMultiplier`, default **0.75**                          |
| Starboard penalty      | `[Spritsails] ObstructedTackForceMultiplier`, default **0.90**                   |
| Sail Rigidbody         | Mass **0.1**, angular drag **1** for both types; separate from carried boat mass |
| Scaling                | Uniform SE scaling; no shipyard rotation or jib flipping                         |
| Travel                 | **±89°**, retaining tighter collision and sheet limits                           |

`AppliedForceMultiplier` accepts finite, nonnegative values, including zero;
invalid values use 0.75. It scales propulsion-local power once in
`Sail.ApplyForce`, including the native final force report. It does not alter
area, boat mass or other families. The junk coefficient does not copy junk
upwind efficiency: both types retain brig jib 110's upwind efficiency and sail
amplifier. Configuration values are read during force calculation; external file
edits require normal BepInEx reload/restart.

Native extended mast height excludes category 6; type-specific height patches
report the scaled luff. Spritsails do not join square topsail angle
coordination. Shadow boxes are ordinary triggers. Only the square/gaff
vertical-overlap exception maps spritsails to gaff, in both installation orders;
physical collision checks remain active. Force and overlap transpilers reject
unexpected native IL patterns, and never temporarily change the sail category.

### Browsing and registration

Family-level shipyard Awake and document-opening patches insert all registered
spritsails through `SpritsailCatalog`. Registration follows SE initialization
and precedes All Sails caching. The menu clones Other and fits seven categories
inside its original six-row footprint. Spritsails appear only in their category.
Native browsing uses a family pager sized to the available buttons; the optional
All Sails adapter builds filtered 12-entry pages in its typed cache. Selection
and reopening reset the page; empty/shrinking lists clamp it. All Sails has no
hard assembly reference, but its installed reflection contract is checked.

A final family `AddNewSail` postfix runs after SE and type-specific sizing, sets
the installation coordinate to `GetScaledHeight()`, then calls native
`MoveHeldSail(0)` to refresh attachments, order and collision. This places the
foot at the mast base; coordinate zero would put it below the mast. Existing
sizes and ordinary vertical adjustment are retained. SE's `rotatablePart` is
explicitly cleared; its rotation buttons and no-target guards handle the rest.

### Marks and donor assets

| Type         | Mk.A / Mk.B IDs | Construction donor                        | Controls                                   |
| ------------ | --------------- | ----------------------------------------- | ------------------------------------------ |
| Loose-footed | **404 / 405**   | Brig jib **110**, `sharedassets15.assets` | Two independent clew sheets and deployment |
| Boomed       | **406 / 407**   | Full gaff **15**, `sharedassets1.assets`  | One boom sheet and deployment              |

Base width is jib 110's `installHeight / 3`, with luff **1.6 × width** and foot
rising **0.08 × width** aft. Mk.A has a **105°** throat: peak reach **1.0 ×
width**, rise **tan(15°) × width** and projected area approximately **1.693975 ×
width²**. Mk.B starts from a **135°** throat and straight foot **1.25 times**
the head, then stretches horizontally by **30%** while retaining corner heights.
Its final angle/ratio differ from those base values. Boomed marks match their
loose-footed companions' deployed corners and default sizing. Camber and
collision clipping derive from the actual cut.

Templates own procedural cloth, shadow and spar meshes; the boom shares its
sail's spar mesh. Donors retain their Animator, WindCloth, audio and shadow
contracts. Gaff 15's `boom_gaff_top` supplies timber material for both types;
gaff **119** supplies the read-only `furled__sail_cloth_back` mesh and its
cloth/rope submeshes. Only the cloth material slot follows recoloring. No game
assets are redistributed or modified.

### Shared sprit and snotter visuals

The sprit is rigid, with a **1.03** thickness multiplier, end radii **85%** of
its middle radius and separate flat-cap vertices. Its fixed socket sits
one-quarter up the deployed luff, on the hinge line. The peak remains lashed
beside the tip; the purchase attaches **90%** along the sprit. Both types use
family spar, pocket, collar, obstruction and struck-bundle visuals.

The decorative pocket has curved cheeks, a lower cradle, mast band and bolts. It
uses native `mast_metal` or an owned dark fallback; its generated mesh is
instance-owned and disposed on destruction. It has no collider or Rigidbody. The
heel seats **1.1 sprit radii + 5 mm** beyond the rendered mast surface.
`SpritsailMastSurface` snapshots readable mast vertices; known unreadable Sanbuq
topmast **80** uses its authored taper. Other misses warn once per surface
instance and fall back to the capsule radius.

Seven luff ties and three-turn mast collars follow the posed luff. Mast collar
rope uses **80%** of normal diameter; the upper purchase coil retains normal
thickness. Each tie has its own mast-local surface-query cache slot, sharing one
mesh snapshot; changed origin/direction/scale misses the cache and a changed
mast replaces it. Socket queries retain a single slot. No runtime performance
improvement has been measured. Luff lines hide when struck, leaving the native
bundle's bindings visible; the purchase remains visible when its winch is
available.

### Sprit obstruction on one tack

Both types set `StarboardAffected = true`, matching the shipyard description.
`SpritsailObstruction` provides one smoothed state for force and local camber.
Installed apparent wind is `Wind.currentWind - shipRigidbody.velocity`: negative
boat-local X airflow arrives from starboard. Classification uses the boat frame,
a **0.035** lateral deadband (about two degrees), retained state inside that
deadband and a half-second transition. Calm, invalid, unbound, loading, disabled
and fully struck states reset the effect.

`ObstructedTackForceMultiplier` accepts 0–1, with non-finite values falling back
to **0.90**. At 1 it disables the extra force loss but retains visual shaping.
The affected tack reduces camber by up to **50%** near the sprit, fading to zero
attenuation at **15% of sail width**. This applies during deployed and
partial-reef poses, before loose-footed sheet flex, without sampling Cloth or
changing solver topology. These are tuning values, not measured aerodynamics or
a guarantee of spar/fabric contact clearance.

### Fitting, collision and force lifecycle

The highest eligible active guide comes from the carrying mast's connected
ancestry, which remains protected through removal previews. The guide must be at
least **5 cm above the fully raised purchase**; otherwise installation reports
`SPRIT HOIST REQUIRES A HIGHER MAST GUIDE`. Lower or resize the sail, or use a
taller supported mast. Native mast save slots and control mechanisms are
retained; no custom save schema is added. Save/reload behavior still needs
runtime validation.

Construct all collision children on the inactive template. Native
`ShipyardSailColChecker.Awake` initializes reporting, tags, layers and kinematic
bodies on direct children and requires each to have a collider. Remove whole
unused donor objects, not just their collider components. Both types now create
and pose their spar sweeps before activation; runtime fitting only updates them.
This fixes the boomed empty-child exception and the loose-footed late-created
sweeps that missed native reporting.

Checks sweep **24 panel strips** and **nine sprit poses**; loose-footed sails
add flex bounds and boomed sails add nine boom poses. Bounds include gathered
fabric and raised spars. These are sampled approximations, not continuous
collision coverage. Native angle checks step by 5° and can narrow each tack
independently. Paired sheets clamp after `JibAngleMaster.Update`; boomed sails
clamp after `RopeControllerSailAngle.LateUpdate`. Neither widens tighter limits.

Force orientation follows posed corners. A type-scoped prefix temporarily
substitutes projected exposed-area fraction for native unroll during force
calculation; a finalizer restores the control value even on exceptions. Folds,
lashing offsets and bundle thickness do not contribute exposed area. At unroll
**≤2%** (or invalid unroll), show the struck bundle with zero force; between
**2–98%**, use procedural skinned gathering; at **≥98%**, use initialized Cloth.
Reefing and tacks move existing bones, with render-state refreshes separate from
topology construction.

## Loose-footed spritsail prototype

Both marks share the jib's paired sheet controllers and coordinated deployment.
The control filter temporarily removes only loose-footed sails from native
binding, wraps other family filters and restores the full mast list last. Shared
winch allocation supplies native-first complete pairs, authored fallbacks and
the carrying mast's halyard group. Boomed sails remain on native binding.

The sprit pivots about its fixed quarter-luff socket (about **34°** from the
mast for fully deployed Mk.A at uniform scale). Pulling the purchase raises it
toward upright; easing spreads the sail. The throat and upper luff remain fixed;
the lower luff gathers toward the socket, and the clew moves inward/upward.
Foot/leech budgets constrain the pose, including wide shallow cuts whose struck
clew rests above the socket. Luff, peak and clew are solver-pinned; the free
edges can flex. Full deployment uses coupled foot/leech fitting.

### Loose-footed spritsail sheet flex

`LooseFootedSpritsailFlex` acts after the ordinary pose on both marks. The lower
peak–clew–tack triangle curves toward loaded sheets; squared clew barycentric
weights vanish along the peak-to-tack diagonal, leaving the upper panel fixed.
Maximum travel is **15% of scaled foot length**, fading with deployment. Native
paid-out/routed rope lengths estimate load over the last **10%** of slack;
loaded sides combine by direction and load, smoothing at **5/s**. Unavailable
controls contribute no load.

The fit budgets sampled curved foot/leech length against the undeformed pose,
including camber/folds, using bounded inward compensation and reduced travel. It
adds no edge length and changes bones only. Endpoint leaves and aerodynamic
frames follow the result; reef-area and force multipliers are unchanged. Lower
collision strips and culling bounds include the flex envelope. The load estimate
and travel limit remain visual tuning parameters.

## Boomed spritsail companions

Both marks retain the gaff's ordinary `angleControllerMid`, two-segment sheet
route and native `midAngleWinch`/`reefWinch` binding by mast order. The sheet
ends on a fresh leaf at the boom tip; boomed sails do not reserve paired sheets
or create custom winches. Original reef/topping-lift visuals are suppressed; the
native boom sheet remains visible when struck.

The boom pivots at the fixed tack, with the complete straight foot pinned to it
and foot camber/solver travel fading to zero. Reefing raises boom and sprit on
separate rigid arcs while cloth gathers at the mast. The throat and entire luff
stay fixed; head/leech chords may slacken but do not stretch. Interior folds
vanish at attachments. The struck bundle spans the tack to raised sprit tip.
There is no loose-footed sheet flex or additional boom control.

Templates set `reverseReefing = false`, overriding gaff 15's true setting.
Paying out/letting fly deploys; hauling in reefs against native **25** weight
resistance and **1.2** wind-load multiplier. Native winch input limiting makes
loaded hauling slower than quick release; no custom speed multiplier is added.
Boat-mass and price premiums are separate from this control resistance.

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
  transitions. `FishermansStaysailEdgeFit` retains support bow and coupled
  lower-corner fitting without moving the fixed head.
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

Stay variants prefer **70°** between the aft spar's downward axis and stay. If
that intersects above the connected forward spar, use its physical masthead and
a steeper stay. Preserve physical fore/aft ordering, exclude higher aft topmasts
from lower variants and store endpoints rather than infer them at runtime.

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
Fisherman's Stays. Loose-footed spritsails also use these controls, within their
Brig/Sanbuq gate; boomed spritsails use ordinary native controls instead. The
follow-up architecture cleanup is implemented.
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
custom stays, `Pair.Fore` for Flying Sails, carrying mast for loose-footed
spritsails). `SheetingWinchPlacementResolver` searches complete native
left/right pairs in authored source order and native array order. It discovers
inactive source rigs through the boat hierarchy without relying on the startup
`BoatRefs.masts` array. Missing or unmatched array entries are skipped with a
diagnostic. Centre sheets never become pair candidates.

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

`HalyardWinchGroup(mast, sources)` maps an exact requested active mast to an
ordered source list. Groups do not inherit across variants or ancestry;
unconfigured masts search only their own reef array. Authored groups search the
requested mast first, then verified associated stays. Source stays may be
unfitted, but the requested mast and selected winch's mounting support must be
active. Startup, allocation and retained-placement validation use the same
mapping.

Cog mast **57** uses **[57, 58]**, borrowing midstay2's reef seat after its own;
mast **8** uses **[8, 51, 65]**, where the two stay references alias one
distinct seat. Identity/alias reservations prevent duplicate claims.
Source-array changes and native reclaim invalidate borrowing; never substitute a
proximity or category-wide search.

`tests/GeometryChecks/Controls/HalyardMounts.txt` records support eligibility;
`tests/GeometryChecks/FishermansStay/NativeWinchSeats.txt` records control
identities and parent-local poses. Groups are authored per requested section,
including topmast seats on lower supports, without borrowing unrelated physical
mast variants' ordinary reef arrays.

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
sources, then its valid fallback templates. All custom paired-control ownership
paths use this policy, independently of geometry donors. Template selection
ignores occupancy and does not reserve seats; halyard templates follow the
requested mast's authored group order, or use that mast alone when no group is
configured. A missing template reports its category or requested mast and
preserves the existing registration rollback. Native startup still receives
initialized control arrays: installed `Mast.UpdateControllerAttachments` indexes
them directly and calls `GPButtonRopeWinch.AttachToController`.

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
prefab/stay IDs, save ordering and saved geometry are unchanged. During complete
seat exhaustion, ropes can still appear at the boat origin; this remains a known
visual limitation.

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
and loose-footed spritsails request their mounting mast; staysail Mk.A/B/C
request the aft base; native sails on custom stays request the stay's aft
halyard source. Topmast seats on an active lower support retain their native
association. There is no inferred alternative-mast search, generated offset or
manual halyard fallback.

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

Overlay and capture share the read-only boat-surface picker, including displaced
walking-model conversion and world obstruction checks. The overlay's separate
inventory includes inactive controls and never reserves seats. Discovery and
cached shapes refresh once per second while shown; transforms and occupancy are
read at camera rendering time. Readable meshes supply triangle edges;
unreadable/empty meshes and skinned renderers use bounds, which are approximate.

Only the main camera draws. The owned `Hidden/Internal-Colored` material
disables depth testing/writing; missing shader/pass support leaves the overlay
off with a diagnostic. Native assets, bindings and activation states remain
untouched. Menus hide it; loading, leaving play, losing the boat or disabling
releases the selection/resources. Reselect after loading. Hidden/disabled
overlays do not scan.

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

## Runtime validation

### Automated baseline and limits

The latest recorded **0.3.0-dev** implementation checks on **2026-10-02** passed
Release with zero warnings/errors, both suites, CSharpier, Prettier and
`git diff --check`. `HarmonySignatureChecks` currently expects **113** patch
targets. This records the preceding code validation, not a new runtime session
or a test run performed for this documentation edit.

| Area                       | Coverage and limitation                                                                                                                                                                                               |
| -------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Geometry and forces        | All sail cuts, skin weights, coupled edges, fixed-head/reef poses, spritsail flex and obstruction, collision bounds, force inputs and finite fallbacks; no Cloth simulation                                           |
| Shipyard and compatibility | IDs/rollback, native IL contracts, patch ordering, filtering/restoration, category paging/scaling, spritsail pre-Awake collider construction, SailInfo reflection and naming; no live menus or Unity initialization   |
| Winch placement            | Seven profiles, 93 stays, 60 halyard groups, exact sources/aliases, atomic pairs, fallbacks, native reclaim, stable reservations, retries, bootstrap and teardown structure; no Unity exception/destruction execution |
| Visuals and diagnostics    | Knot channels, collars, mast-surface query slots, displaced walking frames, overlay occupancy and resource ownership; no rendering, audio startup or interactive validation                                           |

The placement cleanup's synthetic **600-frame / 60 Hz** single-pair fixture
reduced modeled scan requests **1,200 → 20**, candidate builds **600 → 1** and
ledger entries **1,200 → 2**. These are scheduling counters against an
inspection-derived old-behavior model, not Unity hierarchy calls, frame timings
or total allocation measurements. The separate installed-asset collar audit
covered **160 cases** (both ends of 40 variants in visual/walking forms); it
does not establish visual acceptance.

### Confirmed game observations

- **Placement, 2026-09-27:** the user accepted the native placement redesign as
  valid and complete. Brig tests placed Mk.C and a native sail on custom
  stay 144. Jong logs covered eight owner instances across recreation, with a
  final Flying Sail and two Mk.B staysails. All used native seats; inspected
  BepInEx and Unity logs showed no exhaustion/binding failures and
  installed/local DLL hashes matched. This predates the later architecture
  cleanup.
- **Cog mast 57, 2026-09-28:** the user confirmed placement/routing with
  midstay2 absent and the gaff occupying the primary reef winch. No fresh log
  was independently inspected; this does not validate the later mast 8 change,
  reclaim, support changes or reloads.
- **Overlay, 2026-09-28:** the user confirmed operation but found occupied
  amber/yellow hard to distinguish. Occupied traces now use red; that color
  change has no recorded visual confirmation.
- **Spritsail development:** the user accepted the thicker blunt sprit, improved
  fixed-pivot motion, iron fitting and added ropes. A lower-panel wrinkle
  disappeared when easing the sheet and was accepted as tension-dependent. The
  later 20% thinner mast collars, Mk.B and boomed additions, and cache changes
  do not inherit full validation from those observations. The reported zero
  port-tack efficiency could not be reproduced; no speculative force fix was
  made.
- **Boomed regressions:** the user reported installation stuck at “checking
  collision...” and deployment requiring loaded cranking. The Unity log
  confirmed a native checker Awake exception from empty collider children. Code
  fixes address construction timing and reverse reef control direction;
  successful installation and corrected timing still need an in-game retest.

These observations cover specific configurations, not all boats or lifecycle
transitions. The winch redesign remains accepted; remaining checks concern
regressions and later changes rather than unfinished original placement work.

### Runtime checklist

Start on **Brig**, then the affected boats, especially **Sanbuq** for cloth,
tack and mast-surface work. Spritsails remain restricted to those two boats.

1. Fit and resize all affected cuts at several heights, with mixed native/custom
   sails. Check collision completion, spar obstructions, asymmetric trim stops,
   recoloring, category paging and reopening documents. Compare equal-size
   boomed/loose-footed price and boat-mass calculations.
2. Exercise deployment, partial reversals and full strike/set transitions. Check
   fixed mast pivots, gathering, bundle/rope continuity and procedural-to-Cloth
   transitions. Boomed release must deploy; loaded hauling must reef more
   slowly.
3. Tack at full/partial deployment and both trim limits. Inspect inversion,
   detachment, spar penetration, force/audio discontinuities, socket/collar
   seating and sheet flex. Under controlled wind, heading, area and deployment,
   verify the configured starboard penalty; multiplier 1 should remove only that
   loss.
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

Each script shows the latest **5,000 lines**, then follows new output with
`tail -F`, including when a game restart truncates or replaces the log. Missing
files are retried. Paths use the default Steam installation beneath `$HOME`.
Press **Ctrl+C** to stop. These commands only display logs; they do not archive
them.

Capture logs before restarting a freeze. Distinguish other mods' exceptions and
suspected causes from confirmed evidence. Prefer `rg`/`rg --files`; inspect
screenshots with the local image viewer. Temporary IL/asset inspectors under
`/tmp` are disposable, not repository dependencies. Verify their code, assembly
paths and cache freshness before reuse; inspect installed assemblies/assets as
the behavioral reference rather than assuming upstream source matches.
