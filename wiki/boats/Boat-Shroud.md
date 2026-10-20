# Shroud rigging reference

**Boat identity:** `BOAT Shroud Large`.  
**Inspected installation:** Sailwind **0.39.2**; Shattered Seas **0.1.3**.
Inspected on **2026-10-19**.

The short and tall mast options have separate indexes. All referenced objects
below belong to the Shattered Seas boat bundle; they are not base-game or
Shipyard Expansion indexes.

## Objects used by the stays

Indexes below are `Mast.orderIndex` values. Labels, providers, and asset object
IDs were read from the installed files. Parent sections come from the
[authored ancestry map](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/BoatConfiguration/Boats/Shroud.cs).
A parent of “base” means a physical base section; “—” means a stay-only mount.
The use column describes this mod’s references to the object.

| Index | Shipyard label     | Use        | Parent mast | Provider                 | Asset object ID          |
| ----- | ------------------ | ---------- | ----------- | ------------------------ | ------------------------ |
| 5     | Foremast 1 Tall    | attachment | base        | Shattered Seas Expansion | `B:-1172948098651274865` |
| 6     | Foremast 1         | attachment | base        | Shattered Seas Expansion | `B:-6770402354118084709` |
| 7     | Mainmast 1 Tall    | attachment | base        | Shattered Seas Expansion | `B:-1909864090603959860` |
| 8     | Mainmast 1         | attachment | base        | Shattered Seas Expansion | `B:-3544906538865794525` |
| 9     | Mizzenmast 1 Tall  | attachment | base        | Shattered Seas Expansion | `B:-2050395793651268794` |
| 10    | Mizzen 1           | attachment | base        | Shattered Seas Expansion | `B:1994044198287546262`  |
| 24    | Outer Mainstay 1   | donor      | —           | Shattered Seas Expansion | `B:5162944507795792409`  |
| 25    | Outer Mizzenstay 1 | donor      | —           | Shattered Seas Expansion | `B:8536062513178094732`  |

Asset keys identify the serialized file containing the **Mast component**:

- `B`: `CAB-e62a998cc081e6924ad1637bbe4b3d66`.

## Stay configurations

These rows describe
[MoreSailwindSails configuration](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/ShroudStays.cs),
not objects stored in the original boat assets. **Mount** is the new custom stay
index. **Donor**, **Fore**, **Aft**, **Required**, and **Excluded** refer to the
object table above. Required options must be fitted; exclusions must be absent
or unfitted. “—” means no exclusions. A donor supplies setup data and does not
itself have to be fitted.

### Foremast / mainmast

| Mount | Configuration                                                                                                                                                                                    | Donor | Fore | Aft | Required | Excluded |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----- | ---- | --- | -------- | -------- |
| 128   | [Foremast 1 / Mainmast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/ShroudStays.cs#L22)           | 24    | 6    | 8   | 6, 8     | —        |
| 129   | [Foremast 1 Tall / Mainmast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/ShroudStays.cs#L40)      | 24    | 5    | 8   | 5, 8     | —        |
| 130   | [Foremast 1 / Mainmast 1 Tall](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/ShroudStays.cs#L57)      | 24    | 6    | 7   | 6, 7     | —        |
| 131   | [Foremast 1 Tall / Mainmast 1 Tall](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/ShroudStays.cs#L75) | 24    | 5    | 7   | 5, 7     | —        |

### Mainmast / mizzenmast

| Mount | Configuration                                                                                                                                                                                       | Donor | Fore | Aft | Required | Excluded |
| ----- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---- | --- | -------- | -------- |
| 132   | [Mainmast 1 / Mizzen 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/ShroudStays.cs#L98)                | 25    | 8    | 10  | 8, 10    | —        |
| 133   | [Mainmast 1 Tall / Mizzen 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/ShroudStays.cs#L116)          | 25    | 7    | 10  | 7, 10    | —        |
| 134   | [Mainmast 1 / Mizzenmast 1 Tall](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/ShroudStays.cs#L134)      | 25    | 8    | 9   | 8, 9     | —        |
| 135   | [Mainmast 1 Tall / Mizzenmast 1 Tall](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/ShroudStays.cs#L152) | 25    | 7    | 9   | 7, 9     | —        |

Attachment vectors, collar dimensions, and guide selection are linked from each
configuration. See [Fisherman's Stay](Fishermans-Stay) for their coordinate
frames and
[saved selections](Fishermans-Stay#shipyard-registration-and-saved-selections)
before changing mount numbers or option order.

[All boats](Boats)
