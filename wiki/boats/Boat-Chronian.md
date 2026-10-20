# Chronian rigging reference

**Boat identity:** `BOAT CHRONIAN (187)`.  
**Inspected installation:** Sailwind **0.39.2**; OldChronian **0.6.0**.
Inspected on **2026-10-19**.

Mount 129 excludes existing stay 14, the Mizzen Royalstay. Mount 128 has no
exclusions. Neither configuration excludes the optional T’gallant sections. Both
use an owned guide aligned in height to the authored aft endpoint.

## Objects used by the stays

Indexes below are `Mast.orderIndex` values. Labels, providers, and asset object
IDs were read from the installed files. Parent sections come from the
[authored ancestry map](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/BoatConfiguration/Boats/Chronian.cs).
A parent of “base” means a physical base section; “—” means a stay-only mount.
The use column describes this mod’s references to the object.

| Index | Shipyard label   | Use              | Parent mast | Provider    | Asset object ID          |
| ----- | ---------------- | ---------------- | ----------- | ----------- | ------------------------ |
| 2     | Foremast Main    | required support | base        | OldChronian | `B:1435407608494483730`  |
| 3     | Foremast Top     | attachment       | 2           | OldChronian | `B:9035458245884378285`  |
| 4     | Mainmast Main    | attachment       | base        | OldChronian | `B:-8460605858212477400` |
| 5     | Mainmast Top     | attachment       | 4           | OldChronian | `B:1629203650150949607`  |
| 6     | Mizzenmast Main  | required support | base        | OldChronian | `B:-6062975483597191405` |
| 7     | Mizzenmast Top   | attachment       | 6           | OldChronian | `B:334377573238661525`   |
| 12    | Main Royalstay   | donor            | —           | OldChronian | `B:-7978759490997406664` |
| 14    | Mizzen Royalstay | exclusion        | —           | OldChronian | `B:4574228261412151456`  |

Asset keys identify the serialized file containing the **Mast component**:

- `B`: `CAB-d06ee22d0e16c2f725b007bdbf53794b`.

## Stay configurations

These rows describe
[MoreSailwindSails configuration](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/ChronianStays.cs),
not objects stored in the original boat assets. **Mount** is the new custom stay
index. **Donor**, **Fore**, **Aft**, **Required**, and **Excluded** refer to the
object table above. Required options must be fitted; exclusions must be absent
or unfitted. “—” means no exclusions. A donor supplies setup data and does not
itself have to be fitted.

### Foremast / mainmast

| Mount | Configuration                                                                                                                                                                                | Donor | Fore | Aft | Required   | Excluded |
| ----- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---- | --- | ---------- | -------- |
| 128   | [Foremast Top / Mainmast Top](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/ChronianStays.cs#L22) | 12    | 3    | 5   | 2, 3, 4, 5 | —        |

### Mainmast / mizzenmast

| Mount | Configuration                                                                                                                                                                              | Donor | Fore | Aft | Required   | Excluded |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----- | ---- | --- | ---------- | -------- |
| 129   | [Mainmast / Mizzenmast Top](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/ChronianStays.cs#L46) | 12    | 4    | 7   | 4, 5, 6, 7 | 14       |

Attachment vectors, collar dimensions, and guide selection are linked from each
configuration. See [Fisherman's Stay](Fishermans-Stay) for their coordinate
frames and
[saved selections](Fishermans-Stay#shipyard-registration-and-saved-selections)
before changing mount numbers or option order.

[All boats](Boats)
