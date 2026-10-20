# Cog rigging reference

**Boat identity:** `BOAT medi small (40)`.  
**Inspected installation:** Sailwind **0.39.2**; Shipyard Expansion **0.12.2**.
Inspected on **2026-10-19**.

All three configurations need a Shipyard Expansion donor. Having the supporting
native masts alone does not provide the donor needed to construct these stays.

## Objects used by the stays

Indexes below are `Mast.orderIndex` values. Labels, providers, and asset object
IDs were read from the installed files. Parent sections come from the
[authored ancestry map](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/BoatConfiguration/Boats/Cog.cs).
A parent of “base” means a physical base section; “—” means a stay-only mount.
The use column describes this mod’s references to the object.

| Index | Shipyard label | Use        | Parent mast | Provider           | Asset object ID         |
| ----- | -------------- | ---------- | ----------- | ------------------ | ----------------------- |
| 5     | main mast 2    | attachment | base        | Sailwind           | `N:176207`              |
| 6     | main mast 1    | attachment | base        | Sailwind           | `N:177380`              |
| 8     | mizzen mast    | attachment | base        | Sailwind           | `N:176221`              |
| 51    | midstay 2-1    | donor      | —           | Shipyard Expansion | `B:7220131258585403214` |
| 57    | mizzen mast 2  | attachment | base        | Shipyard Expansion | `B:371826162313457304`  |
| 58    | midstay 2-2    | donor      | —           | Shipyard Expansion | `B:470520046885896473`  |
| 65    | midstay 1-1    | donor      | —           | Shipyard Expansion | `B:4552605193688549974` |

Asset keys identify the serialized file containing the **Mast component**:

- `B`: `CAB-027b2b1c061051d0fa7aa2e47cc92195`.
- `N`: `level24`.

## Stay configurations

These rows describe
[MoreSailwindSails configuration](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/CogStays.cs),
not objects stored in the original boat assets. **Mount** is the new custom stay
index. **Donor**, **Fore**, **Aft**, **Required**, and **Excluded** refer to the
object table above. Required options must be fitted; exclusions must be absent
or unfitted. “—” means no exclusions. A donor supplies setup data and does not
itself have to be fitted.

### Mainmast / mizzenmast

| Mount | Configuration                                                                                                                                                                           | Donor | Fore | Aft | Required | Excluded |
| ----- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---- | --- | -------- | -------- |
| 128   | [main mast 2 / mizzen mast](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/CogStays.cs#L22)   | 51    | 5    | 8   | 5, 8     | —        |
| 129   | [main mast 1 / mizzen mast](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/CogStays.cs#L39)   | 65    | 6    | 8   | 6, 8     | —        |
| 130   | [main mast 2 / mizzen mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/CogStays.cs#L56) | 58    | 5    | 57  | 5, 57    | —        |

Attachment vectors, collar dimensions, and guide selection are linked from each
configuration. See [Fisherman's Stay](Fishermans-Stay) for their coordinate
frames and
[saved selections](Fishermans-Stay#shipyard-registration-and-saved-selections)
before changing mount numbers or option order.

[All boats](Boats)
