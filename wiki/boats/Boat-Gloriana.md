# Gloriana rigging reference

**Boat identity:** `BOAT GLORIANA (182)`.  
**Inspected installation:** Sailwind **0.39.2**; OldChronian **0.6.0**.
Inspected on **2026-10-19**.

The aft mast labels use the bundle’s spelling “Mizenmast.” Mount 129 excludes
top section 4; mount 130 requires it. Both variants use donor 6.

## Objects used by the stays

Indexes below are `Mast.orderIndex` values. Labels, providers, and asset object
IDs were read from the installed files. Parent sections come from the
[authored ancestry map](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/BoatConfiguration/Boats/Gloriana.cs).
A parent of “base” means a physical base section; “—” means a stay-only mount.
The use column describes this mod’s references to the object.

| Index | Shipyard label | Use                   | Parent mast | Provider    | Asset object ID          |
| ----- | -------------- | --------------------- | ----------- | ----------- | ------------------------ |
| 1     | Foremast       | attachment            | base        | OldChronian | `B:-7937628128225587416` |
| 2     | Mainmast       | attachment            | base        | OldChronian | `B:-8461686923181141235` |
| 3     | Mizenmast Main | attachment            | base        | OldChronian | `B:3674658074862270702`  |
| 4     | Mizenmast Top  | attachment, exclusion | 3           | OldChronian | `B:-6840834180840745786` |
| 5     | Forestay       | donor                 | —           | OldChronian | `B:-324448584391129569`  |
| 6     | Mizenstay      | donor                 | —           | OldChronian | `B:9191426005328251561`  |

Asset keys identify the serialized file containing the **Mast component**:

- `B`: `CAB-c0f73b00560482830f0424a7fb47b797`.

## Stay configurations

These rows describe
[MoreSailwindSails configuration](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/GlorianaStays.cs),
not objects stored in the original boat assets. **Mount** is the new custom stay
index. **Donor**, **Fore**, **Aft**, **Required**, and **Excluded** refer to the
object table above. Required options must be fitted; exclusions must be absent
or unfitted. “—” means no exclusions. A donor supplies setup data and does not
itself have to be fitted.

### Foremast / mainmast

| Mount | Configuration                                                                                                                                                                        | Donor | Fore | Aft | Required | Excluded |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----- | ---- | --- | -------- | -------- |
| 128   | [Foremast / Mainmast](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/GlorianaStays.cs#L22) | 5     | 1    | 2   | 1, 2     | —        |

### Mainmast / mizzenmast

| Mount | Configuration                                                                                                                                                                              | Donor | Fore | Aft | Required | Excluded |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----- | ---- | --- | -------- | -------- |
| 129   | [Mainmast / Mizenmast Main](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/GlorianaStays.cs#L45) | 6     | 2    | 3   | 2, 3     | 4        |
| 130   | [Mainmast / Mizenmast Top](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/GlorianaStays.cs#L61)  | 6     | 2    | 4   | 2, 3, 4  | —        |

Attachment vectors, collar dimensions, and guide selection are linked from each
configuration. See [Fisherman's Stay](Fishermans-Stay) for their coordinate
frames and
[saved selections](Fishermans-Stay#shipyard-registration-and-saved-selections)
before changing mount numbers or option order.

[All boats](Boats)
