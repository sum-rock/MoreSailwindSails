# Boat rigging reference

Choose a boat to look up the objects used by its Fisherman's Stays. Each page
includes physical mast and donor indexes, supporting sections, part providers,
and the custom stay mappings. Read
[Fisherman's Stay](Fishermans-Stay#supporting-parts-and-controls) first if you
are comparing these numbers with shipyard menu positions.

| Boat                      | Internal boat identity           | Referenced objects | Custom mounts | Configurations |
| ------------------------- | -------------------------------- | ------------------ | ------------- | -------------- |
| [Brig](Boat-Brig)         | `BOAT medi medium (50)`          | 18                 | 24            | 24             |
| [Junk](Boat-Junk)         | `BOAT junk medium (80)`          | 15                 | 9             | 9              |
| [Jong](Boat-Jong)         | `BOAT junk large (70)`           | 16                 | 9             | 9              |
| [Sanbuq](Boat-Sanbuq)     | `BOAT dhow medium (20)`          | 20                 | 26            | 26             |
| [Baghala](Boat-Baghala)   | `BOAT dhow large (30)`           | 19                 | 14            | 14             |
| [Cog](Boat-Cog)           | `BOAT medi small (40)`           | 7                  | 3             | 3              |
| [Kakam](Boat-Kakam)       | `BOAT junk small singleroof(90)` | 5                  | 2             | 2              |
| [Dhow](Boat-Dhow)         | `BOAT dhow small (10)`           | 4                  | 1             | 2              |
| [Shroud](Boat-Shroud)     | `BOAT Shroud Large`              | 8                  | 8             | 8              |
| [Gloriana](Boat-Gloriana) | `BOAT GLORIANA (182)`            | 6                  | 3             | 3              |
| [Chronian](Boat-Chronian) | `BOAT CHRONIAN (187)`            | 8                  | 2             | 2              |
| [Caelanor](Boat-Caelanor) | `BOAT CAELANOR (192)`            | 5                  | 1             | 1              |

The tables cover **131 referenced objects**, **102 custom mounts**, and **103
configurations** across these twelve boats. Counts are per boat; the same mount
number can be reused on a different boat. Dhow has two configurations for one
mount.

Each boat page records the inspected game/mod versions and identifies the
serialized files used for its object IDs. Custom stay mappings come from the
linked MoreSailwindSails source. Missing optional-mod parts can make a
configuration unavailable without changing its saved position.

Winch donors are documented in the [Controls reference](Controls), including
[the fallback donor table](Fallback-winch-templates#sheet-donors). The object
tables on these boat pages identify mast and stay components, not individual
winches. A fallback's `TemplateIndex` is a slot in one of those masts' winch
arrays; it is not another mast index or a prefab ID.

## Table conventions

- **Shipyard label** is the inspected `BoatPartOption.optionName`, preserving
  the boat author's naming and spelling.
- **Parent mast** is the authored supporting physical section, which need not be
  the transform parent.
- **Asset object ID** identifies the `Mast` component using the serialized file
  key given on that page. It is distinct from its `orderIndex`.
- **Required** and **Excluded** contain mast indexes resolved to their part
  options, not shipyard group or option positions.

Configuration labels describe fitted combinations. The Fore and Aft columns
identify the sections actually used for attachment. A topmast in a variant's
label does not imply that both endpoints move onto topmast sections.
