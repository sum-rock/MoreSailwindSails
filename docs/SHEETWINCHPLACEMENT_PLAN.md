# Native winch placement resolver plan

Review draft for the **0.2.1** codebase. This document proposes a replacement
placement policy; it does not describe implemented behavior. Current behavior
and build instructions remain in [DEVELOPMENT.md](DEVELOPMENT.md#winch-placement).

## Goal and viability

Use existing vacant native winch positions before using manually authored sheet
fallbacks. Stop generating additional positions around a donor or along a mast.
Sheet placements belong to a boat-local mast category; halyard placements belong
to the actual selected active mast. Preserve the native controls themselves and
continue operating the mod's own cloned controls and sail-owned controllers.

The installed game supports this approach:

- `Mast` supplies `leftAngleWinch`, `rightAngleWinch` and `reefWinch` arrays.
  `BoatCustomParts.availableParts` groups shipyard options, and
  `BoatPartOption.optionName` supplies their displayed names.
- Native Brig mast IDs **3** (foremast 1) and **2** (foremast 2) share one
  port/starboard pair. Its middle stays reference other pairs. Pooling variants
  alone therefore does not necessarily add distinct positions.
- Native Jong IDs **2** (main mast 1) and **3** (main mast 2) belong to separate
  shipyard groups and can coexist. Removing digits from labels would incorrectly
  merge these physical mast positions.
- Installed `Mast.UpdateWinchesEnabled` calls `ShowWinch(rope != null)`;
  `GPButtonRopeWinch.ShowWinch` changes renderer/collider visibility. Binding and
  visibility can establish occupancy without moving or disabling native objects.
- The current coordinator already owns clones, reservations and controller-safe
  retries. It needs a paired allocation operation because sheets currently claim
  positions independently.

This establishes architectural viability, not sufficient capacity on every boat.
Category membership and native pair correspondence must be audited before
switching each boat to the new policy. Manual fallback coordinates may initially
be undefined; native placement must still work, with a graceful error if a missing
fallback is needed.

## Agreed scope and category definitions

Apply the sheet policy to Flying Sails, Mk.A/B/C staysails and native sails fitted
to custom Fisherman's Stays. Apply the halyard policy to those same ownership
paths. Keep family rigging independently editable; share placement and allocation
only. Include **all eight boats in the first pass**: Brig, Junk, Jong, Sanbuq, Cog,
Leopard, Shroud and large dhow. Shroud uses the same sheet and halyard resolvers;
its belaying-pin seats and measured sheet mounts are boat data, not an exemption
or a separate placement path.

Author named category definitions in each boat's profile, with explicit ordered
index sets such as `Foremast`. Use installed display names to identify members
and actual shipyard option groups to verify which are mutually exclusive. Runtime
lookup uses those index sets, not string parsing. Preserve distinct simultaneous
mast positions even when they share a display-name prefix. Map topmast sections
to their physical base category using the existing authored ancestry.

Each category records physical mast variants separately from the native rig IDs
that supply sheet positions. Include the category's physical
mast variants and explicitly associated native stays in the sheet-source set.
This expands the pool beyond Brig's single shared foremast pair. Membership is
authored from verified rig relationships, not assumed from the word "stay" in a
label. The native-only Brig foremast example `{ 3, 2 }` is not the complete
Shipyard Expansion membership list.

Use the custom sail's forward physical mast to choose its sheet category:
`References.Fore` for custom stays and `Pair.Fore` for Flying Sails. This is an
explicit mod policy: the game does not universally assign a native stay's sheet
controls to the forward physical mast. Audit associated native stays against their
fore/aft supports before adding them to a category; do not equate the existing
sheet geometry donor ID with a physical mast category.

## Resolver and allocation changes

Add internal `SheetingWinchPlacementResolver` and
`HalyardWinchPlacementResolver` under `src/Controls/`. Keep Unity clone creation,
rope binding, handles, outlines and disposal in `FishermanWinchControls`.

The sheet resolver accepts the boat, owner, forward physical mast and current
pair claim. It returns a complete placement pair or a structured failure. Pair
candidates carry a stable identity, port/starboard poses, native source controls,
support requirements and origin (`native` or `fallback`). The halyard resolver
accepts a concrete active mast and returns one existing native seat. Neither
resolver activates native options or changes registration/save ordering.

Extend `WinchReservations` with atomic pair acquisition/release using the same
boat-owned allocation ledger as individual halyards on every boat. Claim actual
native identities as well as physical space; two aliases
or different donors must not claim the same seat. A failed pair acquisition must
leave no partial reservation. Candidate list offsets alone are not seat identities.

### Sheet selection

1. Resolve the forward mast's authored category. Discover its source rigs in
   the full inactive hierarchy by `orderIndex`, independent of `BoatRefs.masts`
   startup registration. Exclude the mod's own mounts and control clones.
2. Enumerate every corresponding left/right pair from those rigs, retaining
   native asymmetry and orientation. Use matching array indices only where the
   installed data confirms correspondence; author explicit index pairs for
   exceptions. Skip missing, null or unmatched entries with a diagnostic. Never
   combine independently chosen port and starboard positions or reflect one side
   to invent the other. A centre sheet is not a port/starboard candidate.
3. Deduplicate shared control references and coincident seats. Retain all native
   aliases for occupancy checks. Inactive source rigs may contribute hull-mounted
   sheet seats only when their actual mounting support exists in the current rig.
   Seats on an absent optional structure are ineligible.
4. Keep a current valid pair stable. Otherwise inspect native pairs in authored
   source order, then array-pair order. Both sides must be vacant, supported and
   clear of other fittings and custom reservations before claiming either.
5. If no native pair qualifies, try that category's named, manually authored
   fallback pair. If it is null, undefined or incomplete, log the contextual error
   described below and proceed to graceful exhaustion. Use a whole fallback pair;
   never mix one native side with one fallback side. No donor-distance search or
   generated strip positions remains in this path.
6. If neither tier succeeds, hide both custom sheets, retain their controllers
   and retry at the existing one-second interval. Placement exhaustion must not
   prevent stay registration or remove saved sails.

For native seats, use the corresponding native controls as clone templates so
their shape/scale matches the seat. Fallback pairs specify compatible templates
and both mounting poses. Reuse existing clones when templates are unchanged;
otherwise replace the pair's clones while preserving the sail-owned controllers
and updating any custom-mount winch references. Place parent mounts and preserve
wheel-local input rotation. Never attach the custom rope to a native control.

### Occupancy, binding and native priority

A bound native `rope` reference blocks borrowing even during visibility changes.
An active native renderer or collider also blocks it. Check all aliases of a
seat and nearby native fittings, not just the source mast currently being queried.
Unused eligible native seats may be borrowed; this does not make every hidden
fitting on the boat available. Preserve collision protection for other fittings
and existing interaction colliders. Do not globally reduce clearance.

Register both custom sheet bindings before attempting the pair claim. Custom
stay variants created at startup remain unclaimed until their controls are needed;
temporary mount-owned and sail-owned controls must not both claim seats for one
sail. Reconcile ownership using the existing native-control suppression paths.

Recheck claims after binding/shipyard changes and during the coordinator refresh.
If either native seat becomes occupied, its support disappears, or either pose
changes relative to the boat, release the entire pair and resolve again. Native
controls take priority. Keep a valid fallback pair when a native seat later frees
to avoid moving controls unnecessarily. Removal, disabled owners and failed
rebinding release claims while preserving controller lifetime rules.

### Halyard selection

Use only the `reefWinch` entries associated with the concrete requested active
mast, in native array order. Preserve current family mast selection: Flying Sails
use their mounting mast; Mk.A/B/C staysails use the aft base; native sails on
custom stays use that stay's existing halyard-source mast. A topmast may reference
a fitting physically attached to its active lower mast; preserve that native
association and verify the actual support remains active.

Borrow an unused native position with the same occupancy, deduplication, native
priority and shared reservation rules. Preserve its native mounting pose. Do not
search another mast variant in the category, generate vertical/rotated offsets,
or invent new mast seats. On exhaustion, retain the controller and hide/retry.
Manual sheet fallbacks do not provide a halyard fallback.

Apply these rules to Shroud's belaying pins in the first pass. Audit which native
pin controls belong to the requested active mast and use those existing seats
through the common resolver. Do not retain the old pooled pin-bank lookup if it
would admit seats from a different mast. Keep any measured pin-specific clearance
in Shroud's profile without changing other boats' clearance. Verify actual pin
capacity and native reclaim in game rather than assuming the previous twelve-seat
pool remains eligible under the new rules.

## Manual fallbacks, migration and diagnostics

Each ship's profile explicitly declares named fallback vector mounts for its
physical mast categories:

- `ForemastFallback` for the foremast category.
- `MainmastFallback` for a single mainmast position, or separate
  `MainmastAFallback` and `MainmastBFallback` for two distinct mainmast positions.
- `MizzenmastFallback` and any further category fallback where that category can
  carry a supported custom sail. Never merge simultaneous mast positions.

Each named fallback is a nullable **port/starboard pair**, with an explicit
boat-local position vector for each side plus orientation/normal, mounting offset,
compatible clone template and support requirements. Preserve authored asymmetry;
do not derive one side by reflecting the other. Null or an absent entry means
"not authored"; `Vector3.zero` is not a missing-value sentinel. An incomplete or
non-finite pair is unavailable as a whole.

Native pairs always take priority on a new allocation. When no native pair is
available and the applicable fallback is missing or invalid, log an **error**
identifying boat, category, owner, fallback name and missing/invalid side. Limit
this error to once per boat/category for that missing definition during the boat
instance's lifetime; retries stay quiet. Hide both sheets, retain their controllers
and continue retrying native placement. Do not throw, roll back stay registration,
remove sails, claim half a pair or place anything at the origin by default.
A missing fallback must not block successful native placement. A defined fallback
that is occupied follows ordinary placement-exhaustion logging, not a missing-data
error.

Author the vectors manually using F9 captures with explicit boat/category/side
labels, then compare collision hits with visible geometry and confirm reach and
rope routing. Existing measured surfaces can inform this work but must be
converted to explicit approved points; do not generate fallback coordinates
algorithmically. Seed Shroud's applicable fallback entry with its existing measured
forward sheet pair after checking category association; try native pairs first.
Leave other unmeasured entries explicitly null and report them for later capture.
Missing vectors do not prevent the first-pass rollout. Multiple sails may still
exhaust finite fallback space.

Keep only numeric measurements and reference identities in fixtures. Audit native
assets and installed Shipyard Expansion/boat-mod additions together, so the
profile's constants describe the installed supported rig options. Validate unique
physical-category membership, deliberate source sharing, paired controls and
active support requirements.

Retire donor-centred sheet strips and generated mast halyard offsets for migrated
boats. `SourceMast`/`SourceIndex` must no longer determine the search area there;
retain any use still needed for clone templates or registration. Preserve
GUID, prefab IDs, stay IDs **128–255**, native save ordering and existing saved
stay geometry. Placement claims remain transient runtime state; no save migration
or new player configuration is required.

Retain initial-placement, quiet retry and
`Winch successfully placed after retry` logging. Report sheet placement as a pair:
boat/owner, category, source rigs and array indices, native/fallback pair ID,
both poses and template identities. Failure counts distinguish unavailable native
pairs, custom reservations, missing supports and exhausted fallback pairs. Missing
fallback definitions use the error behavior above. Log
native-driven reassignment once. Halyard logs identify the requested active mast
and selected native seat. Update the consolidated development guidance when this
policy is implemented; preserve its distinction between automated and game tests.

## Implementation sequence and acceptance

1. **Inventory:** add numeric fixtures for native/SE option labels, group membership,
   left/right pair references and halyard seats across all eight target boats.
   Author category constants and the explicit native-stay associations. Validate
   Brig's shared foremast pair and Jong's distinct simultaneous main masts.
2. **Allocation:** implement the two resolvers and atomic sheet claims, with pure
   allocation tests and installed-assembly visibility/binding checks.
3. **Integration:** route all three ownership paths through the new resolvers,
   preserve initialization/rollback and controller lifetime, and verify shared
   reservations protect native and custom fittings on every boat, including Shroud.
4. **Fallback authoring:** declare every applicable named pair, fill verified
   vectors and leave unmeasured entries null with graceful errors when needed.
   Include Shroud in the first-pass rollout; do not silently retain its previous
   resolver or the old generated search as an additional fallback tier.
5. **Validation:** build Release and run both suites in the pinned Nix environment,
   run CSharpier and diff/link checks, then perform manual game tests. Builds do
   not install the DLL or alter saves. This plan document itself needs only
   documentation checks.

Automated coverage must include shared/duplicate references across variants;
unequal or missing left/right arrays; preserved native asymmetry; one-side-only
vacancy rejecting the whole pair; all native pairs occupied selecting a fallback;
separate MainmastA/MainmastB fallbacks; missing/null/half-defined/non-finite fallback
data logging an error without throwing or claiming an origin/partial pair;
missing fallback data with successful native placement; quiet repeated failures
and recovery when a native pair frees;
atomic acquisition failure; multiple custom sail families; native reclaim of
either side; full exhaustion and recovery; inactive support rejection; early
startup with an incomplete live mast array; controller-preserving template
replacement; and cleanup on shipyard cancellation/removal. Halyard tests must
prove an empty seat on an inactive alternative mast is never used, and that a
native seat on the current mast is used without an invented offset. Include Shroud
pin references, measured clearance, occupancy and native reclaim in these checks.

In game, start on Brig, then test every migrated boat. Fit each sail family alone
and together, occupy/free native pairs with native sails, exercise halyards,
change mast options, complete/cancel shipyard orders, and reload saves. Confirm
both sheets are reachable and independently usable, paired movement/recovery,
stable valid fallbacks, correct rope routing and halyards seated on active
supports. On Shroud, verify the common resolver selects vacant native sheet pairs
before its measured fallback and only uses pins associated with the active halyard
mast. Exercise missing fallback data in game without losing stays or controllers.
Automated tests do not simulate Unity Cloth, interaction or the complete shipyard
lifecycle.

This work addresses the placement model behind
[#16](https://github.com/sum-rock/MoreSailwindSails/issues/16) and
[#21](https://github.com/sum-rock/MoreSailwindSails/issues/21); implementation and
per-boat runtime evidence are required before claiming either broader capacity
or placement coverage is complete.
