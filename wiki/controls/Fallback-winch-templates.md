# Fallback winch templates

Part of the [Controls reference](Controls). This page describes the donor
objects used at authored fallback positions. See
[How winches are placed](How-winches-are-placed) for candidate ordering,
occupancy, and reservations; use
[Winch placement diagnostics](Winch-placement-diagnostics) for inspection tools.

## Template identity and ownership

A donor is an existing `GPButtonRopeWinch` referenced by a mast in the boat's
hierarchy. Fallback definitions do **not** register new boat-object prefabs or
allocate entries in `PrefabsDirectory`. The mod creates runtime clones of the
selected donors, parents them beneath placement mounts, and binds them to the
consumer's rope controllers. Cloning a donor does not take over its rope
binding.

`WinchControls` tracks the clones for both sail families. The stay adapter
publishes them into its dedicated `Mast` arrays; Flying Sails retain the
physical mast's native arrays and bind their controllers through their own
adapter. An `OwnedWinchMarker` excludes them from native donor discovery.
Replacing a donor transfers the existing controller binding to the replacement
clone and destroys the old clone. These ownership rules are implemented by
[OwnedWinch][owned] and [WinchGroup][group]. See the
[consumer contract](Controls#consumer-contract-and-ownership) for registration,
Flying Sail native-binding handoff, and cleanup.

The identifiers refer to different things:

| Identifier      | Meaning                                                                                    |
| --------------- | ------------------------------------------------------------------------------------------ |
| `TemplateMast`  | Existing donor mast's `Mast.orderIndex` on this boat                                       |
| `TemplateRole`  | Source array: `Left` → `leftAngleWinch`, `Right` → `rightAngleWinch`, `Reef` → `reefWinch` |
| `TemplateIndex` | Zero-based slot in that source array                                                       |
| Fallback name   | Authored placement identity used for lookup and reservations                               |

`TemplateIndex` is not a global prefab ID, a shipyard menu index, or a new entry
in the boat's mast list. The [boat object tables](Boats#table-conventions) list
mast/stay components; they are not a list of the individual winch donors.
Registering a custom stay in `BoatRefs.masts` is a separate operation from
cloning its controls. See [stay registration][registry].

## Sheet donors

Each [SheetFallbackPair][pair] selects **one donor on its own boat**, cloned for
both port and starboard. Different pairs can select different donors. Selection
uses the existing [native inventory][inventory], including unfitted masts; the
original donor position may already be occupied. A donor must exist, have a
renderer and collider, and not be a mod-owned clone. Its selection does not
reserve or activate the original control.

The **25 configured pairs across 12 boats** use these donors. Configuration
links include the authored contact points, normals, offsets, and supporting-part
requirements.

| Boat configuration   | Fallback pair     | Donor mast | Source array slot   | Donor object name                |
| -------------------- | ----------------- | ---------- | ------------------- | -------------------------------- |
| [Brig][brig]         | `Mainmast`        | 5          | `leftAngleWinch[0]` | `rope_winch_mastB_angle_left`    |
| [Brig][brig]         | `Mizzenmast`      | 7          | `leftAngleWinch[0]` | `rope_winch_mizzen_angle_left`   |
| [Junk][junk]         | `Forward`         | 10         | `leftAngleWinch[0]` | `winch_angle_mid_left`           |
| [Junk][junk]         | `Aft`             | 10         | `leftAngleWinch[0]` | `winch_angle_mid_left`           |
| [Jong][jong]         | `Forward`         | 2          | `leftAngleWinch[0]` | `winch_angle_main_front_left`    |
| [Jong][jong]         | `Midships`        | 2          | `leftAngleWinch[0]` | `winch_angle_main_front_left`    |
| [Jong][jong]         | `Aft`             | 2          | `leftAngleWinch[0]` | `winch_angle_main_front_left`    |
| [Sanbuq][sanbuq]     | `Midships`        | 12         | `leftAngleWinch[0]` | `rope_winch_back_angle_left (1)` |
| [Sanbuq][sanbuq]     | `Stern`           | 12         | `leftAngleWinch[0]` | `rope_winch_back_angle_left (1)` |
| [Baghala][baghala]   | `Forward`         | 2          | `leftAngleWinch[0]` | `rope_winch_mainmast_left`       |
| [Baghala][baghala]   | `Midships`        | 2          | `leftAngleWinch[0]` | `rope_winch_mainmast_left`       |
| [Baghala][baghala]   | `Stern`           | 2          | `leftAngleWinch[0]` | `rope_winch_mainmast_left`       |
| [Cog][cog]           | `Stern`           | 8          | `leftAngleWinch[0]` | `winch_angle_mizzen_left (2)`    |
| [Kakam][kakam]       | `Mainmast`        | 5          | `leftAngleWinch[0]` | `rope_winch_angle_left`          |
| [Dhow][dhow]         | `Stern`           | 6          | `leftAngleWinch[0]` | `rope_winch_angle (left)`        |
| [Gloriana][gloriana] | `Mainmast`        | 2          | `leftAngleWinch[0]` | `sheet_mainmast1_L`              |
| [Gloriana][gloriana] | `MainmastForward` | 2          | `leftAngleWinch[0]` | `sheet_mainmast1_L`              |
| [Chronian][chronian] | `MainmastForward` | 4          | `leftAngleWinch[0]` | `rope_mainmain_reef_port`        |
| [Chronian][chronian] | `Mainmast`        | 4          | `leftAngleWinch[0]` | `rope_mainmain_reef_port`        |
| [Chronian][chronian] | `Mizzenmast`      | 6          | `leftAngleWinch[0]` | `rope_mizenmain_reef_port`       |
| [Caelanor][caelanor] | `Mainmast`        | 5          | `leftAngleWinch[0]` | `sheet_main_main_L`              |
| [Shroud][shroud]     | `Foremast`        | 6          | `reefWinch[0]`      | `Coil_Foremast_Reef_1`           |
| [Shroud][shroud]     | `Midships`        | 6          | `reefWinch[0]`      | `Coil_Foremast_Reef_1`           |
| [Shroud][shroud]     | `Aft`             | 6          | `reefWinch[0]`      | `Coil_Foremast_Reef_1`           |
| [Shroud][shroud]     | `Stern`           | 6          | `reefWinch[0]`      | `Coil_Foremast_Reef_1`           |

The base-game boats use wheel models. Gloriana, Chronian, Caelanor, and Shroud
use rope-coil models. Shroud deliberately takes its **sheet** template from a
reef array: `TemplateRole` identifies where to find the donor, while the clone's
binding determines which rope it controls.

Baghala's source configuration is named `LargeDhowControls`. The source comments
record the installed donor inspections and captured contacts, including the
2026-10-20 and 2026-10-21 additions. The complete set of boat profiles is in
[BoatControlCatalog][catalog]. A configured pair still needs to pass the
[fitted-span and availability rules](How-winches-are-placed#fitted-span-and-fallback-eligibility).

If a pair's donor is missing or unusable, that pair cannot be allocated. The
allocator can try other eligible candidates; it does not substitute a donor from
another boat. Changes to a live donor array also invalidate a retained placement
if its selected donor no longer matches. See [sheet resolution][sheets].

## Halyard donors

A halyard fallback belongs to the requested **physical base mast**: the aft mast
for a Fisherman's Stay, or the fitted mast for a Flying Sail. Its
`HalyardFallbackSeat.TemplateIndex` selects
`requestedMast.reefWinch[TemplateIndex]`. For example, Brig mast **3** and
Gloriana mast **2** both select `reefWinch[1]` in their respective
configurations. Other mast definitions can select different indexes; consult the
boat profiles linked from [BoatControlCatalog][catalog].

The donor's original seat may be occupied, but the requested mast must be fitted
and the fallback destination must be available. A missing definition, array, or
usable donor leaves no halyard fallback candidate. Halyard selection is
independent of the sheet pair's template. See [halyard resolution][halyards].

The catalog contains **62 halyard definitions**. Definitions count mast
mappings, not unique physical positions: alternative mast configurations can
share a seat. These captured placements supplement or replace generated
positions:

| Boat             | Mast indexes    | Authored placement                                                                                                                     |
| ---------------- | --------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| [Shroud][shroud] | 5/6, 7/8, 9/10  | Six definitions use three captured halyard stands, one per physical mast location; both mast heights share a seat and reef-coil donor. |
| [Jong][jong]     | 1, 3; 4, 51, 53 | Captures supply the formerly unresolved seats for 1 and 3 and reposition seats for 4, 51, and 53.                                      |
| [Junk][junk]     | 10, 11          | Both mainmast alternatives use the same intentional off-rail contact and normal, matching the boat's halyard layout.                   |
| [Sanbuq][sanbuq] | 69              | A captured contact and normal replace the generated position on the SE mizzenmast.                                                     |

Exact contacts, normals, offsets, and donor indexes are in the linked boat
configurations. Shared coordinates do not create independent usable positions;
the allocator still applies occupancy and reservation checks.

## Mounting measurements

Sheet pairs keep donor measurements once for both sides; each
`SheetFallbackSeat` supplies its own destination contact, normal, and optional
supporting mast indexes. Halyard seats carry the corresponding measurements
alongside their template index.

| Value          | Meaning                                                           |
| -------------- | ----------------------------------------------------------------- |
| `Contact`      | Captured point on the mounting surface, relative to `BoatRefs`    |
| `Normal`       | Destination surface normal in the same boat frame                 |
| `SourceNormal` | Donor's local `+Z` mounting direction expressed in the boat frame |
| `Offset`       | Measured rear depth of the rendered donor at its native scale     |

The control origin is `Contact + Normal.normalized * Offset`. Rotation aligns
`SourceNormal` with `Normal`, then applies the donor's boat-relative mounting
rotation. The inventory caches the donor's local wheel rotation so subsequent
wheel input does not move that mounting frame; parent transforms are still
included. Each clone inherits the donor's world scale, compensating for its
placement parent's scale. No fixed unit-scale override is applied.

An offset measured for one model or scale should not be reused for a different
donor without checking its rear extent. The contact point identifies the solid
surface; the offset positions the model's origin so its rear meets that surface.
Both placement and the diagnostic overlay use the same authored offset. See
[sheet alignment][seat], [halyard alignment][halyard-seat], and the
[installed-control measurement helper][measurements].

Captured sheet pairs can be made symmetric by averaging the two measurements'
absolute X values and their signed Y and Z coordinates, then assigning opposite
X signs to port and starboard. Reflect the normals into a common side before
averaging, then mirror X for the opposite side. The authored configurations
record which captures were combined; runtime placement uses those definitions
without repeating the averaging.

Halyard definitions can come from the
[geometry generator](MSS-command#fallback-halyard-generation) or from
[F9 surface captures](Winch-placement-diagnostics#capturing-a-proposed-position).
The captured replacements retain the measured donor mounting depths where the
configuration says so. An unresolved generator result does not prevent authoring
a measured seat separately. Native-scale
[model previews](Winch-placement-diagnostics#previewing-winch-models) display
the resulting mounting frame without reserving it.

## Source

This reference follows [revision `fcdf300`][revision], whose plugin metadata
version is **0.3.0**. The linked configuration and implementation are the source
of truth; recorded donor inspections describe their cited installed assets.

[revision]:
  https://github.com/sum-rock/MoreSailwindSails/tree/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5
[owned]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Runtime/OwnedWinch.cs
[group]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Runtime/WinchGroup.cs
[registry]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Stays/FishermansStay/FishermansStayRegistry.cs
[pair]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/SheetFallbackPair.cs
[inventory]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Discovery/NativeWinchInventory.cs
[brig]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/Boats/BrigControls.cs
[kakam]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/Boats/KakamControls.cs
[gloriana]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/Boats/GlorianaControls.cs
[chronian]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/Boats/ChronianControls.cs
[caelanor]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/Boats/CaelanorControls.cs
[shroud]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/Boats/ShroudControls.cs
[catalog]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/BoatControlCatalog.cs
[sheets]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Placement/SheetingWinchPlacementResolver.cs
[halyards]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Placement/HalyardWinchPlacementResolver.cs
[seat]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/SheetFallbackSeat.cs
[halyard-seat]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/HalyardFallbackSeat.cs
[measurements]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/tools/halyards/controls.py
[junk]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/Boats/JunkControls.cs
[jong]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/Boats/JongControls.cs
[sanbuq]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/Boats/SanbuqControls.cs
[baghala]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/Boats/LargeDhowControls.cs
[cog]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/Boats/CogControls.cs
[dhow]:
  https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Configuration/Boats/DhowControls.cs
