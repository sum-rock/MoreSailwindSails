# Winch placement redesign cleanup

Status: **planned, not implemented**. Created 2026-09-27 from the architecture
review of boat definitions and winch placement. This document is intended to be
usable without the original conversation.

## Baseline and accepted behavior

The native winch placement redesign is accepted as valid and complete on all
eight boats; its completed implementation plan has been removed. Current behavior
is documented in [Winch placement](DEVELOPMENT.md#winch-placement). This is a
follow-up cleanup to the accepted redesign.

Baseline: branch `redesign-winch-placement-strategy`, commit `f6beddb`
(`Remove Clearence Restriction on Winch Placement`), plugin/project **0.2.1**.
The branch is context, not an instruction to reset or discard subsequent changes.

Subsequent user decisions are part of the baseline:

- Boat profiles use named domain-constructor arguments; `Vector3` coordinates
  remain positional. Definition types each have their own file under
  `src/BoatRigs/Definitions`, retaining namespace `MoreSailwindSails.BoatRigs`.
- **Neither native placements nor authored fallbacks use geometric clearance.**
  Do not restore collider/mesh radii, proximity rejection, Shroud pin-clearance
  overrides, generated rail/mast offsets or pooled pin selection.
- Native seat identity and coincident-reference aliases prevent duplicate use.
  Coincident-seat recognition is identity deduplication, not a clearance test.
  Native rope bindings and active native renderers/colliders block borrowing.
- Sheet pairs are allocated atomically. Native pairs have priority for new
  allocation; valid current placements, including fallbacks, remain stable.
  Halyards use only the requested active mast's native reef array.
- Exhaustion hides controls and retries while retaining sail-owned controllers.
  Placement moves the parent mount, preserving wheel-local input rotation.
- Only Shroud's foremast fallback is authored. Other fallbacks intentionally
  remain null; this cleanup does not invent their positions.

The architecture to retain is: authored boat data -> native inventory -> separate
sheet/halyard resolvers -> shared reservation policy -> owned clone coordinator.
Keep Flying Sail and staysail mechanics independently editable; the unrelated
[shared-calculation extraction](CLEANUP.md) remains deferred.

## Evidence and validation status

The review found no critical blocker in the current boat data. The redesign is
accepted as complete; this plan's improvements remain **not implemented**.
[Runtime validation](DEVELOPMENT.md#runtime-validation) owns the automated results
and successful Brig/Jong retest observations. Refer there instead of treating
historical failures or earlier retest-pending notes as current status.

Those observations are the baseline for cleanup regressions, not evidence that
these planned changes work. Neither existing suite executes Unity object
lifecycles or measures in-game frame time. Logs can be overwritten on game launch;
paths and installed-assembly inspection instructions are in
[Local investigation](DEVELOPMENT.md#local-investigation).

## Work items

### WC-1 — Avoid rebuilding placement searches every frame (P2)

**Finding:** `FishermanWinchControls.LateUpdate` calls `NativeWinchSeats.Refresh`
every frame, including when the manager has no controls. Every active group then
rebuilds all candidate pairs/seats, even with a valid current placement. Each seat
scans the full inventory for aliases and occupancy. Stable reservations are also
released/recreated, and inactive groups repeatedly suspend already hidden clones.
This is confirmed repeated work; its frame-time impact has not been profiled.

**Change:**

- Separate inventory membership, live seat state, current-placement validation
  and alternative-seat search. Reuse one inventory and identity/alias lookup per
  boat rather than rebuilding them separately for each owner.
- Validate the currently claimed seats first. Keep their claims and clones when
  support, native occupancy, identity and pose remain valid. Search alternatives
  only on first placement or invalidation; retain the existing one-second retry
  behavior for exhausted groups.
- Keep live support/native-reclaim checks responsive. Caching must not freeze
  occupancy, mounting poses, native array contents or newly installed SE parts.
  Invalidate on relevant lifecycle/shipyard changes and refresh discovery before
  allocating after an inventory change. Preserve recovery from incomplete startup
  inventories; do not assume `BoatRefs.masts` is the complete inventory.
- Skip inventory work when there are no owned controls; avoid repeated suspension
  work for already released groups. Keep visual pose updates separate from seat
  searches where moving supports require them.
- Establish an inspection/profiling baseline before choosing cache invalidation
  hooks. Use installed assemblies to identify the actual native/SE lifecycle
  hooks; do not guess hook names from upstream source.

**Acceptance:** a settled group does not enumerate alternative placements or
recreate reservations every frame; an empty manager does no hierarchy scanning.
Native reclaim of either sheet releases/replaces the whole pair, removed/inactive
supports invalidate claims, newly available seats recover, and a valid fallback
does not move merely because a native seat becomes free. Measure hierarchy scan,
candidate construction and allocation counts before/after under the same setup.

### WC-2 — Separate bootstrap templates from geometry donors (P2)

**Finding:** the new resolvers support multiple source rigs, but startup still
requires usable controls on the original donor. `FishermansStay.Create` throws
when its initial controls are missing, and `FishermansStayRegistry.Register`
rolls back that boat's entire stay registration. Both sail-family rigging
`TryResolve` methods also gate readiness on the original donor controls.
Consequently, another valid source or fallback can exist without the resolver
being reached. Current audited donors pass; this is a robustness/design gap.

**Change:**

- Keep geometry donors, authored attachment points, guides, requirements and
  stable mount IDs unchanged. Introduce a separate bootstrap-template selection
  path shared by the three control ownership paths.
- For sheet bootstrap templates, use a complete usable pair from the forward
  category's ordered sources, then that category's valid fallback template pair.
  Selecting a hidden bootstrap template does not reserve its seat or require
  vacancy; templates are cloned and native controls remain untouched.
- For halyards, use the requested mast's usable reef templates. Do not turn
  template selection into permission to place on another mast.
- Replace original-donor-only readiness checks with the same selection policy.
  Keep the native requirement for initialized controls during startup; inspect
  installed `Mast`/winch binding behavior before changing that sequence.
- If no usable template exists anywhere in the allowed sources, report a precise
  template-unavailable failure. Preserve existing safe registration rollback
  rather than registering broken native arrays. This cleanup promises recovery
  from a missing original donor template, not arbitrary template-free startup.

**Acceptance:** a missing original sheet template with a usable later source
still constructs valid controls; valid fallback templates can bootstrap sheets;
port/starboard bootstrap selection remains paired. A totally missing template
fails cleanly without corrupting registration/save ordering. Exercise Flying
Sail, Mk.A/B/C and native sails on custom stays. Verify controller retention and
mount-array updates when bootstrap clones are replaced by selected seat clones.

### WC-3 — Remove the unused explicit-pair authoring branch (P3)

**Finding:** `NativeSheetSource.Pairs` supports custom index correspondence, but
`SheetWinchCategory` accepts only integer source IDs and always constructs sources
with null pairs. No boat authors an override. The pure pairing test exercises a
capability the normal profile constructor cannot express.

**Change:** remove `Pairs` and the authored-correspondence branch in
`NativeSheetPairing`. Store the ordered source IDs directly in the category and
remove `NativeSheetSource` if it has no remaining responsibility. Retain pairing
by matching array index, null/missing-side rejection and unmatched diagnostics.
Do not retain an unused extension API solely for hypothetical boats.

**Acceptance:** all existing authored source order and selected pairs remain
unchanged. Unequal arrays or a missing side never form a partial pair or shift
indices to borrow the next usable control. Update the fixture checks and remove
the obsolete explicit-correspondence test.

### WC-4 — Simplify reservation acquisition (P3)

**Finding:** `WinchReservations.Claim` copies owner/seat data that no caller reads.
Callers only use null/non-null success. `WinchResolution.Claim` is also unused
outside the policy, while the ledger already owns all reservation state.

**Change:** replace `AcquireSeats` with boolean `TryAcquireSeats`, remove `Claim`
and the result's claim field, and keep ownership in the boat-local ledger. Make
unchanged reacquisition idempotent without recreating entries, supporting WC-1.
Preserve all-or-nothing validation before mutating an existing claim.

**Acceptance:** aliases and duplicate identities still reject the whole pair;
failed acquisition leaves no partial reservation; release affects only the
owner; halyards and sheets share the ledger; distinct nearby seats and authored
fallbacks remain allowed without any geometric check.

### WC-5 — Make definition collections immutable (P3)

**Finding:** `readonly` array fields prevent field reassignment, not mutation of
contents. Most definitions retain caller-owned arrays, so validated profile data
can change after construction. Mast ancestry already makes a defensive copy.
No current caller mutation was found.

**Change:** defensively copy collection inputs and expose read-only wrappers,
using `IReadOnlyList<T>` for ordered collections and the existing read-only
ancestry dictionary. Merely exposing an underlying array as `IReadOnlyList<T>` is
insufficient; use a wrapper that cannot be cast back and edited. Apply this to
profile supports/stays/categories, support sections, stay variants and
requirements, category source/member IDs and fallback support IDs. Cache a
read-only catalog rather than allocating `BoatRigCatalog.All` on each access.

Accept `IEnumerable<T>` collection inputs where needed so named array literals
remain convenient and definitions can be passed without exposing arrays. Preserve
ordering and every authored value. Update consumers from array-specific access
such as `Length` to `Count` where required. No new dependency is needed.

**Acceptance:** mutating constructor input arrays after construction cannot change
lookups, requirements or source ordering; exposed collections cannot be edited.
Existing ancestry/cycle checks, all 111 authored stays and eight boat profiles
continue to pass. Null fallbacks remain supported.

### WC-6 — Check support before discarding coincident alternatives (edge-case risk)

**Finding:** both resolvers deduplicate by position before selection. An earlier
unsupported candidate can discard a later supported candidate at the same seat.
The audit fixture contained no exact distinct-reference duplicates among native
rows, so this is an unverified edge-case risk, not a demonstrated boat failure.

**Change:** add a focused reproducer with distinct coincident references and
inactive/active mounting supports. Preserve the first supported candidate in
source order instead of allowing an unsupported representative to suppress it;
retain alias occupancy across all references. Apply the rule to whole sheet
pairs and individual halyards. Share the small selection rule if necessary to
avoid inconsistent behavior between resolvers.

**Acceptance:** an inactive first representative cannot hide an otherwise usable
active equivalent. An occupied alias still blocks borrowing. Deduplication never
mixes sides from different pairs or permits duplicate reservations. This must not
reintroduce geometric clearance.

### WC-7 — Remove small obsolete branches and parameters (P3)

- Remove the unused `boat` parameter from `FishermanWinchControls.Source`, or
  remove that helper if WC-2 supersedes it.
- Remove unused `WinchRole.Mid` and the default-to-centre behavior in
  `NativeWinchSeats.Sources`. Handle supported roles explicitly; reject invalid
  role values rather than silently treating them as centre controls.
- Simplify centre-sheet validation in `FishermansStay.Create`: the code assigns
  an empty `midAngleWinch` array immediately before testing whether it is empty.
  Retain that empty native array and the independent `midRopeAtt` attachment
  handling where required; do not delete similarly named native APIs blindly.

## Execution order, checks and handoff

Suggested reviewable work units: (1) WC-3/WC-4/WC-7 API cleanup; (2) WC-5 immutable
data; (3) WC-6 deduplication regression; (4) WC-2 bootstrap integration; (5) WC-1
steady-state optimization. Add behavior-focused tests with their corresponding
change. This sequence does not authorize commits or implementation by itself.

Read [AGENTS.md](../AGENTS.md), README, `src/Plugin.cs`, the relevant
[development guidance](DEVELOPMENT.md) and actual source before editing. Preserve
user changes. Use installed game assemblies as the behavioral reference. Keep
GUID `com.august.moresailwindsails`, DLL name, prefab IDs 400/401/402/403, stay IDs
128–255, native save ordering, family mechanics and fixed live Cloth topology.
Retain version 0.2.1 unless a separate release instruction changes it.

Existing regression locations are `tests/GeometryChecks/Controls`,
`tests/GeometryChecks/FishermansStay`, `tests/AssemblyChecks/Controls` and
`tests/AssemblyChecks/FishermansStay/WinchChecks.cs`. Pure allocation checks are
executable; IL checks establish wiring only. Do not describe them as Unity
lifecycle coverage. Use the pinned environment:

```sh
nix develop -c bash -c 'dotnet csharpier check . && dotnet build -c Release --no-restore && dotnet run --project tests/GeometryChecks -c Release --no-restore && dotnet run --project tests/AssemblyChecks -c Release --no-restore'
git diff --check
```

Use CSharpier `format` only for implementation edits. Do not change dependencies
to bypass restore failures. Builds produce
`src/bin/Release/netstandard2.0/MoreSailwindSails.dll`; they do not install it.
Never execute files under `scripts/`, change saves, publish or push as part of
this work. Do not commit without a current explicit instruction.

After implementation, update DEVELOPMENT.md with the final architecture and
actual checks/observations. Do not edit README without explicit permission.
Validate in game on Brig and Jong first, then all affected boats, including
Shroud's fallback and pin behavior. Cover native reclaim, mast/support changes,
shipyard complete/cancel, removal/recreation, repeated reloads and mixed sail
families. Report automated results separately from runtime observations and
performance measurements. Preserve the user's acceptance of the original redesign;
do not represent its limited recorded sessions as exhaustive cleanup validation.
