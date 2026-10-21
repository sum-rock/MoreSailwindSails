# Fallback winch templates

Part of the [Controls reference](Controls). This page describes the donor
objects used at authored fallback positions. See
[How winches are placed](How-winches-are-placed) for candidate ordering,
occupancy, reservations, and diagnostics.

## Template identity and ownership

A donor is an existing `GPButtonRopeWinch` referenced by a mast in the boat's
hierarchy. Fallback definitions do **not** register new boat-object prefabs or
allocate entries in `PrefabsDirectory`. The mod creates runtime clones of the
selected donors, parents them beneath placement mounts, and binds them to the
custom stay's rope controllers. The original donors retain their own bindings.

The runtime clones are referenced by the custom stay's `Mast` winch arrays and
tracked by `WinchControls`. An `OwnedWinchMarker` excludes them from native
donor discovery. Replacing a donor transfers the existing controller binding to
the replacement clone and destroys the old clone. These ownership rules are
implemented by [OwnedWinch][owned] and [WinchGroup][group].

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

The nine configured pairs use these donors. Configuration links include the
authored contact points, normals, offsets, and supporting-part requirements.

| Boat configuration   | Fallback pair     | Donor mast | Source array slot   | Donor object name              |
| -------------------- | ----------------- | ---------- | ------------------- | ------------------------------ |
| [Brig][brig]         | `Mainmast`        | 5          | `leftAngleWinch[0]` | `rope_winch_mastB_angle_left`  |
| [Brig][brig]         | `Mizzenmast`      | 7          | `leftAngleWinch[0]` | `rope_winch_mizzen_angle_left` |
| [Kakam][kakam]       | `Mainmast`        | 5          | `leftAngleWinch[0]` | `rope_winch_angle_left`        |
| [Gloriana][gloriana] | `Mainmast`        | 2          | `leftAngleWinch[0]` | `sheet_mainmast1_L`            |
| [Gloriana][gloriana] | `MainmastForward` | 2          | `leftAngleWinch[0]` | `sheet_mainmast1_L`            |
| [Chronian][chronian] | `Mainmast`        | 4          | `leftAngleWinch[0]` | `rope_mainmain_reef_port`      |
| [Chronian][chronian] | `Mizzenmast`      | 6          | `leftAngleWinch[0]` | `rope_mizenmain_reef_port`     |
| [Caelanor][caelanor] | `Mainmast`        | 5          | `leftAngleWinch[0]` | `sheet_main_main_L`            |
| [Shroud][shroud]     | `Foremast`        | 6          | `reefWinch[0]`      | `Coil_Foremast_Reef_1`         |

Brig and Kakam use wheel models. Gloriana, Chronian, Caelanor, and Shroud use
rope-coil models. Shroud deliberately takes its **sheet** template from a reef
array: `TemplateRole` identifies where to find the donor, while the clone's
binding determines which rope it controls.

Junk, Jong, Sanbuq, Baghala, Cog, and Dhow have no authored sheet fallback pairs
in this revision. They use the native candidates described in the placement
guide. The complete set of boat profiles is in [BoatControlCatalog][catalog].

If a pair's donor is missing or unusable, that pair cannot be allocated. The
allocator can try other eligible candidates; it does not substitute a donor from
another boat. Changes to a live donor array also invalidate a retained placement
if its selected donor no longer matches. See [sheet resolution][sheets].

## Halyard donors

A halyard fallback belongs to the requested aft **physical base mast**. Its
`HalyardFallbackSeat.TemplateIndex` selects
`requestedMast.reefWinch[TemplateIndex]`. For example, Brig mast **3** and
Gloriana mast **2** both select `reefWinch[1]` in their respective
configurations. Other mast definitions can select different indexes; consult the
boat profiles linked from [BoatControlCatalog][catalog].

The donor's original seat may be occupied, but the requested mast must be fitted
and the fallback destination must be available. A missing definition, array, or
usable donor leaves no halyard fallback candidate. Halyard selection is
independent of the sheet pair's template. See [halyard resolution][halyards].

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

[revision]:
  https://github.com/sum-rock/MoreSailwindSails/tree/d8d99742a28657fe7397b669a4160cfd0284094c
[owned]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Runtime/OwnedWinch.cs
[group]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Runtime/WinchGroup.cs
[registry]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Stays/FishermansStay/FishermansStayRegistry.cs
[pair]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Configuration/SheetFallbackPair.cs
[inventory]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Discovery/NativeWinchInventory.cs
[brig]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Configuration/Boats/BrigControls.cs
[kakam]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Configuration/Boats/KakamControls.cs
[gloriana]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Configuration/Boats/GlorianaControls.cs
[chronian]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Configuration/Boats/ChronianControls.cs
[caelanor]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Configuration/Boats/CaelanorControls.cs
[shroud]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Configuration/Boats/ShroudControls.cs
[catalog]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Configuration/BoatControlCatalog.cs
[sheets]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Placement/SheetingWinchPlacementResolver.cs
[halyards]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Placement/HalyardWinchPlacementResolver.cs
[seat]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Configuration/SheetFallbackSeat.cs
[halyard-seat]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/src/Controls/Configuration/HalyardFallbackSeat.cs
[measurements]:
  https://github.com/sum-rock/MoreSailwindSails/blob/d8d99742a28657fe7397b669a4160cfd0284094c/tools/halyards/controls.py
