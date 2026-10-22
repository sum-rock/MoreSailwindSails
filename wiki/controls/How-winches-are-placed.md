# How winches are placed

Part of the [Controls reference](Controls). For donor selections and mounting
measurements, see [Fallback winch templates](Fallback-winch-templates).

MoreSailwindSails places the sheet and halyard winches for Fisherman's Stays
using available positions on the boat's fitted rigging. It keeps a valid
existing placement; when a new placement is needed, it checks compatible native
positions first, then predefined fallback positions. Sheets need a free port and
starboard pair, while the halyard is placed independently.

Placement depends on the supporting mast pair, usable winch templates, fitted
supports, and occupancy. This page explains that selection process and why
controls can move or remain hidden.
[Fisherman's Flying Sails](Fishermans-Flying-Sails) use the existing winch
positions on the mast they are attached to. The game assigns those winches using
its normal rules; the mod does not choose custom or fallback positions for
Flying Sails.

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

### Sheeting winches

For each matching source, the allocator examines `leftAngleWinch[i]` and
`rightAngleWinch[i]` as a pair, in array order. Both controls must exist and
have a renderer and collider. A missing right control at index `0` cannot be
paired with a right control at index `1`, and `midAngleWinch` entries are not
used for these sheet pairs.

After the native candidates, it tries the boat's authored `SheetFallbacks`. Both
fallback contact points must be at or aft of the forward base mast in boat-local
coordinates. Eligible pairs are ordered by their forwardmost contact point,
progressing aft along **−Z**. Both sides' specified supporting masts must be
fitted.

Each `SheetFallbackPair` specifies one donor on the **same boat** using
`TemplateMast`, `TemplateRole`, and `TemplateIndex`. Both sides clone that
donor, with separate authored contacts and surface normals. The existing
inventory includes unfitted donor masts; the donor's original position may be
occupied. Only the destination supports and positions must be available. The
supporting forward and aft physical masts must also remain fitted.

The pair supplies its donor's `SourceNormal` and measured `Offset`. Placement
uses the donor's cached mounting rotation and native scale, aligning each clone
to its destination surface. A missing or destroyed donor, missing array entry,
or control without a renderer or collider makes that pair unavailable. Mod-owned
clones are excluded as donors. Other eligible candidates can still be tried. See
[Fallback winch templates](Fallback-winch-templates#sheet-donors) for the
configured donor slots and their object names.

### Halyard winch

For each matching source stay, the allocator examines its `reefWinch` entries in
array order. Each usable entry is a single-position candidate. This search uses
the same base-mast pair as the sheet search, but the halyard and sheets can come
from different source stays.

After those candidates, it tries the `HalyardFallbacks` entry matching the aft
**base mast's** `orderIndex`. That entry supplies an authored position and
orientation and a `TemplateIndex` into the base mast's `reefWinch` array. The
original template position may be occupied because the clone is placed at the
fallback position. That destination must still be free, and the aft base mast
must be fitted. A missing fallback definition or template leaves no fallback
candidate. Its mounting normal and offset are authored per fallback; rotation
and scale come from the selected donor, as described in
[Halyard donors](Fallback-winch-templates#halyard-donors).

The halyard winch is separate from the upper rope guide. The guide comes from
the stay definition's aft attachment section and `guideIndex`, as described in
[Fisherman's Stay](Fishermans-Stay).

### Occupancy, reservations, and placement changes

Before searching again, the allocator retains an existing placement while its
source, supports, occupancy, and reservation remain valid. Otherwise it tries
candidates in the order above, skipping unavailable positions. Repeated
references to the same mounting position are treated as aliases.

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
[revision `d8d9974`](https://github.com/sum-rock/MoreSailwindSails/tree/d8d99742a28657fe7397b669a4160cfd0284094c),
whose plugin metadata version is **0.3.0**.

- [Diagnostic settings and shortcuts](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Plugin.cs)
- [Native control discovery](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Discovery/NativeWinchInventory.cs)
- [Stay control construction](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Stays/FishermansStay/FishermansStay.cs)
- [Control setup and base-mast resolution](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/WinchControls.cs)
- [Stay dependency traversal](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Discovery/StayDependencies.cs)
- [Matching native sheet pairs](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Discovery/NativeSheetPairing.cs)
- [Halyard placement](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Placement/HalyardWinchPlacementResolver.cs)
- [Sheet placement](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Placement/SheetingWinchPlacementResolver.cs)
- [Per-pair donor and mounting data](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Configuration/SheetFallbackPair.cs)
- [Sheet surface alignment](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Configuration/SheetFallbackSeat.cs)
- [Sheet fallback ordering](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Placement/SheetFallbackOrder.cs)
- [Candidate selection](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Placement/WinchPlacementPolicy.cs)
- [Position reservations](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Placement/WinchReservations.cs)
- [Placement retention and retry](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Runtime/WinchGroup.cs)
- [Boat control configuration](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Configuration/BoatControlCatalog.cs)
- [Surface picking and frame conversion](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Utils/BoatSurfacePicker.cs)
- [Position logging](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Utils/LogFallbackWinchPlacement.cs)
- [Overlay colors and drawing](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Utils/WinchMountOverlay.cs)
- [Halyard generator options](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/tools/halyards/cli.py)
  and
  [geometry search](https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/tools/halyards/generate.py)
