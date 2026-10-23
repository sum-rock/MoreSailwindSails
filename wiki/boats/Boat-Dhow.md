# Dhow rigging reference

**Boat identity:** `BOAT dhow small (10)`.  
**Inspected installation:** Sailwind **0.39.2**; Shipyard Expansion **0.12.2**.
Inspected on **2026-10-19**.

Both rows describe the same mount **128** and the same shipyard option. The
forward endpoint switches between short mast 6 and tall mast 7. The mizzenmast
and donor come from Shipyard Expansion.

## Objects used by the stays

Indexes below are `Mast.orderIndex` values. Labels, providers, and asset object
IDs were read from the installed files. Parent sections come from the
[authored ancestry map](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/BoatConfiguration/Boats/Dhow.cs).
A parent of “base” means a physical base section; “—” means a stay-only mount.
The use column describes this mod’s references to the object.

| Index | Shipyard label | Use        | Parent mast | Provider           | Asset object ID          |
| ----- | -------------- | ---------- | ----------- | ------------------ | ------------------------ |
| 6     | short mast     | attachment | base        | Sailwind           | `N:174956`               |
| 7     | tall mast      | attachment | base        | Sailwind           | `N:175270`               |
| 51    | mizzen mast    | attachment | base        | Shipyard Expansion | `B:-6279031102539392348` |
| 54    | middlestay     | donor      | —           | Shipyard Expansion | `B:-6016662364342948841` |

Asset keys identify the serialized file containing the **Mast component**:

- `B`: `CAB-027b2b1c061051d0fa7aa2e47cc92195`.
- `N`: `level24`.

## Stay configurations

These rows describe
[MoreSailwindSails configuration](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/DhowStays.cs),
not objects stored in the original boat assets. **Mount** is the new custom stay
index. **Donor**, **Fore**, **Aft**, **Required**, and **Excluded** refer to the
object table above. Required options must be fitted; exclusions must be absent
or unfitted. “—” means no exclusions. A donor supplies setup data and does not
itself have to be fitted.

### Mainmast / mizzenmast

| Mount | Configuration                                                                                                                                                                      | Donor | Fore | Aft | Required | Excluded |
| ----- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---- | --- | -------- | -------- |
| 128   | [mainmast / mizzenmast](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/DhowStays.cs#L17) | 54    | 6    | 51  | 6, 51    | —        |
| 128   | [mainmast / mizzenmast](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/DhowStays.cs#L34) | 54    | 7    | 51  | 7, 51    | —        |

Attachment vectors, collar dimensions, and guide selection are linked from each
configuration. See [Fisherman's Stay](Fishermans-Stay) for their coordinate
frames and
[saved selections](Fishermans-Stay#shipyard-registration-and-saved-selections)
before changing mount numbers or option order.

[All boats](Boats)
