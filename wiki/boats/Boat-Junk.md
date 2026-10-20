# Junk rigging reference

**Boat identity:** `BOAT junk medium (80)`.  
**Inspected installation:** Sailwind **0.39.2**; Shipyard Expansion **0.12.2**.
Inspected on **2026-10-19**.

Indexes 16 and 61 both have the label “foremast stay” in the inspected assets.
They are distinct donors supplied by different packages. Resolve them by index
and asset identity.

## Objects used by the stays

Indexes below are `Mast.orderIndex` values. Labels, providers, and asset object
IDs were read from the installed files. Parent sections come from the
[authored ancestry map](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/BoatConfiguration/Boats/Junk.cs).
A parent of “base” means a physical base section; “—” means a stay-only mount.
The use column describes this mod’s references to the object.

| Index | Shipyard label     | Use                   | Parent mast | Provider           | Asset object ID          |
| ----- | ------------------ | --------------------- | ----------- | ------------------ | ------------------------ |
| 5     | middle stay 1      | donor                 | —           | Sailwind           | `N:177550`               |
| 6     | middle stay 2      | donor                 | —           | Sailwind           | `N:177549`               |
| 7     | middle stay 3      | donor                 | —           | Sailwind           | `N:177548`               |
| 9     | foremast           | attachment            | base        | Sailwind           | `N:177538`               |
| 10    | main mast 1        | attachment            | base        | Sailwind           | `N:177512`               |
| 11    | main mast 2        | attachment            | base        | Sailwind           | `N:177536`               |
| 12    | mizzen mast 1      | attachment            | base        | Sailwind           | `N:177539`               |
| 13    | mizzen mast 2      | attachment            | base        | Sailwind           | `N:177542`               |
| 16    | foremast stay      | donor                 | —           | Sailwind           | `N:177574`               |
| 52    | mizzen topstay 2-3 | donor                 | —           | Shipyard Expansion | `B:295449522806650863`   |
| 53    | mizzen topmast     | attachment, exclusion | 12          | Shipyard Expansion | `B:-4431819781822095073` |
| 57    | mizzen mast 3      | attachment            | base        | Shipyard Expansion | `B:-3184279678302632393` |
| 58    | raked foremast     | attachment            | base        | Shipyard Expansion | `B:-662675937051401679`  |
| 61    | foremast stay      | donor                 | —           | Shipyard Expansion | `B:-2154889157703801618` |
| 62    | fore topmast       | attachment, exclusion | 9           | Shipyard Expansion | `B:1859122082183086496`  |

Asset keys identify the serialized file containing the **Mast component**:

- `B`: `CAB-027b2b1c061051d0fa7aa2e47cc92195`.
- `N`: `level24`.

## Stay configurations

These rows describe
[MoreSailwindSails configuration](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JunkStays.cs),
not objects stored in the original boat assets. **Mount** is the new custom stay
index. **Donor**, **Fore**, **Aft**, **Required**, and **Excluded** refer to the
object table above. Required options must be fitted; exclusions must be absent
or unfitted. “—” means no exclusions. A donor supplies setup data and does not
itself have to be fitted.

### Foremast / mainmast

| Mount | Configuration                                                                                                                                                                             | Donor | Fore | Aft | Required  | Excluded |
| ----- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---- | --- | --------- | -------- |
| 128   | [foremast / main mast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JunkStays.cs#L22)       | 16    | 9    | 10  | 9, 10     | 62       |
| 129   | [fore topmast / main mast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JunkStays.cs#L40)   | 16    | 62   | 10  | 9, 10, 62 | —        |
| 130   | [raked foremast / main mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JunkStays.cs#L58) | 61    | 58   | 11  | 11, 58    | —        |

### Mainmast / mizzenmast

| Mount | Configuration                                                                                                                                                                              | Donor | Fore | Aft | Required   | Excluded |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----- | ---- | --- | ---------- | -------- |
| 131   | [main mast 1 / mizzen mast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JunkStays.cs#L82)   | 5     | 10   | 12  | 10, 12     | 53       |
| 132   | [main mast 1 / mizzen topmast](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JunkStays.cs#L100) | 5     | 10   | 53  | 10, 12, 53 | —        |
| 133   | [main mast 2 / mizzen mast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JunkStays.cs#L118)  | 6     | 11   | 12  | 11, 12     | 53       |
| 134   | [main mast 2 / mizzen topmast](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JunkStays.cs#L136) | 6     | 11   | 53  | 11, 12, 53 | —        |
| 135   | [main mast 2 / mizzen mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JunkStays.cs#L154)  | 7     | 11   | 13  | 11, 13     | —        |
| 136   | [main mast 2 / mizzen mast 3](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JunkStays.cs#L172)  | 52    | 11   | 57  | 11, 57     | —        |

Attachment vectors, collar dimensions, and guide selection are linked from each
configuration. See [Fisherman's Stay](Fishermans-Stay) for their coordinate
frames and
[saved selections](Fishermans-Stay#shipyard-registration-and-saved-selections)
before changing mount numbers or option order.

[All boats](Boats)
