# Caelanor rigging reference

**Boat identity:** `BOAT CAELANOR (192)`.  
**Inspected installation:** Sailwind **0.39.2**; OldChronian **0.6.0**.
Inspected on **2026-10-19**.

This configuration uses aft mainmast sections 5 and 6. It does not use the
alternative Midmast sections 3 and 4. The owned guide is aligned in height to
the aft endpoint, and the variant has no exclusions for optional T’gallant
sections.

## Objects used by the stays

Indexes below are `Mast.orderIndex` values. Labels, providers, and asset object
IDs were read from the installed files. Parent sections come from the
[authored ancestry map](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/BoatConfiguration/Boats/Caelanor.cs).
A parent of “base” means a physical base section; “—” means a stay-only mount.
The use column describes this mod’s references to the object.

| Index | Shipyard label | Use              | Parent mast | Provider    | Asset object ID          |
| ----- | -------------- | ---------------- | ----------- | ----------- | ------------------------ |
| 1     | Foremast Main  | required support | base        | OldChronian | `B:-1808901055417920971` |
| 2     | Foremast Top   | attachment       | 1           | OldChronian | `B:8907339594828990725`  |
| 5     | Mainmast Main  | required support | base        | OldChronian | `B:-6801721206716432579` |
| 6     | Mainmast Top   | attachment       | 5           | OldChronian | `B:-6551987479535673024` |
| 14    | Main Royalstay | donor            | —           | OldChronian | `B:6375001669388967116`  |

Asset keys identify the serialized file containing the **Mast component**:

- `B`: `CAB-2008c073de5acd7892c5cfa24f6ab6d6`.

## Stay configurations

These rows describe
[MoreSailwindSails configuration](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/CaelanorStays.cs),
not objects stored in the original boat assets. **Mount** is the new custom stay
index. **Donor**, **Fore**, **Aft**, **Required**, and **Excluded** refer to the
object table above. Required options must be fitted; exclusions must be absent
or unfitted. “—” means no exclusions. A donor supplies setup data and does not
itself have to be fitted.

### Foremast / mainmast

| Mount | Configuration                                                                                                                                                                                | Donor | Fore | Aft | Required   | Excluded |
| ----- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---- | --- | ---------- | -------- |
| 128   | [Foremast Top / Mainmast Top](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/CaelanorStays.cs#L16) | 14    | 2    | 6   | 1, 2, 5, 6 | —        |

Attachment vectors, collar dimensions, and guide selection are linked from each
configuration. See [Fisherman's Stay](Fishermans-Stay) for their coordinate
frames and
[saved selections](Fishermans-Stay#shipyard-registration-and-saved-selections)
before changing mount numbers or option order.

[All boats](Boats)
