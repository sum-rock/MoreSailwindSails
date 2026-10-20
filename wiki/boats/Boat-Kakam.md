# Kakam rigging reference

**Boat identity:** `BOAT junk small singleroof(90)`.  
**Inspected installation:** Sailwind **0.39.2**; Shipyard Expansion **0.12.2**.
Inspected on **2026-10-19**.

The supporting physical masts are native, but donors 62 and 70 come from
Shipyard Expansion. Both configurations therefore require its rigging assets.

## Objects used by the stays

Indexes below are `Mast.orderIndex` values. Labels, providers, and asset object
IDs were read from the installed files. Parent sections come from the
[authored ancestry map](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/BoatConfiguration/Boats/Kakam.cs).
A parent of “base” means a physical base section; “—” means a stay-only mount.
The use column describes this mod’s references to the object.

| Index | Shipyard label | Use        | Parent mast | Provider           | Asset object ID         |
| ----- | -------------- | ---------- | ----------- | ------------------ | ----------------------- |
| 5     | main mast 1    | attachment | base        | Sailwind           | `N:176958`              |
| 6     | main mast 2    | attachment | base        | Sailwind           | `N:177821`              |
| 7     | mizzen mast    | attachment | base        | Sailwind           | `N:176959`              |
| 62    | middlestay     | donor      | —           | Shipyard Expansion | `B:7220019905627761814` |
| 70    | middlestay 2   | donor      | —           | Shipyard Expansion | `B:7440769462194201299` |

Asset keys identify the serialized file containing the **Mast component**:

- `B`: `CAB-027b2b1c061051d0fa7aa2e47cc92195`.
- `N`: `level24`.

## Stay configurations

These rows describe
[MoreSailwindSails configuration](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/KakamStays.cs),
not objects stored in the original boat assets. **Mount** is the new custom stay
index. **Donor**, **Fore**, **Aft**, **Required**, and **Excluded** refer to the
object table above. Required options must be fitted; exclusions must be absent
or unfitted. “—” means no exclusions. A donor supplies setup data and does not
itself have to be fitted.

### Mainmast / mizzenmast

| Mount | Configuration                                                                                                                                                                           | Donor | Fore | Aft | Required | Excluded |
| ----- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---- | --- | -------- | -------- |
| 128   | [main mast 1 / mizzen mast](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/KakamStays.cs#L16) | 62    | 5    | 7   | 5, 7     | —        |
| 129   | [main mast 2 / mizzen mast](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/KakamStays.cs#L32) | 70    | 6    | 7   | 6, 7     | —        |

Attachment vectors, collar dimensions, and guide selection are linked from each
configuration. See [Fisherman's Stay](Fishermans-Stay) for their coordinate
frames and
[saved selections](Fishermans-Stay#shipyard-registration-and-saved-selections)
before changing mount numbers or option order.

[All boats](Boats)
