# Sanbuq rigging reference

**Boat identity:** `BOAT dhow medium (20)`.  
**Inspected installation:** Sailwind **0.39.2**; Shipyard Expansion **0.12.2**.
Inspected on **2026-10-19**.

Native topmasts 13 and 14 belong to base masts 10 and 11. The additional
foremast and mizzen arrangements in this table come from Shipyard Expansion.

## Objects used by the stays

Indexes below are `Mast.orderIndex` values. Labels, providers, and asset object
IDs were read from the installed files. Parent sections come from the
[authored ancestry map](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/BoatConfiguration/Boats/Sanbuq.cs).
A parent of “base” means a physical base section; “—” means a stay-only mount.
The use column describes this mod’s references to the object.

| Index | Shipyard label     | Use                   | Parent mast | Provider           | Asset object ID          |
| ----- | ------------------ | --------------------- | ----------- | ------------------ | ------------------------ |
| 9     | middle stay        | donor                 | —           | Sailwind           | `N:177954`               |
| 10    | mast 1             | attachment            | base        | Sailwind           | `N:176864`               |
| 11    | mast 2             | attachment            | base        | Sailwind           | `N:177927`               |
| 12    | mizzen mast        | attachment            | base        | Sailwind           | `N:175818`               |
| 13    | topmast 1          | attachment, exclusion | 10          | Sailwind           | `N:177957`               |
| 14    | topmast 2          | attachment, exclusion | 11          | Sailwind           | `N:177956`               |
| 51    | foremast 1         | attachment            | base        | Shipyard Expansion | `B:5531684820488636084`  |
| 54    | mizzen stay 1-2    | donor                 | —           | Shipyard Expansion | `B:-4734527574801850326` |
| 55    | mizzen mast 2      | attachment            | base        | Shipyard Expansion | `B:6907941193626863712`  |
| 57    | middle stay f-2    | donor                 | —           | Shipyard Expansion | `B:159279460030207807`   |
| 59    | mizzen topmast 1   | attachment, exclusion | 55          | Shipyard Expansion | `B:-189832570321696306`  |
| 61    | mizzen stay 1-3    | donor                 | —           | Shipyard Expansion | `B:6084280672632492573`  |
| 62    | raked foremast     | attachment            | base        | Shipyard Expansion | `B:5905330390296313675`  |
| 64    | fore topmast       | attachment, exclusion | 51          | Shipyard Expansion | `B:1090416404975902927`  |
| 66    | mizzen stay 2-2    | donor                 | —           | Shipyard Expansion | `B:2358509844322658984`  |
| 68    | main topstay 2     | donor                 | —           | Shipyard Expansion | `B:2771968494131659253`  |
| 69    | mizzen mast 3      | attachment            | base        | Shipyard Expansion | `B:3827359847053108870`  |
| 70    | mizzen topmast 2   | attachment, exclusion | 69          | Shipyard Expansion | `B:4601423590045525473`  |
| 80    | mizzen topmast 3   | attachment, exclusion | 12          | Shipyard Expansion | `B:-3800533611567445198` |
| 81    | mizzen topstay 1-3 | donor                 | —           | Shipyard Expansion | `B:8410147896412493871`  |

Asset keys identify the serialized file containing the **Mast component**:

- `B`: `CAB-027b2b1c061051d0fa7aa2e47cc92195`.
- `N`: `level24`.

## Stay configurations

These rows describe
[MoreSailwindSails configuration](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs),
not objects stored in the original boat assets. **Mount** is the new custom stay
index. **Donor**, **Fore**, **Aft**, **Required**, and **Excluded** refer to the
object table above. Required options must be fitted; exclusions must be absent
or unfitted. “—” means no exclusions. A donor supplies setup data and does not
itself have to be fitted.

### Mainmast / mizzenmast

| Mount | Configuration                                                                                                                                                                                | Donor | Fore | Aft | Required       | Excluded |
| ----- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---- | --- | -------------- | -------- |
| 128   | [mast 1 / mizzen mast](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L22)          | 9     | 10   | 12  | 10, 12         | 13, 80   |
| 129   | [mast 1 / mizzen topmast 3](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L40)     | 9     | 10   | 80  | 10, 12, 80     | 13       |
| 130   | [topmast 1 / mizzen mast](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L58)       | 9     | 10   | 12  | 10, 12, 13     | 80       |
| 131   | [topmast 1 / mizzen topmast 3](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L76)  | 9     | 13   | 80  | 10, 12, 13, 80 | —        |
| 132   | [mast 1 / mizzen mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L94)        | 54    | 10   | 55  | 10, 55         | 13, 59   |
| 133   | [mast 1 / mizzen topmast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L112)    | 54    | 10   | 59  | 10, 55, 59     | 13       |
| 134   | [topmast 1 / mizzen mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L130)    | 54    | 10   | 55  | 10, 13, 55     | 59       |
| 135   | [topmast 1 / mizzen topmast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L148) | 54    | 13   | 59  | 10, 13, 55, 59 | —        |
| 136   | [mast 2 / mizzen mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L166)       | 66    | 11   | 55  | 11, 55         | 14, 59   |
| 137   | [mast 2 / mizzen topmast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L184)    | 66    | 11   | 59  | 11, 55, 59     | 14       |
| 138   | [topmast 2 / mizzen mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L202)    | 66    | 11   | 55  | 11, 14, 55     | 59       |
| 139   | [topmast 2 / mizzen topmast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L220) | 66    | 14   | 59  | 11, 14, 55, 59 | —        |
| 140   | [mast 1 / mizzen mast 3](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L238)       | 61    | 10   | 69  | 10, 69         | 13, 70   |
| 141   | [mast 1 / mizzen topmast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L256)    | 61    | 10   | 70  | 10, 69, 70     | 13       |
| 142   | [topmast 1 / mizzen mast 3](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L274)    | 61    | 10   | 69  | 10, 13, 69     | 70       |
| 143   | [topmast 1 / mizzen topmast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L292) | 61    | 13   | 70  | 10, 13, 69, 70 | —        |
| 144   | [mast 2 / mizzen mast](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L310)         | 81    | 11   | 12  | 11, 12         | 14, 80   |
| 145   | [mast 2 / mizzen topmast 3](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L328)    | 81    | 11   | 80  | 11, 12, 80     | 14       |
| 146   | [topmast 2 / mizzen mast](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L346)      | 81    | 11   | 12  | 11, 12, 14     | 80       |
| 147   | [topmast 2 / mizzen topmast 3](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L364) | 81    | 14   | 80  | 11, 12, 14, 80 | —        |

### Foremast / mainmast

| Mount | Configuration                                                                                                                                                                              | Donor | Fore | Aft | Required       | Excluded |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----- | ---- | --- | -------------- | -------- |
| 148   | [foremast 1 / mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L388)        | 57    | 51   | 11  | 11, 51         | 14, 64   |
| 149   | [foremast 1 / topmast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L406)     | 57    | 51   | 14  | 11, 14, 51     | 64       |
| 150   | [fore topmast / mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L424)      | 57    | 51   | 11  | 11, 51, 64     | 14       |
| 151   | [fore topmast / topmast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L442)   | 57    | 64   | 14  | 11, 14, 51, 64 | —        |
| 152   | [raked foremast / mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L460)    | 68    | 62   | 11  | 11, 62         | 14       |
| 153   | [raked foremast / topmast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/SanbuqStays.cs#L478) | 68    | 62   | 14  | 11, 14, 62     | —        |

Attachment vectors, collar dimensions, and guide selection are linked from each
configuration. See [Fisherman's Stay](Fishermans-Stay) for their coordinate
frames and
[saved selections](Fishermans-Stay#shipyard-registration-and-saved-selections)
before changing mount numbers or option order.

[All boats](Boats)
