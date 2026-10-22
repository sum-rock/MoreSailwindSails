# How winches are placed

Part of the [Controls reference](Controls). For donor selections and mounting
measurements, see [Fallback winch templates](Fallback-winch-templates).

MoreSailwindSails places owned sheet and halyard winches for Fisherman's Stays
and Fisherman's Flying Sails using available positions on the boat's fitted
rigging. It keeps a valid existing placement; when a new placement is needed, it
normally checks compatible native positions first, then predefined fallback
positions. Sheets need a free port and starboard pair, while the halyard is
placed independently.

Placement depends on the consumer's native source policy, usable templates,
fitted supports, sail span, and occupancy. Both families share the boat's
reservation ledger. The
[forced-fallback setting](Winch-placement-diagnostics#forcing-sheet-fallbacks)
skips native sheet positions for both families; it leaves halyard selection
unchanged.

## Winch templates and mounting positions

Physical masts and stay-only masts can reference `reefWinch`, `leftAngleWinch`,
`rightAngleWinch`, and `midAngleWinch` arrays. In this implementation, halyards
use reef controls and sheets use corresponding left and right array entries. A
template supplies the winch object to clone; a mounting position supplies its
location and orientation. A usable template can still have an occupied original
position, so template availability and placement availability are checked
separately.

## How staysail sheets and halyards are found

For a sail on a Fisherman's Stay, the stay owns the port sheet, starboard sheet,
and halyard controls. During construction, its configured `donor` supplies the
initial sheet templates and its aft attachment mast supplies the initial halyard
template. Placement can later replace those clones with templates from other
eligible sources. The configured donor does not fix the final winch positions.

### Matching stays to the supporting mast pair

The allocator resolves the stay's forward and aft attachment sections to their
physical base masts using the boat's authored ancestry map. For example, Brig
topmasts **55** and **56** resolve to base masts **3** and **5**. This lets a
stay attached to topmasts find controls associated with the underlying mast
pair.

It then walks the boat's registered shipyard options in their existing order. An
eligible source must:

- Have a `Mast` with `onlyStaysails = true`.
- Be an existing stay rather than another Fisherman's Stay.
- Have no sail attached.
- Reach **both base masts** through its `BoatPartOption.requires` dependencies,
  following indirect dependencies as well as direct ones.

This includes registered stays supplied by optional mods. The source stay itself
can be unfitted; its control's actual mounting supports must still be active.
The search follows fitting dependencies, rather than choosing a nearby stay by
name or distance.

## How Flying Sail sources are found

A Flying Sail owns its controls through its rigging adapter. Its fitted physical
mast supplies initial templates and is the sole native source for both sheet and
reef candidates (`FittedMast` policy). It does not search matching stays. The
mast must be fitted and present in the boat's native inventory.

The adapter requires the forward mast, aft supporting mast, and both guide
sections to remain active. The fitted forward mast is also the requested halyard
fallback mast. Physical mast arrays keep their native controls; the adapter
releases native bindings to the Flying Sail's controllers before using owned
clones. See the [consumer contract](Controls#consumer-contract-and-ownership)
for ownership and cleanup.

## Selecting sheet and halyard positions

The following placement rules apply to both families, using their respective
native source policy. Fisherman's Stays use `MatchingStays`; Flying Sails use
`FittedMast`.

### Sheeting winches

For each eligible native source, the allocator examines `leftAngleWinch[i]` and
`rightAngleWinch[i]` as a pair, in array order. Both controls must exist and
have a renderer and collider. A missing right control at index `0` cannot be
paired with a right control at index `1`, and `midAngleWinch` entries are not
used for these sheet pairs.

After the native candidates, it tries the boat's authored `SheetFallbacks`. Both
fallback contacts must satisfy the fitted-span boundary below. Eligible pairs
are ordered by their forwardmost contact point, progressing aft along **−Z**.
Both sides' specified supporting masts must be fitted.

Each `SheetFallbackPair` specifies one donor on the **same boat** using
`TemplateMast`, `TemplateRole`, and `TemplateIndex`. Both sides clone that
donor, with separate authored contacts and surface normals. The existing
inventory includes unfitted donor masts; the donor's original position may be
occupied. Only the destination supports and positions must be available. The
consumer's required physical masts and guide supports must also remain fitted.

The pair supplies its donor's `SourceNormal` and measured `Offset`. Placement
uses the donor's cached mounting rotation and native scale, aligning each clone
to its destination surface. A missing or destroyed donor, missing array entry,
or control without a renderer or collider makes that pair unavailable. Mod-owned
clones are excluded as donors. Other eligible candidates can still be tried. See
[Fallback winch templates](Fallback-winch-templates#sheet-donors) for the
configured donor slots and their object names.

### Fitted span and fallback eligibility

The boundary is measured in the `BoatRefs` frame:

```text
boundaryZ = forwardBaseMastZ - fittedSpan
port.Contact.z <= boundaryZ AND starboard.Contact.z <= boundaryZ
```

`fittedSpan` is the absolute Z component of the neutral fitted width vector in
boat space. The sail rig includes fitting scale and orientation and the mast's
transform when calculating it. Boat rotation and world scale are removed by
conversion into boat space. It is a projected width, not the sail edge length or
a measurement of live Cloth, trim, reefing, or billow. Eligibility compares
surface contacts, before adding the donor's mounting offset.

For example, a forward base mast at Z = 3 and a fitted span of 5 put the
boundary at Z = −2. A pair with both contacts at −2 is eligible. A pair with one
contact at −1.9 is not; a pair at −6 can be tried next if the nearer pair is
occupied. Native positions retain priority even when they are ahead of this
boundary.

A recognized custom sail without valid fitted geometry supplies no span, so
sheet fallbacks are deferred while native candidates remain available. Other
sail types on Fisherman's Stays supply zero, preserving the mast-only boundary.

Growth can invalidate an existing fallback and trigger a search farther aft.
Shrinking retains a still-valid placement instead of moving it to a newly
eligible nearer pair. A changed span clears the sheet retry delay so resizing
can recover without waiting for the next scheduled retry.

### Halyard winch

For each eligible native source, the allocator examines its `reefWinch` entries
in array order. Each usable entry is a single-position candidate. For a stay,
this uses the same base-mast pair as the sheet search, but the halyard and
sheets can come from different source stays. A Flying Sail searches only its
fitted physical mast's reef array.

After those candidates, it tries the `HalyardFallbacks` entry matching the
requested **physical base mast's** `orderIndex`: the aft mast for a Fisherman's
Stay, or the fitted mast for a Flying Sail. That entry supplies an authored
position and orientation and a `TemplateIndex` into the base mast's `reefWinch`
array. The original template position may be occupied because the clone is
placed at the fallback position. That destination must still be free, and the
requested base mast must be fitted. A missing fallback definition or template
leaves no fallback candidate. Its mounting normal and offset are authored per
fallback; rotation and scale come from the selected donor, as described in
[Halyard donors](Fallback-winch-templates#halyard-donors).

The halyard winch is separate from the upper rope guide. For a Fisherman's Stay,
the guide comes from the stay definition's aft attachment section and
`guideIndex`, as described in [Fisherman's Stay](Fishermans-Stay).

### Occupancy, reservations, and placement changes

Before searching again, the allocator retains an existing placement while its
source, supports, occupancy, reservation, and any sheet fallback span constraint
remain valid. Otherwise it tries candidates in the order above, skipping
unavailable positions. Repeated references to the same mounting position are
treated as aliases.

A native position is occupied if its control has a bound rope, even when hidden,
or if the control is active with its renderer or collider enabled. The boat's
reservation ledger also prevents custom controls from claiming the same
position. Both sheet positions are acquired together; the halyard is acquired
separately. Thus a free halyard does not imply a free sheet pair.

When no candidate succeeds, the affected controls are hidden and their claims
released. While the controls are still needed, placement retries after one
second. Authored fallbacks are predefined mounting positions; this runtime
search does not generate a new position on an arbitrary deck or rail surface.

## Inspecting placement

See [Winch placement diagnostics](Winch-placement-diagnostics) for overlay
settings, native-scale model previews, surface capture, and the startup-only
option to force authored sheet fallbacks.

### Viewing winch mounting points

See
[viewing winch mounting points](Winch-placement-diagnostics#viewing-winch-mounting-points).

### Coordinate frames

See [coordinate frames](Winch-placement-diagnostics#coordinate-frames).

### Capturing a proposed position

See
[capturing a proposed position](Winch-placement-diagnostics#capturing-a-proposed-position).

### Generating halyard fallback positions

See
[generating halyard fallback positions](Winch-placement-diagnostics#generating-halyard-fallback-positions).

## Source

This page describes
[revision `fcdf300`](https://github.com/sum-rock/MoreSailwindSails/tree/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5),
whose plugin metadata version is **0.3.0**.

- [Diagnostic settings and shortcuts](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Plugin.cs)
- [Native control discovery](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Discovery/NativeWinchInventory.cs)
- [Stay control construction](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Stays/FishermansStay/FishermansStay.cs)
- [Control setup and base-mast resolution](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/WinchControls.cs)
- [Stay dependency traversal](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Discovery/Inventory/StayDependencies.cs)
- [Matching native sheet pairs](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Discovery/Inventory/NativeSheetPairing.cs)
- [Halyard placement](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Placement/HalyardWinchPlacementResolver.cs)
- [Sheet placement](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Placement/SheetingWinchPlacementResolver.cs)
- [Per-pair donor and mounting data](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/SheetFallbackPair.cs)
- [Sheet surface alignment](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/SheetFallbackSeat.cs)
- [Sheet fallback ordering](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Placement/SheetFallbackOrder.cs)
- [Candidate selection](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Placement/WinchPlacementPolicy.cs)
- [Position reservations](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Placement/WinchReservations.cs)
- [Placement retention and retry](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Runtime/WinchGroup.cs)
- [Boat control configuration](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/BoatControlCatalog.cs)
- [Stay consumer adapter](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Stays/FishermansStay/FishermansStayControls.cs)
- [Flying Sail consumer adapter](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Sails/FishermansFlyingSail/FishermansFlyingSailRigging.cs)
- [Neutral span validation](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/SailControlGeometry.cs)
- [Staysail fitted width](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Sails/FishermansStaysail/FishermansStaysailRig.cs)
- [Flying Sail fitted width](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Sails/FishermansFlyingSail/FishermansFlyingSailRig.cs)
