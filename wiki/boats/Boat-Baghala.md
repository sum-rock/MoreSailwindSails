# Baghala rigging reference

**Boat identity:** `BOAT dhow large (30)`.  
**Inspected installation:** Sailwind **0.39.2**. Inspected on **2026-10-19**.

Baghala is named `LargeDhow` in the source. All objects referenced by these
stays are present in the inspected native game assets. Main topmasts 3 and 5
depend on masts 2 and 4 respectively; that dependency is represented explicitly
even though their transforms are siblings.

## Objects used by the stays

Indexes below are `Mast.orderIndex` values. Labels, providers, and asset object
IDs were read from the installed files. Parent sections come from the
[authored ancestry map](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/BoatConfiguration/Boats/LargeDhow.cs).
A parent of “base” means a physical base section; “—” means a stay-only mount.
The use column describes this mod’s references to the object.

| Index | Shipyard label  | Use                   | Parent mast | Provider | Asset object ID |
| ----- | --------------- | --------------------- | ----------- | -------- | --------------- |
| 0     | foremast 1      | attachment            | base        | Sailwind | `N:177017`      |
| 1     | foremast 2      | attachment            | base        | Sailwind | `N:177826`      |
| 2     | main mast 1     | attachment            | base        | Sailwind | `N:175586`      |
| 3     | main topmast 1  | attachment, exclusion | 2           | Sailwind | `N:176273`      |
| 4     | main mast 2     | attachment            | base        | Sailwind | `N:174951`      |
| 5     | main topmast 2  | attachment, exclusion | 4           | Sailwind | `N:174744`      |
| 6     | mizzen mast 1   | attachment            | base        | Sailwind | `N:175993`      |
| 7     | mizzen mast 2   | attachment            | base        | Sailwind | `N:176621`      |
| 8     | mizzen mast 3   | attachment            | base        | Sailwind | `N:174910`      |
| 20    | middle stay 1-1 | donor                 | —           | Sailwind | `N:177197`      |
| 23    | middle stay 1-2 | donor                 | —           | Sailwind | `N:174700`      |
| 26    | middle stay 2-1 | donor                 | —           | Sailwind | `N:175045`      |
| 29    | middle stay 2-2 | donor                 | —           | Sailwind | `N:177741`      |
| 32    | mizzen stay 1-1 | donor                 | —           | Sailwind | `N:176825`      |
| 34    | mizzen stay 1-2 | donor                 | —           | Sailwind | `N:176291`      |
| 36    | mizzen stay 2-1 | donor                 | —           | Sailwind | `N:176633`      |
| 38    | mizzen stay 2-2 | donor                 | —           | Sailwind | `N:175892`      |
| 40    | mizzen stay 3-1 | donor                 | —           | Sailwind | `N:175854`      |
| 42    | mizzen stay 3-2 | donor                 | —           | Sailwind | `N:176896`      |

Asset keys identify the serialized file containing the **Mast component**:

- `N`: `level24`.

## Stay configurations

These rows describe
[MoreSailwindSails configuration](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/LargeDhowStays.cs),
not objects stored in the original boat assets. **Mount** is the new custom stay
index. **Donor**, **Fore**, **Aft**, **Required**, and **Excluded** refer to the
object table above. Required options must be fitted; exclusions must be absent
or unfitted. “—” means no exclusions. A donor supplies setup data and does not
itself have to be fitted.

### Foremast / mainmast

| Mount | Configuration                                                                                                                                                                                  | Donor | Fore | Aft | Required | Excluded |
| ----- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---- | --- | -------- | -------- |
| 128   | [foremast 1 / main mast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/LargeDhowStays.cs#L24)     | 20    | 0    | 2   | 0, 2     | 3        |
| 129   | [foremast 1 / main topmast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/LargeDhowStays.cs#L42)  | 20    | 0    | 3   | 0, 2, 3  | —        |
| 130   | [foremast 2 / main mast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/LargeDhowStays.cs#L60)     | 23    | 1    | 2   | 1, 2     | 3        |
| 131   | [foremast 2 / main topmast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/LargeDhowStays.cs#L78)  | 23    | 1    | 3   | 1, 2, 3  | —        |
| 132   | [foremast 1 / main mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/LargeDhowStays.cs#L96)     | 26    | 0    | 4   | 0, 4     | 5        |
| 133   | [foremast 1 / main topmast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/LargeDhowStays.cs#L114) | 26    | 0    | 5   | 0, 4, 5  | —        |
| 134   | [foremast 2 / main mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/LargeDhowStays.cs#L132)    | 29    | 1    | 4   | 1, 4     | 5        |
| 135   | [foremast 2 / main topmast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/LargeDhowStays.cs#L150) | 29    | 1    | 5   | 1, 4, 5  | —        |

### Mainmast / mizzenmast

| Mount | Configuration                                                                                                                                                                                  | Donor | Fore | Aft | Required | Excluded |
| ----- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---- | --- | -------- | -------- |
| 136   | [main mast 1 / mizzen mast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/LargeDhowStays.cs#L174) | 32    | 2    | 6   | 2, 6     | —        |
| 137   | [main mast 2 / mizzen mast 1](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/LargeDhowStays.cs#L192) | 34    | 4    | 6   | 4, 6     | —        |
| 138   | [main mast 1 / mizzen mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/LargeDhowStays.cs#L210) | 36    | 2    | 7   | 2, 7     | —        |
| 139   | [main mast 2 / mizzen mast 2](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/LargeDhowStays.cs#L228) | 38    | 4    | 7   | 4, 7     | —        |
| 140   | [main mast 1 / mizzen mast 3](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/LargeDhowStays.cs#L246) | 40    | 2    | 8   | 2, 8     | —        |
| 141   | [main mast 2 / mizzen mast 3](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/LargeDhowStays.cs#L264) | 42    | 4    | 8   | 4, 8     | —        |

Attachment vectors, collar dimensions, and guide selection are linked from each
configuration. See [Fisherman's Stay](Fishermans-Stay) for their coordinate
frames and
[saved selections](Fishermans-Stay#shipyard-registration-and-saved-selections)
before changing mount numbers or option order.

[All boats](Boats)
