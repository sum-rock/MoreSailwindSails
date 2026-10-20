# Brig rigging reference

**Boat identity:** `BOAT medi medium (50)`.  
**Inspected installation:** Sailwind **0.39.2**; Shipyard Expansion **0.12.2**.
Inspected on **2026-10-19**.

The numbered foremast, mainmast, and mizzenmast options are distinct objects. A
variant label describes the fitted combination; use the Fore and Aft columns to
determine the actual attachment sections, which can remain on a lower mast even
when a topmast is fitted.

## Objects used by the stays

Indexes below are `Mast.orderIndex` values. Labels, providers, and asset object
IDs were read from the installed files. Parent sections come from the
[authored ancestry map](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/BoatConfiguration/Boats/Brig.cs).
A parent of “base” means a physical base section; “—” means a stay-only mount.
The use column describes this mod’s references to the object.

| Index | Shipyard label      | Use                   | Parent mast | Provider           | Asset object ID          |
| ----- | ------------------- | --------------------- | ----------- | ------------------ | ------------------------ |
| 2     | foremast 2          | attachment            | base        | Sailwind           | `N:176346`               |
| 3     | foremast 1          | attachment            | base        | Sailwind           | `N:176389`               |
| 4     | main mast 2         | attachment            | base        | Sailwind           | `N:176390`               |
| 5     | main mast 1         | attachment            | base        | Sailwind           | `N:176391`               |
| 6     | mizzen mast 2       | attachment            | base        | Sailwind           | `N:176392`               |
| 7     | mizzen mast 1       | attachment            | base        | Sailwind           | `N:176393`               |
| 15    | middle top stay 1-1 | donor                 | —           | Sailwind           | `N:176402`               |
| 16    | middle top stay 1-2 | donor                 | —           | Sailwind           | `N:176403`               |
| 18    | middle top stay 2-1 | donor                 | —           | Sailwind           | `N:176407`               |
| 20    | middle top stay 2-2 | donor                 | —           | Sailwind           | `N:176405`               |
| 22    | mizzen top stay 1   | donor                 | —           | Sailwind           | `N:176411`               |
| 24    | mizzen top stay 2   | donor                 | —           | Sailwind           | `N:176409`               |
| 55    | fore topmast 1      | attachment, exclusion | 3           | Shipyard Expansion | `B:-1399400828695225380` |
| 56    | main topmast 1      | attachment, exclusion | 5           | Shipyard Expansion | `B:-5559596662385387392` |
| 57    | fore topmast 2      | attachment, exclusion | 2           | Shipyard Expansion | `B:-3347457321425232087` |
| 58    | main topmast 2      | attachment, exclusion | 4           | Shipyard Expansion | `B:413929370601754406`   |
| 59    | mizzen topmast 1    | attachment, exclusion | 7           | Shipyard Expansion | `B:-4019865861139852324` |
| 60    | mizzen topmast 2    | attachment, exclusion | 6           | Shipyard Expansion | `B:-7202118155475996431` |

Asset keys identify the serialized file containing the **Mast component**:

- `B`: `CAB-027b2b1c061051d0fa7aa2e47cc92195`.
- `N`: `level24`.

## Stay configurations

These rows describe
[MoreSailwindSails configuration](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs),
not objects stored in the original boat assets. **Mount** is the new custom stay
index. **Donor**, **Fore**, **Aft**, **Required**, and **Excluded** refer to the
object table above. Required options must be fitted; exclusions must be absent
or unfitted. “—” means no exclusions. A donor supplies setup data and does not
itself have to be fitted.

### Foremast / mainmast

| Mount | Configuration                                                                                                                                                                                 | Donor | Fore | Aft | Required     | Excluded |
| ----- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---- | --- | ------------ | -------- |
| 128   | [foremast 1 / main mast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L22)         | 15    | 3    | 5   | 3, 5         | 55, 56   |
| 129   | [foremast 1 / main topmast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L38)      | 15    | 3    | 56  | 3, 5, 56     | 55       |
| 130   | [fore topmast 1 / main mast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L56)     | 15    | 3    | 5   | 3, 5, 55     | 56       |
| 131   | [fore topmast 1 / main topmast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L72)  | 15    | 55   | 56  | 3, 5, 55, 56 | —        |
| 132   | [foremast 1 / main mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L90)         | 16    | 3    | 4   | 3, 4         | 55, 58   |
| 133   | [foremast 1 / main topmast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L106)     | 16    | 3    | 58  | 3, 4, 58     | 55       |
| 134   | [fore topmast 1 / main mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L124)    | 16    | 3    | 4   | 3, 4, 55     | 58       |
| 135   | [fore topmast 1 / main topmast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L140) | 16    | 55   | 58  | 3, 4, 55, 58 | —        |
| 136   | [foremast 2 / main mast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L158)        | 18    | 2    | 5   | 2, 5         | 56, 57   |
| 137   | [foremast 2 / main topmast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L174)     | 18    | 2    | 56  | 2, 5, 56     | 57       |
| 138   | [fore topmast 2 / main mast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L192)    | 18    | 2    | 5   | 2, 5, 57     | 56       |
| 139   | [fore topmast 2 / main topmast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L208) | 18    | 57   | 56  | 2, 5, 56, 57 | —        |
| 140   | [foremast 2 / main mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L226)        | 20    | 2    | 4   | 2, 4         | 57, 58   |
| 141   | [foremast 2 / main topmast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L242)     | 20    | 2    | 58  | 2, 4, 58     | 57       |
| 142   | [fore topmast 2 / main mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L260)    | 20    | 2    | 4   | 2, 4, 57     | 58       |
| 143   | [fore topmast 2 / main topmast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L276) | 20    | 57   | 58  | 2, 4, 57, 58 | —        |

### Mainmast / mizzenmast

| Mount | Configuration                                                                                                                                                                                   | Donor | Fore | Aft | Required     | Excluded |
| ----- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---- | --- | ------------ | -------- |
| 144   | [main mast 2 / mizzen mast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L300)       | 22    | 4    | 7   | 4, 7         | 58, 59   |
| 145   | [main mast 2 / mizzen topmast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L317)    | 22    | 4    | 59  | 4, 7, 59     | 58       |
| 146   | [main topmast 2 / mizzen mast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L334)    | 22    | 4    | 7   | 4, 7, 58     | 59       |
| 147   | [main topmast 2 / mizzen topmast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L351) | 22    | 4    | 59  | 4, 7, 58, 59 | —        |
| 148   | [main mast 2 / mizzen mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L368)       | 24    | 4    | 6   | 4, 6         | 58, 60   |
| 149   | [main mast 2 / mizzen topmast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L385)    | 24    | 4    | 60  | 4, 6, 60     | 58       |
| 150   | [main topmast 2 / mizzen mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L402)    | 24    | 4    | 6   | 4, 6, 58     | 60       |
| 151   | [main topmast 2 / mizzen topmast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs#L419) | 24    | 58   | 60  | 4, 6, 58, 60 | —        |

Attachment vectors, collar dimensions, and guide selection are linked from each
configuration. See [Fisherman's Stay](Fishermans-Stay) for their coordinate
frames and
[saved selections](Fishermans-Stay#shipyard-registration-and-saved-selections)
before changing mount numbers or option order.

[All boats](Boats)
