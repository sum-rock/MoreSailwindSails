# Jong rigging reference

**Boat identity:** `BOAT junk large (70)`.  
**Inspected installation:** Sailwind **0.39.2**; Shipyard Expansion **0.12.2**.
Inspected on **2026-10-19**.

The two mainmast positions and their forward alternatives produce several
different mast pairs. Keep the authored group order when extending the shipyard
options.

## Objects used by the stays

Indexes below are `Mast.orderIndex` values. Labels, providers, and asset object
IDs were read from the installed files. Parent sections come from the
[authored ancestry map](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/BoatConfiguration/Boats/Jong.cs).
A parent of “base” means a physical base section; “—” means a stay-only mount.
The use column describes this mod’s references to the object.

| Index | Shipyard label   | Use        | Parent mast | Provider           | Asset object ID          |
| ----- | ---------------- | ---------- | ----------- | ------------------ | ------------------------ |
| 1     | foremast         | attachment | base        | Sailwind           | `N:176721`               |
| 2     | main mast 1      | attachment | base        | Sailwind           | `N:176718`               |
| 3     | main mast 2      | attachment | base        | Sailwind           | `N:176719`               |
| 4     | mizzen mast      | attachment | base        | Sailwind           | `N:176720`               |
| 10    | main stay 2      | donor      | —           | Sailwind           | `N:176731`               |
| 12    | middle stay      | donor      | —           | Sailwind           | `N:176729`               |
| 13    | mizzen stay      | donor      | —           | Sailwind           | `N:176737`               |
| 51    | main mast 1 fore | attachment | base        | Shipyard Expansion | `B:3435880615456936629`  |
| 52    | main mast 2 fore | attachment | base        | Shipyard Expansion | `B:-5094820933390530960` |
| 53    | mizzen mast 2    | attachment | base        | Shipyard Expansion | `B:5830495717278163118`  |
| 55    | middle stay 2-2  | donor      | —           | Shipyard Expansion | `B:-182568603314608511`  |
| 56    | mizzen stay 2-2  | donor      | —           | Shipyard Expansion | `B:850588142706746230`   |
| 58    | middle stay 1-2  | donor      | —           | Shipyard Expansion | `B:8023672398005240557`  |
| 71    | mizzen stay 1-2  | donor      | —           | Shipyard Expansion | `B:3920624721095551448`  |
| 73    | middle stay 2-f  | donor      | —           | Shipyard Expansion | `B:8040526548158034860`  |
| 75    | mizzen stay 2-f  | donor      | —           | Shipyard Expansion | `B:-3820857628395480682` |

Asset keys identify the serialized file containing the **Mast component**:

- `B`: `CAB-027b2b1c061051d0fa7aa2e47cc92195`.
- `N`: `level24`.

## Stay configurations

These rows describe
[MoreSailwindSails configuration](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JongStays.cs),
not objects stored in the original boat assets. **Mount** is the new custom stay
index. **Donor**, **Fore**, **Aft**, **Required**, and **Excluded** refer to the
object table above. Required options must be fitted; exclusions must be absent
or unfitted. “—” means no exclusions. A donor supplies setup data and does not
itself have to be fitted.

### Foremast / mainmast 1

| Mount | Configuration                                                                                                                                                                       | Donor | Fore | Aft | Required | Excluded |
| ----- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---- | --- | -------- | -------- |
| 128   | [foremast / main mast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JongStays.cs#L22) | 10    | 1    | 2   | 1, 2     | —        |

### Mainmast 1 / mainmast 2

| Mount | Configuration                                                                                                                                                                                    | Donor | Fore | Aft | Required | Excluded |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----- | ---- | --- | -------- | -------- |
| 129   | [main mast 1 / main mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JongStays.cs#L44)           | 12    | 2    | 3   | 2, 3     | —        |
| 130   | [main mast 1 fore / main mast 2 fore](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JongStays.cs#L60) | 55    | 51   | 52  | 51, 52   | —        |
| 131   | [main mast 1 fore / main mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JongStays.cs#L76)      | 58    | 51   | 3   | 3, 51    | —        |

### Foremast / mainmast 2

| Mount | Configuration                                                                                                                                                                            | Donor | Fore | Aft | Required | Excluded |
| ----- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---- | --- | -------- | -------- |
| 132   | [foremast / main mast 2 fore](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JongStays.cs#L98) | 73    | 1    | 52  | 1, 52    | —        |

### Mainmast 2 / mizzenmast

| Mount | Configuration                                                                                                                                                                                  | Donor | Fore | Aft | Required | Excluded |
| ----- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---- | --- | -------- | -------- |
| 133   | [main mast 2 / mizzen mast](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JongStays.cs#L120)        | 13    | 3    | 4   | 3, 4     | —        |
| 134   | [main mast 2 fore / mizzen mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JongStays.cs#L136) | 56    | 52   | 53  | 52, 53   | —        |
| 135   | [main mast 2 fore / mizzen mast](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JongStays.cs#L152)   | 71    | 52   | 4   | 4, 52    | —        |

### Mainmast 1 / mizzenmast

| Mount | Configuration                                                                                                                                                                             | Donor | Fore | Aft | Required | Excluded |
| ----- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---- | --- | -------- | -------- |
| 136   | [main mast 1 / mizzen mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/JongStays.cs#L174) | 75    | 2    | 53  | 2, 53    | —        |

Attachment vectors, collar dimensions, and guide selection are linked from each
configuration. See [Fisherman's Stay](Fishermans-Stay) for their coordinate
frames and
[saved selections](Fishermans-Stay#shipyard-registration-and-saved-selections)
before changing mount numbers or option order.

[All boats](Boats)
