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
| `src/Sails/Spritsail/`                      | Category identity, family catalog, native control binding, balance rules, shipyard browsing and optional All Sails integration    |
| `src/Sails/Spritsail/LooseFootedSpritsail/` | Shared loose-footed rig, geometry and patches; `MkA/` and `MkB/` supply shape and identity                                        |
| `src/Sails/Spritsail/BoomedSpritsail/`      | Independent boom-supported rig, native single-sheet controls and lifting-boom deployment; `MkA/` and `MkB/` supply companion cuts |
| `src/Stays/FishermansStay/`                 | Independent mounts, registration, previews, controls and save compatibility                                                       |
| `src/BoatRigs/`                             | One class per boat owns supports, stays, mast ancestry, sheet categories and fallbacks                                            |
| `src/Controls/`                             | Native seat discovery, separate sheet/halyard resolvers, atomic reservations and owned control cloning                            |
| `src/Visuals/`                              | Route-only rope rendering; family code retains all routing, visibility and control decisions                                      |
| `src/Utils/`                                | Optional in-game diagnostics, profiling and their geometry helpers                                                                |

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
- Fisherman families resolve authored, connected **active** mast
  sections/guides. Spritsails resolve their carrying mast and native assigned
  guides. Registration and previews can precede activation; protect occupied
  stays and actual support parts.
- Clamp fisherman sail travel to **±40°** and spritsail travel to **±89°** after
  native controller updates, preserving tighter limits without snapping
  transforms. Paired sheets use `JibAngleMaster.Update`; boomed spritsails use
  the ordinary angle controller's `LateUpdate`.
- Each family retains its iterative order-text guard before NANDFixes. HarmonyX
  runs later prefixes even after `false`: append wrapped lines to the native
  list and consume input so later prefixes cannot recurse on it.
- New sails use native white palette **11** and SE's named plain texture
  **ParticleCloudWhite**. Preserve saved colors, recoloring and the hidden
  color-reference renderer; scope plain texture/selector/material guards to
  custom sails. Never change donor/shared assets. With **Shipyard Expansion
  0.12.1**, texture selections and allowed options are strings, and the catalog
  is a name-keyed dictionary. The compatibility patch registers the native plain
  texture before prefab setup without changing other catalog entries. The former
  numeric texture-zero ordering helper is no longer used. This build targets the
  new API and does not support SE 0.11.1.

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

## Custom rope rendering

Flying Sail sheets, shared spans and fixed ties, plus both Spritsail types'
custom sheets, lashings and snotter purchases/collars, follow the native
`Settings.clothRopes` option. When enabled, `RoutedRope` builds a six-sided tube
with smooth radial normals along each family's existing route. When disabled, it
uses the original line material, colors and flat renderer. The native boomed
Spritsail sheet and Fisherman's Staysail ropes retain their native rendering.

The authored Spritsail luff mount's seven rope meshes remain part of the mount
in either setting; they use the same native 3D rope material.

The installed plain `3d rope` donor was inspected on **2026-10-07**:
`sharedassets24.assets` renderer **4503**, mesh **1475**, material **64**
(`rope static`). It has **66 vertices**, eleven six-vertex rings, a **0.02 m**
source radius, smooth radial normals and no UVs or assigned textures. Custom
tubes reuse this donor's material and shadow settings at runtime and retain
their existing authored world-space diameters, including spritsail thickness
multipliers. They do not copy the flat-line texture onto the tube.

Family code submits the final posed route before selecting visibility. The
rendering component owns one lazily created mesh per live rope, reuses its
buffers/topology and compensates for the complete parent transform. Invalid
routes never upload a tube mesh. After a failed pose, the next valid upload
compares against the mesh vertex count to rebuild indices even if CPU buffers
were already resized. Missing donor materials warn once, use the flat fallback
and retry. Mode changes select only one representation; disable hides both, and
destruction releases only the generated mesh. Shared native meshes/materials and
sail Cloth are untouched. This rendering primitive does not merge family
mechanics or resume the deferred calculation extraction.

The user's **2026-10-07** report of flat-looking Flying Sail/Spritsail lines
matches the previous implementation: custom routes always used flat lines and
suppressed native 3D visuals. The new geometry checks cover diameter, normals,
winding, coils, bends, translations and degenerate routes; assembly checks cover
native setting/material access, route integration, resource ownership and
physics isolation. These do not establish visual acceptance. Validate on Brig
first, then Sanbuq: compare with adjacent native ropes from several camera
angles, toggle 3D ropes both ways, resize, trim/tack, reef/strike, lose/recover
supports, preview/cancel shipyard changes and reload. Check both Spritsail types
and marks, coil continuity, knots, shading, thickness, duplicate/missing lines
and cleanup. Runtime switching, Unity rendering and performance remain
unverified.

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

All four marks fit ordinary physical masts directly from native mast data,
including raked masts and boats without a MoreSailwindSails profile. Native
square-only and stay-only restrictions remain. A usable capsule collider, active
native rope guides, sufficient guide clearance and native control slots are
required. No aft mast or Fisherman's Stay is needed. Availability does not
establish in-game validation of each rig.

Both types use native `Mast.UpdateControllerAttachments` binding by sail order:
boomed sails use `midAngleWinch`, loose-footed sails use `leftAngleWinch` and
`rightAngleWinch`, and both use `reefWinch`. Category 6 and `squareSail = false`
remain unchanged; loose-footed sails do not share square-topsail trim. There are
no spritsail winch clones, borrowed fittings or profile fallbacks.

The binding guard runs after Fisherman filters and records the native slot and
its assigned guides before the full sail order is restored. It leaves sails in
the list. Missing or conflicting slots temporarily suppress only that
spritsail's controller/attachment references; a finalizer restores the
references and keeps unbound controllers inactive without deleting their input
state. Native square sails claim pair zero even above a loose-footed sail: this
conflict is rejected. Malformed mast winch arrays skip native binding with a
diagnostic because native setup dereferences every entry. Other native
exceptions still propagate.

Guides come from the carrying mast's indexed `mastReefAtt` and optional
`mastReefAttExtension`, falling back to the former when no extension is
assigned. These array names describe native routing roles, not height: order the
assigned pair along the upward mast axis in boat space. The higher guide sets
luff/hoist clearance and the purchase route's upper turn; the lower guide
supplies its preceding turn. There is no search through other slots or mast
sections. Runtime ancestry identifies actual mast/guide-owning parts for removal
and preview protection. Clearance is measured along the mast axis, and the
existing luff/hinge frame follows rake. Horizontal/degenerate axes remain
invalid for height-based fitting.

Existing prefab IDs, installation coordinates and save fields remain unchanged.
Loaded arrangements that depended on borrowed fittings or higher profiled guides
may require refitting. No save migration or automatic repositioning is
performed.

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

The rigid sprit has a **1.03** thickness multiplier, end radii **85%** of its
middle radius and separate flat-cap vertices. Its bolt pivot sits one-quarter up
the deployed luff, initially starboard of the mast. The sheeting hinge lies on
the **mast axis**: tacking rotates the sleeve, bolt, sprit and offset luff
around it. Reefing pivots the sprit about the bolt; equal side-lashing offsets
at both ends keep it **perpendicular to the bolt**, including when struck. The
peak remains lashed beside the tip, and the purchase attaches **90%** of the
distance from bolt to tip.

The forward end extends **12 inches (0.3048 m)** along the sprit from the bolt,
regardless of sail size or reef pose. Deployment's `Heel` denotes the bolt; only
the timber and deployed collider extend forward. The sleeve stays centered on
the mast. The lower mounting follows boat movement and installation height, but
uses the boat's neutral starboard direction projected perpendicular to the mast,
so it does not follow tacks. The authored luff mount and struck bundle follow
the rotating sail frame.

`SpritsailSnotterGeometry` fits the authored parts to these dimensions:

- Sleeve opening: **5 mm** minimum clearance from the sampled mast radius.
- Luff: **3 mm** outside the sleeve envelope. The bolt pivot adds one maximum
  sprit radius, matching the peak's side-lashing offset.
- Lower mounting: **outside diameter = mast diameter + 8 inches (0.2032 m)**.
  Its radial scale is independent; its vertical scale matches the sleeve,
  preserving their junction at source Y = -1.
- Bolt: the head begins **1 mm** beyond the tapered sprit surface at the pivot;
  its tip projects **5 mm** beyond that surface. Shaft and head lengths are
  fitted separately, with thickness based on the sprit's local radius.

Each instance lazily owns two rigid meshes: sleeve/bolt and lower mounting, with
wood and metal submeshes in each. Topology and UVs are initialized once;
positions and inverse-transpose fitted normals update when dimensions change.
Only tangents and bounds are recalculated, preserving Blender's smooth/sharp
normals. Each visible rig `LateUpdate` submits world frames through
`Graphics.DrawMesh`, with shadows and light probes enabled. This avoids ordinary
per-frame mesh uploads and distortion from nonuniform parent scale. There are no
detached renderer objects, colliders or Rigidbodies for the fitting; both meshes
are disposed with their instance. Mast alignment reuses its resolved axis for
`AftDirection` in both spritsail families.

`SpritsailMastSurface` snapshots readable mast vertices; known unreadable Sanbuq
topmast **80** uses its authored taper. Other misses warn once per surface
instance and fall back to the capsule radius.

The [authored luff mount](#authored-sail-mount-runtime) replaces the seven short
ties and three-turn mast collars. It hides when struck, leaving the native
bundle's bindings visible. The upper purchase coil retains normal rope thickness
and follows snotter visibility; the purchase line additionally requires an
active reef winch. Socket queries retain a single cache slot.

#### Snotter materials

`SpritsailSnotterMaterials` couples native donor selection with UV mapping. It
searches the specified prefab's mesh renderers, including inactive children:

| Face tag   | Native material and donor               | UV policy                                 |
| ---------- | --------------------------------------- | ----------------------------------------- |
| `DarkWood` | `dhow_medium_paint`, sail prefab **62** | Map 0–1 into U 0.350–0.352, V 0.870–0.900 |
| `Metal`    | Lantern `metal2`, item prefab **114**   | Preserve exported UVs                     |

The wood patch is the dark-brown vertical mast trim in Sailwind 0.39's Sanbuq
atlas. Keep exported wood UVs inside **0–1**. Native materials and their shader
settings remain unchanged; the sprit retains its gaff timber material.

Missing donors log a warning and use untextured fallbacks built with the gaff
material's Standard shader, without copying its wood maps. Wood uses matte RGB
**(0.12, 0.065, 0.035)**. Metal matches lantern `metal2`: RGB **(0.11035956,
0.14488259, 0.1509434)**, metallic **0.51**, smoothness **0.4**. The creating
template owns and disposes fallback materials; its clones share them. Native
materials are neither modified nor destroyed.

#### Authoring and baking the snotter

Keep all three objects in `assets/snotter/snotter.obj`, named exactly
**`Sleave`**, **`Bolt`** and **`Mounting`**. Use Y up, with the bolt pointing
along -X and centered at Y = Z = 0. Export UVs, finite unit normals and material
assignments. Tag every face `DarkWood` or `Metal`; numeric suffixes such as
`.001` are accepted. The baker reads OBJ `usemtl` tags, not MTL colors or
textures, and rejects unknown or missing assignments.

The baker measures the sleeve's inner facets and outer envelope, mounting radius
and bolt endpoints. Set `boltHeadInner` in `assets/snotter/snotter.fit.json` to
the shaft/head transition's **negated source X coordinate (`-x`)**; it must lie
between the bolt endpoints and match an exported bolt vertex plane. Remodeling
updates the source and marker rather than C# measurement constants; physical
clearances remain runtime policy.

After exporting, run from the repository root:

```sh
blender -b --python tools/convert_snotter.py
python -m unittest discover -s tests/AssetChecks
```

The converter uses Blender triangulation, preserves UV seams and per-corner
normals, and rejects degenerate triangles or winding inconsistent with normals.
It removes exact duplicate faces with matching positions, UVs, normals, part and
material. Coincident faces within a part that differ in attributes produce a
warning naming the object and OBJ lines; both faces remain for author review.
The current source retains overlapping metal quads on `Mounting` at lines **861
and 876** with different shading attributes. Resolve them in Blender if
unwanted; inspect for flicker or shading artifacts in game.

The current bake has **3,164 vertices and 1,634 triangles**: **164 DarkWood**
and **1,470 Metal**. Internal format `MSN6` uses a 100-byte header with counts,
six fit dimensions and SHA-256 hashes of the OBJ and marker file; each vertex
stores position, UV, normal and part in 36 bytes, and each triangle stores three
indices and a material in 16 bytes. GeometryChecks detects changed source or
marker files without a rebake. Keep the OBJ, marker and generated
`snotter.bytes` in version control. Ordinary .NET builds embed the checked-in
bytes and require neither Blender nor loose runtime assets.

#### Snotter validation

The branch's **0.3.0-dev** Release build passed with zero warnings/errors; asset
checks, GeometryChecks and AssemblyChecks passed. Checks cover source freshness,
material/topology preservation, independent mesh data, fitted normals and UVs,
dimensions across mast/sprit sizes, boat motion, tacking, reefing, the
perpendicular sprit and 12-inch extension. Assembly checks guard mesh ownership
and the separation of refitting from ordinary drawing.

The user confirmed sleeve, bolt and sprit placement and accepted the 8-inch
mounting allowance. Their **2026-10-06** wood-grain feedback established the
dark-brown mast trim as the appearance reference. The current finishes, smooth
shading and rigid draw path still need in-game verification; automated checks do
not simulate Unity Cloth, establish rendered appearance or measure frame-time
gains.

Use the [mount and visual-cache checklist](#mount-and-visual-cache-validation)
for shared rig validation. Also inspect the fixed mounting, rotating
sleeve/bolt, sleeve/mounting junction, 12-inch extension, material regions and
overlapping faces. Verify collision clearance and fit around tapered or
noncircular masts.

### Authored sail mount bake

`assets/sail_mount/SailMount.obj` contains the luff edging, seven eyelets and
seven mast ropes. Bake it from the repository root:

```sh
blender -b --factory-startup --python-exit-code 1 --python tools/convert_sail_mount.py
python -m unittest discover -s tests/AssetChecks
```

The baker writes `assets/sail_mount/sail_mount.bytes` using Blender polygon
triangulation. It preserves positions, UVs, corner normals and part identities,
sharing identical vertex attributes only within a part. Loose OBJ lines are
omitted. Missing parts, invalid indices, nonfinite attributes, nonunit normals,
degenerate triangles and inconsistent winding fail before output is written.
Keep the OBJ and baked bytes together in version control; AssetChecks verifies
source freshness, triangle counts, material regions and authored attributes.

Use object names `LuffEdge`, `Eyelet_01` through `Eyelet_07`, and `Rope01`
through `Rope07` (an underscore before the rope number is also accepted). Each
eyelet needs both `Brass` and `ThickCloth` face tags; numeric material suffixes
are accepted. Strip and rope slots come from object identity. MTL files and
textures are unnecessary because runtime materials come from native assets.

Internal format **MSL1** is little-endian: a 44-byte header contains four magic
bytes, uint32 vertex/index counts and the source OBJ's SHA-256. Each 36-byte
vertex stores position, UV, normal and uint32 part ID; each 16-byte triangle
stores three uint32 indices and a uint32 material ID. Part IDs are **0** for the
strip, **1–7** for eyelets and **8–14** for ropes. Material IDs are **0
SailCloth**, **1 Brass**, **2 ThickCloth**, **3 Rope**. The exported 6 m strip
runs along X, spans Z = -0.1 to 0 and lies at Y = 0; fitting and axis conversion
happen at runtime.

The current bake has **15,015 vertices**, **6,566 triangles** and **645,640
bytes**: **546 SailCloth**, **728 Brass**, **1,680 ThickCloth** and **3,612
Rope** triangles, with **112** loose line records omitted. The DLL embeds it as
`MoreSailwindSails.SailMount`; ordinary builds need neither Blender nor loose
runtime assets.

### Authored sail mount runtime

Both loose-footed and boomed Mk.A/B attach the mount to their existing posed
luff bones. The 10 cm strip extends forward from a 2 mm sail overlap; fitting
follows sail size and reef pose while keeping holes aligned with rigid eyelets.
Separate reverse faces/normals make the strip visible from both tacks. The mount
owns its mesh and does not modify live Cloth topology, bones, solver settings,
controls or forces.

Source rope loops have a Y/Z elliptical centerline centered at Z = 0.23 m, with
Y/Z radii 0.2/0.275 m. Each loop's baked X extent determines its tube diameter,
currently about 16 mm. Runtime fitting preserves this thickness and routes two
approach curves through each eyelet to a circular mast collar. The collar fits
the largest of 16 directional surface samples at that height, allowing taper but
potentially leaving gaps around noncircular sections. The 112 directional
queries share one mast snapshot with separate cache slots; mast/mesh replacement
refreshes the snapshot. Sampling uses the existing capsule fallback and Sanbuq
topmast taper.

Each mount lazily owns one mesh, four material slots and reusable fit buffers.
Immutable data keyed by luff bone count precomputes strip interpolation and rope
curve/normal coefficients. Fitting uses local transforms through the cloth and
mast's common ancestor to avoid subtracting large world positions. Uniform
ancestor scales cancel shared rotations; nonuniform ancestors retain stretch and
shear. Unrelated roots use the world-coordinate fallback. A rigid draw matrix
carries the fitted mesh with the cloth between rebuilds.

The rope fitter evaluates its hot path with component arithmetic, retaining the
previous operation order, normalization thresholds and finite-difference
tangents. This removes repeated Unity `Vector3` operator/property calls without
changing the curve, normals, topology, surface sampling or cache triggers.

Both cloth slots use the current sail material, following recoloring. Brass uses
prefab **103** (`103 mug metal gold`) material `metal gold`, inspected in
`sharedassets24.assets`, material **131**, on **2026-10-08**. Rope uses
`RefsDirectory.clothRopePrefab`'s skinned material. Materials are shared
read-only; missing materials log once per mount and retry. Generated meshes are
disposed with their instance. No native assets are redistributed.

#### Visual caching

`SailVisualRevision` and `SailVisualCache` under `Visuals` provide a reusable
revision and independent consumer acknowledgements. `SpritsailVisualTriggers`
reads native inputs once per rig visual update. A revision changes on:

- Sheet controller paid-out `currentLength` or identity changes, and reef
  `currentUnroll` changes.
- A tack-side change, using boat-relative sail orientation and a ±5° hysteresis
  band that retains the previous side inside the band.
- Authored corner dimensions, installation height, local sail/ancestor scale
  products, or carrying mast/shared-mesh identity changes.
- Initial fitting, activation or rig reactivation.

Control and fitting values use exact comparisons. World translation, boat
rocking/heading, wind load, same-side native sway and sampled radius changes do
not invalidate the revision. Temporary visual misalignment during sway or
control settling is an accepted tradeoff until the next trigger.

On a pending revision, the mount samples all seven mast envelopes, refits the
complete mesh and uploads positions, normals, tangents, bounds and reverse-strip
data. Reuse skips sampling, fitting and upload while continuing draw submission
and material lookup. The sprit retains its local pose/scale. The snotter retains
its sail-relative rotating frame, mast-relative fixed mounting and rope routes;
its dimension thresholds still suppress unnecessary mesh uploads during a
rebuild.

The snotter draws its purchase and collar through `RoutedRope.DrawCached`.
Cached tubes follow their anchor through draw matrices; flat ropes update world
points from cached routes. Settings changes and material retries use the current
transported route. Other routed-rope consumers retain live route updates.
Private serialized renderer/mount references preserve template cloning; route
setters invalidate the rope cache. The collar only poses its route, leaving
drawing to the snotter.

Each consumer acknowledges only its own successful update; hidden, bypassed or
failed consumers retain pending work. Purchase reactivation refreshes its route.
Mounts hide while loading, unsupported or struck. Physics, native controls,
aerodynamic posing, live Cloth, clew sheets, the boomed boom and furled visuals
continue updating independently.

#### Mount fitting benchmark

Use [Performance profiling](#performance-profiling) for live CPU/frame captures
and visual bypass comparisons.

The offline benchmark compares the current full fitter with the frozen original
and the previous cached fitter on identical inputs, excluding initialization and
Unity mesh upload/rendering:

```sh
nix develop -c dotnet run --project tests/GeometryChecks -c Release --no-restore -- --benchmark-mount
```

After 100 warmups, seven alternating rounds of 50 fits on **2026-10-09**
measured **0.774 ms/full fit**, compared with **2.731 ms** for the previous
cached fitter (**3.53× faster**) and **3.946 ms** for the original. Fifty warmed
optimized fits allocated **zero managed bytes**. These .NET timings do not
predict Unity/Mono frame rate.

Release and both check suites passed with zero build warnings/errors. Across the
**288** geometry fixtures and alternate luff layouts, fitted vertex and normal
components exactly match the previous cached implementation.

The subsequent Junk capture on **2026-10-09**, with the matching installed DLL,
recorded **693 rebuilds** over approximately **210 seconds** of sheeting and
reefing. Mean fitting time per rebuild fell from **7.85 to 2.74 ms** for the
loose-footed sail and **7.78 to 2.19 ms** for the boomed sail. The previous run
had only 89 rebuilds, so these are per-rebuild comparisons, not matched-workload
FPS evidence. No exceptions appeared during the capture. Boomed surface queries
then averaged **3.84 ms/rebuild**, peaking at **10.17 ms**; uploads averaged
**1.2–1.4 ms/rebuild** across the two types. Visual clearance, both tacks and
NORMAL/BYPASS resumption have not yet been confirmed for this fitter.

#### Mount and visual-cache validation

The **0.3.0-dev** cleanup on **2026-10-09** passed Release with zero
warnings/errors, both C# suites, all eight asset checks and formatting checks.
GeometryChecks compares the fitter against the frozen reference across **288**
cut/type/size/reef/rake/radius combinations: positions within **0.01 mm** and
normal-vector differences below **0.001**. It also checks seams, hole alignment,
rigid eyelets, mast clearance, alternate bone layouts, relative frames and
allocation-free warmed fitting. Cache checks cover exact controls, tack
hysteresis, fitting/support/reactivation, independent pending consumers and zero
allocations across 1,000 warmed state samples. AssemblyChecks guards
integration, ownership/serialization, upload ordering, disabled diagnostics and
separation from live mechanics. Neither suite simulates Unity rendering or
Cloth.

Earlier live captures showed repeated mount fitting, especially rope fitting, to
be a substantial CPU cost. The subsequent Sanbuq ABAB test supported keeping
deliberate-state caching, but the user still reported worse perceived frame rate
with two spritsails. The remaining cause is unresolved. Temporary staysail-angle
and Spritsail-effectiveness issues cleared after restarting; no persistent
regression was established. Superseded numerical-cache measurements and
implementation history remain in Git.

Validate the cleanup on **Brig** with all four sails, then **Sanbuq** and
affected masts. Check continuous sheet/reef input, both tacks, resizing,
installation height, mast replacement, hide/show, preview/cancel and
save/reload. Inspect seam and eyelet alignment, rope passage, timber clearance,
recoloring and shadows, especially crowded lower eyelets. Exercise F7 resumption
and flat/3D rope toggling; confirm sprit/snotter/purchase placement. In steady
sailing, rebuild counts should stop growing and mount fitting/sampling/upload
time should approach zero. Targeted in-game confirmation of this cleanup remains
pending.

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

Both loose-footed and boomed marks accept raked and leaning carrying masts. The
physical capsule axis is measured in boat-local coordinates, so boat heel/pitch
does not change eligibility. The luff and hinge follow that axis; nonfinite,
degenerate or effectively horizontal axes cannot support the existing
height-based fitting calculation.

The native slot's higher guide must be at least **5 cm above the fully raised
purchase**, measured along the mast axis. Otherwise installation reports
`SPRIT HOIST REQUIRES A HIGHER MAST GUIDE`. Lower or resize the sail, or use a
mast with a suitable native guide. The actual guide-owning parts remain
protected through removal previews. Native mast save slots are retained; no
custom save schema is added. Save/reload behavior still needs runtime
validation.

Construct all collision children on the inactive template. Native
`ShipyardSailColChecker.Awake` initializes reporting, tags, layers and kinematic
bodies on direct children and requires each to have a collider. Remove whole
unused donor objects, not just their collider components. Both types create and
pose their deployed spar colliders before activation; runtime fitting only
updates them. This retains the fix for the boomed empty-child exception and
ensures every collider participates in native reporting.

Both variants use **24 panel strips, 0.10 m thick before scaling**, plus one
fully deployed sprit collider; boomed sails add one deployed boom collider. The
installed full gaff **15** uses narrow boxes about **0.145 m** thick;
standard/full junk **21/90** use **0.10 m** panel boxes with narrow edge boxes
about **0.149 m** thick. Spritsails follow that fixed deployed-shape approach:
no billow, sheet-flex or intermediate/raised reef poses expand shipyard checks.
The spar boxes retain their existing physical dimensions. Panel clipping and
mast-pivot alignment remain specific to each Spritsail cut.

Native angle checks step by **5°**: neutral obstruction blocks installation,
while off-center contacts narrow each tack independently. Paired sheets clamp
after `JibAngleMaster.Update`; boomed sails clamp after
`RopeControllerSailAngle.LateUpdate`. Neither widens tighter limits. Rendering
bounds separately retain nine reef samples, gathered fabric, raised spars and
loose-footed flex padding to preserve visibility throughout deployment.

Geometry checks require the expected active panel strips, including the
mast-adjacent strip, then verify thickness, cut clipping and scaling for both
marks/types. Assembly checks cover pre-Awake collider initialization and
separate rendering bounds. Live collision reports and retest targets are
recorded under [Runtime validation](#runtime-validation).

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
Their controllers bind directly to the carrying mast's native left/right sheet
and reef slots. Spritsails remain in the native binding list alongside boomed
and vanilla sails; Fisherman families retain their separate filters/allocation.

The sprit reefs about its quarter-luff bolt in the rotating mast fitting (about
**34°** from the mast for fully deployed Mk.A at uniform scale). Pulling the
purchase raises it toward upright; easing spreads the sail. The throat and upper
luff remain fixed within the rotating sail frame; the lower luff gathers toward
the socket, and the clew moves inward/upward. Foot/leech budgets constrain the
pose, including wide shallow cuts whose struck clew rests above the socket.
Luff, peak and clew are solver-pinned; the free edges can flex. Full deployment
uses coupled foot/leech fitting.

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
frames follow the result; reef-area and force multipliers are unchanged. Culling
bounds include the flex envelope; shipyard colliders use the thin deployed panel
described above. The load estimate and travel limit remain visual tuning
parameters.

The Junk profiling capture on **2026-10-09** measured sheet flex at about **1.29
ms/frame** during the first 4 minutes 20 seconds of sailing. It accounted for
most of the loose-footed rig's **2.05 ms/frame** custom CPU time; these
measurements exclude Unity's internal Cloth solver and GPU execution.

The optimized fitter retains the same 17 candidate directions, 18 bisection
steps, smoothing and edge budgets. Before searching a direction, it computes the
closest possible point on that candidate's complete displacement segment. A
candidate is skipped only when this lower error bound exceeds the best fitted
error, with a relative rounding margin to preserve near ties. Zero requested
movement returns before sampling edge budgets. No fitted displacement is cached
across frames: sheet tension and the underlying pose remain live inputs.

Authored-corner flex weights are computed during template setup and serialized
for live clones. Each frame still poses the base shape first; flex then skips
fixed bones and zero-displacement writes. This preserves relaxation, endpoint
posing and the aerodynamic frame without changing Cloth topology.

Release and both check suites passed on **2026-10-09**, with zero build warnings
or errors. **4,320** fixtures exactly matched a frozen copy of the original
solver across both marks, scales, reefs, camber, obstruction and sheet
loads/directions. Checks also cover original bone weights, template
serialization, pose ordering, edge budgets, mirrored tacks and zero managed
allocations over 1,000 warmed fits. The offline benchmark is available with:

```sh
nix develop -c dotnet run --project tests/GeometryChecks -c Release --no-restore -- --benchmark-flex
```

Seven alternating rounds over **2,880** active fixtures measured medians of
**0.0218 ms/fit** for the reference and **0.0099 ms/fit** optimized (**2.21×**).
These .NET solver timings exclude Unity bone updates and do not predict game
frame rate. Runtime validation remains pending: begin on Brig, then repeat the
Junk capture and check both marks under taut/slack/opposing sheets, tacks,
reefing, resizing and save/reload. Compare the `SheetFlex` timing with identical
profiling selections and camera conditions.

## Boomed spritsail companions

Both marks retain the gaff's ordinary `angleControllerMid`, two-segment sheet
route and native `midAngleWinch`/`reefWinch` binding by mast order. The sheet
ends on a fresh leaf at the boom tip; boomed sails do not reserve paired sheets
or create custom winches. Original reef/topping-lift visuals are suppressed; the
native boom sheet remains visible when struck.

The boom pivots at the fixed tack, with the complete straight foot pinned to it
and foot camber/solver travel fading to zero. Reefing raises boom and sprit on
separate rigid arcs while cloth gathers at the mast. The throat and entire luff
stay fixed within the rotating sail frame; head/leech chords may slacken but do
not stretch. Interior folds vanish at attachments. The struck bundle spans the
tack to raised sprit tip. There is no loose-footed sheet flex or additional boom
control.

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

Cut construction accepts finite head slopes from **0° to 80°**, with positive
luff and leech geometry. Mk.C keeps its fixed `1.5 × width` luff. All authored
stays rise in their forward-mast frame.

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

| Boat                         | Part groups | Registered stays | Forward-masthead fallbacks |
| ---------------------------- | ----------: | ---------------: | -------------------------: |
| Brig                         |           2 |               24 |                          4 |
| Junk                         |           2 |                9 |                          2 |
| Jong                         |           5 |                9 |                          0 |
| Sanbuq                       |           2 |               26 |                          5 |
| Cog                          |           1 |                3 |                          0 |
| Shroud                       |           2 |                8 |                          2 |
| Large dhow (Sailwind 0.39)   |           2 |               14 |                          8 |
| Gloriana (OldChronian 0.6.0) |           2 |                3 |                          1 |
| Chronian (OldChronian 0.6.0) |           2 |                2 |                          0 |
| Caelanor (OldChronian 0.6.0) |           1 |                1 |                          0 |
| Kakam (90)                   |           1 |                2 |                          0 |
| Dhow (10)                    |           1 |                1 |                          0 |

The twelve profiles provide **102** stays across **103** mast-pair
configurations. Fisherman support for OldChronian is profile-based with no hard
mod assembly dependency; Gloriana, Chronian and Caelanor have profiles.
Spritsails do not require profiles, including on Gallus. Exact vectors,
prerequisites and ordered control-source lists live in each profile, backed by
numeric installed-asset fixtures.

**Leopard has no Fisherman support:** its BoatRig profile was removed after
in-game compatibility problems. Profile gates reject Flying Sail fitting and
skip Fisherman's Stay registration on Leopard; no save migration is provided.
Spritsails use its native mast fittings under the same profile-independent rules
as other boats. Future compatibility work is tracked in
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

Append groups/options after SE initializes customization; never reorder save
slots. Reserve mount IDs **128–255** and expand capacity to **256** without
shrinking larger arrays. Missing old-save/cancellation entries restore **None**.
Validate profiles and occupied IDs before construction; roll back a boat's new
stays on failure. Protect occupied stays/supports during invalid previews and
restore preview state in a finalizer. Normalize stay and walking geometry
independently to the same endpoints; donor bounds may differ from `mastHeight`.

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

### Gloriana (OldChronian)

[Gloriana.cs](../src/BoatRigs/Gloriana.cs) profiles OldChronian 0.6.0's
`gloriana` bundle, identity `BOAT GLORIANA (182)`. Physical bases are foremast
**1**, mainmast **2** and lower mizzen **3**; upper mizzen **4** requires **3**.
Flying Sail supports cover fore/main and main/mizzen.

Two stay groups supply mounts **128–130**. Fore/main meets the rendered foremast
tip at local z **1.608888**, about **67.195°** from the aft spar's downward
axis. Main/lower-mizzen and main/upper-mizzen use **70°** at native gaff-guide
heights. The lower variant excludes section 4; the upper requires both mizzen
sections. Native forestay **5** and mizenstay **6** supply stay geometry/control
templates without needing to be fitted.

Lower-mizzen halyards try sources **3 → 4 → 6**: `halyard_mizenmast1`,
`halyard_mizenmast2`, then `halyard_mizenstay`. All three seats are on the
permanent mizzen fife rail and may be borrowed from unfitted rigs when free.
Upper mizzen uses **4 → 6**. Mainmast **2** and lower mizzen **3** each have one
[measured reef fallback](#gloriana-reef-fallbacks); all sheet fallbacks are
null.

### Chronian (OldChronian)

[Chronian.cs](../src/BoatRigs/Chronian.cs) profiles OldChronian 0.6.0's
`aelasyl` bundle, identity `BOAT CHRONIAN (187)`. Physical chains are fore **2 →
3 → 15**, main **4 → 5 → 16**, and mizzen **6 → 7 → 17** (Main → Top →
T’gallant). Bowsprit 1 is excluded. Flying Sail supports cover both adjacent
mast pairs through their highest active sections.

Two Fisherman's Stays use prototype **12** and `AlignGuideHeightToAftAnchor`:

| Mount   | Mast sections                       | Required sections | Attachment and exclusion                                                                       |
| ------- | ----------------------------------- | ----------------- | ---------------------------------------------------------------------------------------------- |
| **128** | Foremast Top **3 → Mainmast Top 5** | **2/3/4/5**       | Anchors 2 cm below rendered wood tips; **8.482916°** rise in the foremast frame; no exclusions |
| **129** | Mainmast **4 → Mizzenmast Top 7**   | **4/5/6/7**       | Mizzen Royalstay **14** centerline extended to the mast axes; **24.5°** rise; excludes **14**  |

Only mount 129 borrows the native stay's attachment line; its construction still
uses prototype 12. The exclusion uses reciprocal red shipyard help in both
installation orders. Optional T’gallants neither replace nor disable these
stays. Six halyard groups associate each Main/Top section with its native stay
seats; T’gallants use their own arrays. All manual fallbacks are null.

### Caelanor (OldChronian)

[Caelanor.cs](../src/BoatRigs/Caelanor.cs) profiles OldChronian 0.6.0's
`caelanor` bundle, identity `BOAT CAELANOR (192)`. Its single Fisherman's Stay
**128** joins **Foremast Top 2 → Mainmast Top 6**, requiring **1/2/5/6**. This
uses the aft mainmast position; the Midmast alternative has no stay option.

Anchors sit **2 cm** below the rendered wood tips, excluding the taller offset
poles. The line rises **10.462292°** in boat coordinates and **13.462291°** in
the foremast frame. Construction uses prototype **14** and
`AlignGuideHeightToAftAnchor`. Optional T’gallants **17/18** neither move nor
disable the stay; there are no rigging exclusions.

Physical chains are fore **1 → 2 → 17**, aft main **5 → 6 → 18**, and alternate
main **3 → 4 → 19**. Flying Sail support covers fore/aft-main, including their
T’gallants. Both mainmast positions share one sheet category and native control
identities. Six halyard groups cover Main/Top sections; T’gallants use their own
seats. All manual fallbacks are null.

### Gallus (OldChronian)

OldChronian 0.6.0's `gallus` bundle, identity `BOAT GALLUS (197)`, offers **1
(Plumb)** and **4 (Raked, 15°)**. Both mast frames are eligible for all four
Spritsail marks subject to native fittings and clearance. Gallus has no boat
profile and receives no Fisherman's Stays or Flying Sail support. Its installed
mast measurements remain as regression fixtures for profile-independent fitting.

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
supports the boats listed in [Boat profiles and stays](#boat-profiles-and-stays)
and applies to Flying Sails, all three staysail cuts and native sails on
Fisherman's Stays. Both spritsail types use ordinary native controls
independently of these profiles. The follow-up architecture cleanup is
implemented. [Runtime validation](#runtime-validation) separates accepted
Brig/Jong evidence from later changes and remaining in-game uncertainty.

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

`HalyardWinchGroup(mast, sources, fallback)` maps an exact requested active mast
to an ordered source list. Groups do not inherit across variants or ancestry;
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
| Gloriana   |              4 |
| Chronian   |              6 |
| Caelanor   |              6 |
| Kakam      |              1 |

The supported catalog has **77 halyard groups**.

Unlisted masts retain their original lookup, including Jong's raked foremast and
unsupported bermuda variants. Cog covers both ordinary mizzen variants; the
other supported profiles cover their audited stay reef seats.

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

[Gloriana.cs](../src/BoatRigs/Gloriana.cs) defines two `HalyardFallbackSeat`s
for native-seat exhaustion. Exact contact vectors and template normals live in
the profile; capture provenance is:

| Requested mast     | Native sources first     | F9 capture / surface            | Clone template                       |
| ------------------ | ------------------------ | ------------------------------- | ------------------------------------ |
| Mainmast **2**     | **2** (three reef seats) | **2026-10-04 #2**, `MAST_MAIN`  | `reefWinch[1]`, `halyard_mainmast2`  |
| Lower mizzen **3** | **3 → 4 → 6**            | **2026-10-05 #1**, `FIFE_MIZEN` | `reefWinch[0]`, `halyard_mizenmast1` |

Both captures came from `walk cols/WALK GLORIANA/structure_container/`. Rotate
the template's local +Z normal onto the captured surface normal. The installed
coil's rear Z bound **−0.13091201** at scale **0.7** gives the authored outward
offset **0.09163841 m**; this is a measured mounting depth, not a runtime
clearance check.

Each seat is reserved as `halyard-fallback/<requested mast>` for one owner
across all custom halyard users. Template cloning may use an occupied native
original. New allocations prefer native seats; a valid fallback remains stable
when one frees. The requested mast must stay active, and retained placement
validates the template identity and pose. Support loss releases the claim and
hides/retries the existing controller.

### Sanbuq reef fallbacks

[Sanbuq.cs](../src/BoatRigs/Sanbuq.cs) defines four base-mast halyard fallbacks
from **2026-10-07** F9 captures in `BepInEx/LogOutput.log` and `Player.log`.
Existing native source groups remain first; topmasts and other mast alternatives
keep their existing sources.

| Requested mast      | Capture | Walking surface               |
| ------------------- | ------- | ----------------------------- |
| Mainmast 1 **10**   | 5       | `structure/mast`              |
| Mainmast 2 **11**   | 3       | `structure/mast_1`            |
| Mizzenmast 2 **55** | 2       | `SE_cols_sanbuq/mizzen_mast2` |
| Mizzenmast 3 **69** | 4       | `SE_cols_sanbuq/mizzen_mast`  |

Capture 2 supersedes capture 1 on mizzenmast 2. Exact contacts and normals live
in the profile. Each seat uses the requested mast's `reefWinch[0]`, whose local
+Z points toward boat **(0, 0, −1)**. The installed mesh rear Z bound
**−0.063862** gives offsets **0.08191725 m** for native mainmast templates
(scale **1.282723**) and **0.08191708 m** for SE mizzen templates (scale
**1.282720**).

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

[Kakam.cs](../src/BoatRigs/Kakam.cs) uses three **2026-10-07** F9 captures from
`BepInEx/LogOutput.log` and `Player.log`: capture 1 on
`mast_001/mast_holder_001` for the mizzen halyard, and captures 2/3 on
`trim_009` for the port/starboard sheets. Exact authored vectors live in the
profile.

Mainmast variants **5/6** use the aft sheet pair. An empty native source list
excludes the forward seats; mast **5** left/right angle controls at index **0**
still provide the clone templates. The captured distances from center, heights
and fore/aft positions are averaged into symmetric contacts; averaging the
normals gives both winches parallel orientations. The templates have identical
rotations and a rear Z bound of **−0.044704**, giving offset **0.044704 m** at
native scale.

Mizzen **7** tries its own reef array before capture 1's halyard fallback, using
`reefWinch[0]`. Its local +Z points boat-left. The mesh rear Z bound
**−0.063862** at scale **0.792** gives offset **0.05057871 m**.

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
`shipyard_expansion.assets`, `ShatteredSeasExpansion/veil piercer` and the
OldChronian bundles `gloriana`, `aelasyl`, `caelanor` and `gallus`. Include
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
array correspondence and parent-local poses across thirteen measured boats
(including the profile-free Gallus). These are reference measurements, **not
boat-local fallback vectors**. The inventory was read from installed assets and
SE's serialized part/option metadata; no proprietary assemblies or extracted
asset payloads are committed.

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

The **0.3.0-dev** baseline on **2026-10-07**, including profile-free spritsails,
OldChronian profiles, the architecture cleanup, Kakam/Dhow support, Dhow's
shared stay, and Kakam/Sanbuq fallbacks, passed Release with zero
warnings/errors, both suites, CSharpier, Prettier and `git diff --check`.
AssemblyChecks covers **114** Harmony targets against installed assemblies.
These results do not establish in-game behavior.

| Area                       | Coverage and limitation                                                                                                                                                                                             |
| -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Geometry and forces        | All sail cuts, skin weights, coupled edges, fixed-head/reef poses, spritsail flex and obstruction, collision bounds, force inputs and finite fallbacks; no Cloth simulation                                         |
| Shipyard and compatibility | IDs/rollback, native IL contracts, patch ordering, filtering/restoration, category paging/scaling, spritsail pre-Awake collider construction, SailInfo reflection and naming; no live menus or Unity initialization |
| Winch placement            | All profiled stays and halyard groups, exact sources/aliases, atomic pairs, fallbacks, native reclaim, stable reservations, retries, bootstrap and teardown structure; no Unity exception/destruction execution     |
| Visuals and diagnostics    | Knot channels, collars, mast-surface query slots, displaced walking frames, overlay occupancy and resource ownership; no rendering, audio startup or interactive validation                                         |

OldChronian profile checks cover measured endpoints/slopes, mast ancestry,
capsule acceptance, guide alignment, native controls and all three staysail
cuts. Availability checks include **1,024** Chronian support/exclusion
combinations, **512** Caelanor combinations, both Gloriana mizzen variants and
Gallus's absence of Fisherman's support. Spritsail checks cover native control
slots, conflicts and rake/lean acceptance, transform invariance and expected
active collision strips. Gloriana fallback checks cover native priority,
exclusive reservations, stable retention, support loss/recovery and
template/pose validation. Dhow checks cover one mount for both mast pairs,
coincident endpoints and all three staysail cuts. Kakam checks cover aft sheet
selection, symmetric parallel poses, startup templates and halyard mounting
depth. Sanbuq's fallback addition passed the existing suites; its new mounting
poses have no dedicated geometry test.

The placement cleanup's synthetic **600-frame / 60 Hz** single-pair fixture
reduced modeled scan requests **1,200 → 20**, candidate builds **600 → 1** and
ledger entries **1,200 → 2**. These are scheduling counters against an
inspection-derived old-behavior model, not Unity hierarchy calls, frame timings
or total allocation measurements. The separate installed-asset collar audit
covered **160 cases** (both ends of 40 variants in visual/walking forms); it
does not establish visual acceptance.

### Shipyard Expansion 0.12.1 compatibility

The 0.2.2 texture compatibility fix is incorporated into **0.3.0-dev** and
extended to loose-footed and boomed spritsails. All families use SE 0.12.1's
named texture catalog, string selections and string allowed-option lists. The
obsolete numeric catalog-order helper and fixture are removed; installed
assembly checks cover the named API and each family's plain-texture guard. SE
**0.12.1 is required**; the dependency declaration intentionally has no
minimum-version loading guard.

On **2026-10-04**, the original failed development session logged a
`MissingFieldException` for `SailTextureChanger.sailTextures`, interrupting
`PrefabsDirectory.Start`. A later SE scale exception interrupted save loading;
incomplete prefab initialization is a suspected consequence. The user then
successfully loaded a save upgraded from MSS 0.2.1 / SE 0.11.1 to MSS 0.2.2 / SE
0.12.1 and observed no disappearing sails or bad textures. That observation
covers the tested save, not spritsails or general save migration and downgrade
compatibility. SE 0.12.1 also rotates the sail root when its rotation target is
null. A spritsail-only `SetAngle` prefix preserves the existing no-rotation
policy, including saved angles; the null target still hides rotation buttons on
eligible non-square-only masts. Other sails retain SE rotation.

The spritsail adaptation still needs runtime loading, fitting, recoloring and
save/reload checks, starting on Brig.

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
- **Gloriana collisions, 2026-10-03:** the user reported Spritsail/lateen end
  collisions with loose-footed Mk.B at **110%** and dhow lateen 2 at **85%**.
  The Unity log confirms repeated `col check` / `col_parent` contacts without
  identifying the Spritsail child. The current thin deployed-shape colliders
  need an in-game retest of this arrangement.
- **Gallus collisions, 2026-10-05:** the user reported boomed Spritsail Mk.B
  obstruction before visible contact. The Unity log records neutral contacts
  with `staticrig_left_plumb` and `staticrig_right_plumb`, without identifying
  the sail child. Both Spritsail types now follow native gaff/junk
  deployed-shape checks; installation and actual-contact rotation limits need an
  in-game retest.
- **Boomed regressions:** the user reported installation stuck at “checking
  collision...” and deployment requiring loaded cranking. The Unity log
  confirmed a native checker Awake exception from empty collider children. Code
  fixes address construction timing and reverse reef control direction;
  successful installation and corrected timing still need an in-game retest.
- **Junk mainmast guides, 2026-10-09:** the user reported a false
  `LUFF ABOVE MAST GUIDE` rejection for boomed Mk.B on mainmast 2. Installed
  `level24` confirms both native mainmasts **10/11** put `mastReefAtt` at boat
  height **24.3734 m** and `mastReefAttExtension` at **5.0253 m**; every slot
  uses this reversed-height pair. Both spritsail types now order their assigned
  pair along the mast before clearance checks and routing. Measured geometry
  fixtures and assembly integration checks cover the correction; live
  installation and purchase routing on both Junk mainmasts still need
  confirmation.

These observations cover specific configurations, not all boats or lifecycle
transitions. The winch redesign remains accepted; remaining checks concern
regressions and later changes rather than unfinished original placement work.

### Spritsail native binding revision (2026-10-07)

Targeted native-control regression validation remains incomplete. Validate both
types/marks on Brig, then Gallus plumb/raked and Sanbuq; include mixed
square/gaff/Fisherman arrangements, reordered sails, missing or occupied native
slots, guide-owner removal previews, and loading existing spritsails. Check
native sheet/reef operation and both tacks without Cloth resets. Earlier runtime
acceptance of Fisherman winch placement does not validate this spritsail change.

### Runtime checklist

Start on **Brig**, then the affected boats, especially **Sanbuq** for cloth,
tack and mast-surface work. Spritsail fitting is independent of boat profiles;
Junk, Jong, Cog, Shroud and Baghala need runtime coverage of their eligible mast
variants, guide clearance, controls, collision and save/reload.

For Kakam and Dhow, check registration, collars, controls, preview/cancel and
save/reload on both supported mainmast variants. Verify Dhow's single stay
option follows an empty mast-pair change and protects an occupied pair. Check
Kakam's aft sheet pair and mizzen halyard fallback, plus Sanbuq's four new
halyard fallbacks, for seating, reachability and rope routing. These changes
have no recorded visual acceptance.

For OldChronian, apply the common checklist below plus these profile-specific
checks. Current profile and collider revisions have no recorded runtime
acceptance:

- **Gloriana:** compare raked-foremast and upright main/mizzen spritsail
  fitting; cover both mizzen variants, native-stay removal, collar appearance,
  free auxiliary mizzen halyard seats, and both measured reef fallbacks'
  seating, reachability and operation. Retest the reported Spritsail/lateen
  arrangement.
- **Chronian:** check both authored stays, optional T’gallants and reciprocal
  red conflict help for mount 129 versus Mizzen Royalstay 14.
- **Caelanor:** use foremast/aft-mainmast with and without optional T’gallants;
  verify occupied controls and absence of a stay for the Midmast alternative.
- **Gallus:** fit both Spritsail types/marks on the plumb and raked masts
  without a boat profile; verify native controls, guide clearance,
  contact-limited rotation and reefing visibility.

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

### Performance profiling

The reusable profiler lives under `src/Utils/Profiling/`. Profiling is disabled
by default and captures always start OFF. With the game closed, configure
`[Diagnostics]` in `BepInEx/config/com.august.moresailwindsails.cfg`:

```ini
EnableProfiling = true
ToggleProfiling = F7
ProfileTargets = Rig,Frame,Shape,SheetFlex,Aerodynamics,Ropes,SailMount,Sprit,Snotter,VisualCache
ProfileBypass = SailMount
```

`ProfileTargets` accepts comma-separated, case-insensitive names; whitespace and
duplicates are ignored. `All` selects every category; an empty list records only
whole-game frame statistics. Unknown entries warn once per configuration change
and are ignored. `ProfileBypass` is independent of the timing selection:

| Bypass target        | Visual work skipped                                                |
| -------------------- | ------------------------------------------------------------------ |
| `None`               | No bypass; F7 toggles NORMAL/OFF                                   |
| `SailMount`          | Authored Spritsail luff-mount fitting and drawing                  |
| `Snotter`            | Spritsail snotter fitting/drawing and purchase/collar rope visuals |
| `SpritsailLiveRopes` | Custom Spritsail peak lashings and loose-footed sheets             |

For a visual target, **F7** cycles **NORMAL → BYPASS → OFF**. Selected timings
continue in BYPASS. OFF, configuration changes, or disabling the diagnostic
restore normal visuals; changed configuration returns the capture to OFF.
Unknown bypass names warn and select `None`. Native controls, rope forces,
aerodynamic posing, shape bones and live Cloth remain active in every mode.
Hidden visual consumers retain pending cache updates for resumption. State is
transient and never saved to the boat. Existing
`EnableSpritsailMountProfiling`/`ToggleSpritsailMountProfiling` settings migrate
to the new keys, with explicit new values taking precedence.

| Timing target  | Measured custom CPU work                                                             |
| -------------- | ------------------------------------------------------------------------------------ |
| `Rig`          | Entire rig LateUpdate                                                                |
| `Frame`        | Mast/stay alignment, including physics and collision callers                         |
| `Shape`        | Deployment and shape preparation, edge fitting, obstruction response and bone posing |
| `SheetFlex`    | Loose-footed Spritsail sheet-flex calculation/application                            |
| `Aerodynamics` | Aerodynamic-frame refresh                                                            |
| `Ropes`        | Custom routing, tube fitting/upload and submission; excludes native ropes            |
| `SailMount`    | Mount Draw, with Setup/Surface/Fit/Upload/Submit stages                              |
| `Sprit`        | Cached spar posing                                                                   |
| `Snotter`      | Snotter fitting, posing and rendering, including purchase/collar work                |
| `VisualCache`  | Trigger sampling, revision reasons and consumer rebuild/reuse counts                 |

Ten-second summaries and partial windows at transitions use
`[PerformanceProfile]` in `BepInEx/LogOutput.log`, with session/window IDs,
mode, bypass and selected targets. Measurements distinguish loose-footed
Spritsail, boomed Spritsail, Flying Sail and Fisherman's Staysail work across
**all loaded boats**, including boats other than the player's. Standalone
lifecycle calls without a family scope are labeled `Unspecified`. Categories
with no calls have no timing row.

- Each timing row gives calls, calls per rendered frame, CPU milliseconds per
  rendered frame and maximum call duration. Parent totals include nested
  categories/stages: **do not add inclusive rows together**. Reentrant work in
  the same family/category/stage is counted once.
- Mount counters retain completed uploads and directional surface-cache
  hits/misses. Surface queries outside measured mount calls are excluded.
- Visual-cache counters retain
  Initial/Sheet/Reef/Tack/Fitting/Support/Reactivate reasons and
  Mount/Sprit/Snotter rebuild/reuse counts. Reasons can overlap; rebuild
  requests are not completed uploads.
- Frame mean/max, average FPS and frames over 33 ms describe the whole game,
  including VSync and other mods. `gc0Global` is the process-wide
  generation-zero collection delta.

Custom CPU timings do **not** measure Unity's internal Cloth solver or GPU
completion. Compare 20–30 seconds of steady sailing in NORMAL/BYPASS, then
repeat with the same camera, graphics settings and fitted sails. Measure
deployed, furled and continuous sheet/reef input separately. Exclude loading,
shipyard transitions and initial allocation windows. Instrumentation itself adds
CPU overhead; use identical timing selections throughout a comparison.

The collector uses typed, disposable scopes and fixed buffers; disabled or
unselected hooks do not read timestamps, and warmed collection allocates no
samples or strings. Family context is inherited by shared visuals. Reports and
configuration parsing occur outside measured scopes. Timing/session tests use a
deterministic clock; neither automated suite executes Unity rendering or Cloth.
Release and both check suites passed for the expanded profiler on
**2026-10-09**, including installed-BepInEx configuration migration and
allocation-free sampling. Runtime validation remains pending: start on Brig,
then Sanbuq with two Spritsails, exercising all bypass targets, both tacks,
controls, full/partial/furled sails and flat/3D ropes. Confirm normal visuals
resume after stopping, configuration changes and plugin disable.

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
