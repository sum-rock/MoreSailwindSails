# Working on MoreSailwindSails

Read [README.md](README.md), [src/Plugin.cs](src/Plugin.cs) and relevant code before
editing. Check `git status --short` and preserve user changes. Current user
instructions take precedence over historical design choices.

## Scope and workflow

- Use named arguments rather than positional arguments whenever possible to
  improve readability.
- Prefer one class per file as a rule and describe the responsibility of the
  class within a code comment at the top of the file.
- Update `README.md` only when the user explicitly requests a README change.
  Feature, bug-fix, release and general documentation work do not imply permission
  to edit it; put technical and validation updates in `docs/DEVELOPMENT.md`.
- MoreSailwindSails is an expandable collection of sail families. Keep Flying
  Sail and staysail mechanics independently editable; future families need not
  follow either design. Shared-helper extraction remains
  [deferred](docs/CLEANUP.md); existing shared winch controls remain in use.
- The native winch placement redesign is accepted as valid and complete. Remaining
  improvements are tracked in
  [WINCH_PLACEMENT_REDESIGN_CLEANUP.md](docs/WINCH_PLACEMENT_REDESIGN_CLEANUP.md);
  do not treat that follow-up work as incomplete original implementation.
- Implementation requests authorize editing, building and checking. Do not
  commit, push, alter saves or replace installed game files as part of a build.
  **Never execute files from `scripts/` or use it as the working directory.**
- Use installed assemblies/assets as the behavioral reference; upstream source
  may differ. Never commit proprietary assemblies or extracted game assets.
- Preserve GUID `com.august.moresailwindsails`, DLL `MoreSailwindSails.dll`,
  prefab IDs **400** (Flying Sail), **401/402/403** (Mk.A/B/C), and stay mount
  IDs **128–255**. Keep plugin/project versions and release documentation aligned.

## Technical reference

[DEVELOPMENT.md](docs/DEVELOPMENT.md) owns the detailed guidance. Read the relevant
sections before changing a feature:

| Area                                            | Reference                                                              |
| ----------------------------------------------- | ---------------------------------------------------------------------- |
| Directories, namespaces and test layout         | [Source organization](docs/DEVELOPMENT.md#source-organization)         |
| Cloth, templates, controls and Harmony          | [Runtime safeguards](docs/DEVELOPMENT.md#runtime-safeguards)           |
| Mast-mounted sail                               | [Flying Sail](docs/DEVELOPMENT.md#flying-sail)                         |
| Mk.A/B/C fitting, fixed head and reefing        | [Staysails](docs/DEVELOPMENT.md#staysails)                             |
| Authored stays, registration and large dhow     | [Boat profiles and stays](docs/DEVELOPMENT.md#boat-profiles-and-stays) |
| Native seats and manual fallback pairs          | [Winch placement](docs/DEVELOPMENT.md#winch-placement)                 |
| Installed assemblies, logs and inspection tools | [Local investigation](docs/DEVELOPMENT.md#local-investigation)         |

## Essential safeguards

- **Keep live Cloth topology fixed:** no mesh swaps, bind-pose changes, solver
  resets or Cloth rebuilding on tacks. Move existing bones. The mirrored-mesh
  experiment passed geometry checks but detached/reset in game.
- Construct templates inactive with fresh Cloth for new topology. Preserve the
  donor Animator, shadow hierarchy and mesh ownership. Rope endpoints must be
  separate leaves, never skin bones; native `RopeEffect` rotates them.
- Keep the wind-center/audio object beneath the family pivot: `SailFlapAudio`
  searches only two parents up for `Sail`. Preserve its posed aerodynamic frame.
- Preserve coupled edge fitting, finite fallbacks and scoped force/appearance
  patches. Do not alter vanilla forces or shared donor assets. Keep full native
  sail lists outside control binding and restore them in a finalizer.
- Resolve authored **active** mast sections/guides and protect occupied supports.
  Preserve save ordering, preview restoration and registration rollback.
- Enforce **±40°** after native sway, retaining tighter collision limits. Keep
  iterative order-text guards before NANDFixes; later HarmonyX prefixes still
  run after `false` and must receive consumed input.
- Preserve Flying Sail **85%** upper sheeting and fixed ties; staysails use
  **14° × clamped currentUnroll**, independent lower sheets and upward reefing.
- Sheets reserve complete native pairs atomically by identity and aliases,
  then try the category's explicit manual fallback. Halyards use only the requested
  active mast's native seats. Native bindings take priority. Move the parent mount,
  never the input wheel; exhaustion hides/retries while retaining controllers.
  Trust native and authored fallback positions: do not add geometric clearance,
  radius or proximity checks. Never restore generated rail/mast offsets or pooled
  Shroud pin selection.

## Verification and handoff

Use the pinned Nix/CSharpier environment and the
[build and check commands](docs/DEVELOPMENT.md#build-and-automated-checks).
Always build Release for plugin-affecting changes and run both check suites.
Use CSharpier `check` for read-only work and `format` to fix formatting.
Documentation-only changes normally need diff/link/path review, not a build.
Do not change dependencies to bypass first-time restore/network failures.

**Neither suite simulates Unity Cloth.** Report automated results separately
from observed game behavior. For future changes, follow the runtime validation
guidance in [DEVELOPMENT.md](docs/DEVELOPMENT.md), starting on Brig, then affected
boats (especially Sanbuq for tack/cloth work). Keep validation notes current
when user observations confirm or contradict an approach.

At handoff, report the version, changes, checks actually run, remaining in-game
uncertainty and built DLL path when applicable.
