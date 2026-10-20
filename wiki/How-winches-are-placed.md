# How winches are placed

MoreSailwindSails places the sheet and halyard winches for Fisherman's Stays
using available positions on the boat's fitted rigging. It keeps a valid
existing placement; when a new placement is needed, it checks compatible native
positions first, then predefined fallback positions. Sheets need a free port and
starboard pair, while the halyard is placed independently.

Placement depends on the supporting mast pair, usable winch templates, fitted
supports, and occupancy. This page explains that selection process and why
controls can move or remain hidden.
[Fisherman's Flying Sails](Fishermans-Flying-Sails) bind through the physical
mast's native control arrays and sail ordering.

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

Fallback sheets use a shared template: **Kakam mast 5, `leftAngleWinch[0]`**,
cloned into both authored positions. The template is found among loaded scene
boats, including inactive boats and unfitted masts; its original position does
not need to be free. If that template is unavailable, the fallback pair cannot
be created.

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
candidate.

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

The diagnostic overlay shows which mounting positions are occupied and whether
their supports are fitted. Position capture supplies coordinates for authoring
fallbacks. These tools expose placement data without changing the allocator's
selection rules.

### Viewing winch mounting points

With MoreSailwindSails installed, set this entry in
`BepInEx/config/com.august.moresailwindsails.cfg`:

```ini
[Diagnostics]
EnableWinchMountOverlay = true
```

Close menus, aim at a boat surface within **10 metres**, and press **F8**. Press
F8 again to hide the overlay. `ToggleWinchMountOverlay` changes the shortcut;
setting it to `None` disables that shortcut.

| Color | Meaning                                |
| ----- | -------------------------------------- |
| Green | Unused position on fitted supports     |
| Red   | Occupied, including hidden bound ropes |
| Cyan  | Unused position on unfitted supports   |

Green describes occupancy and support. A sail still needs a compatible template,
source mast pair, and, for sheets, a complete pair of positions.

### Coordinate frames

| Value                              | Frame                                    |
| ---------------------------------- | ---------------------------------------- |
| Stay `ForePoint` / `AftPoint`      | Local to the respective physical mast    |
| Authored fallback control position | Local to the `BoatRefs` root             |
| Native control `localPosition`     | Local to that control's immediate parent |
| Position captured by F9            | Local to the `BoatRefs` root             |

For a normal scene transform, convert a world position with
`boat.transform.InverseTransformPoint(position: worldPoint)`. Convert a
mast-local endpoint to world space with
`mast.transform.TransformPoint(position: localPoint)`. Include parent rotation
and scale; copying a native control's `localPosition` into a boat-local field
can place it on the wrong side of the boat.

Sailwind also uses a separate walking-collision model. The capture tool maps
hits from that model through the visible boat model before returning boat-local
coordinates. It transforms surface normals with the inverse transpose so scaled
transforms are handled correctly.

### Capturing a proposed position

Aim at a solid boat surface within 10 metres during normal play and press
**F9**. `CaptureWinchPosition` changes this shortcut. The overlay does not need
to be enabled.

Entries beginning with `Winch position capture #` in `BepInEx/LogOutput.log`
give the boat, boat-local position and surface normal, hit model, hierarchy
path, collider identity, and distance. Capturing a point does not move or
reserve a control. It supplies a measurement for an authored fallback; that
fallback's rotation and supporting parts are defined separately in the boat's
control configuration.

## Source

- [Diagnostic settings and shortcuts](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Plugin.cs)
- [Native control discovery](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Controls/Discovery/NativeWinchInventory.cs)
- [Stay control construction](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/FishermansStay.cs)
- [Control setup and base-mast resolution](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Controls/WinchControls.cs)
- [Stay dependency traversal](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Controls/Discovery/StayDependencies.cs)
- [Matching native sheet pairs](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Controls/Discovery/NativeSheetPairing.cs)
- [Halyard placement](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Controls/Placement/HalyardWinchPlacementResolver.cs)
- [Sheet placement](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Controls/Placement/SheetingWinchPlacementResolver.cs)
- [Sheet fallback ordering](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Controls/Placement/SheetFallbackOrder.cs)
- [Shared sheet template discovery](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Controls/Discovery/SharedSheetWinchTemplate.cs)
- [Shared sheet template identity](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Controls/Configuration/SheetFallbackTemplate.cs)
- [Candidate selection](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Controls/Placement/WinchPlacementPolicy.cs)
- [Position reservations](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Controls/Placement/WinchReservations.cs)
- [Placement retention and retry](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Controls/Runtime/WinchGroup.cs)
- [Boat control configuration](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Controls/Configuration/BoatControlCatalog.cs)
- [Surface picking and frame conversion](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Utils/BoatSurfacePicker.cs)
- [Position logging](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Utils/LogFallbackWinchPlacement.cs)
- [Overlay colors and drawing](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Utils/WinchMountOverlay.cs)
