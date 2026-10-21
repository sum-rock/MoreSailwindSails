# Controls reference

Controls connect a sail's ropes to usable fittings on its boat. A control donor
supplies the object and appearance to clone; a mounting position determines
where the clone belongs. The fitting can be a wheel winch or a rope-coil
control.

| Topic                                                                | Reference                                                                                   |
| -------------------------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| Native sheet pairs, halyards, fallback ordering, and reservations    | [How winches are placed](How-winches-are-placed)                                            |
| Donor mast/array indexes, selected models, and mounting measurements | [Fallback winch templates](Fallback-winch-templates)                                        |
| Runtime clones and prefab registration                               | [Template identity and ownership](Fallback-winch-templates#template-identity-and-ownership) |
| Overlay, surface capture, and halyard generation                     | [Inspecting placement](How-winches-are-placed#inspecting-placement)                         |

[Fisherman's Stays](Fishermans-Stay) own cloned sheet and halyard controls.
Their allocator retains valid placements, tries compatible native positions
first when another placement is needed, then tries authored fallbacks.
[Fisherman's Flying Sails](Fishermans-Flying-Sails) use the native controls on
their physical mast through the game's normal assignment rules.

Use the [boat reference](Boats) to identify mast and stay indexes. Use the
[sail reference](Sails) for sail prefab IDs and fitting rules; those identities
are separate from control donor array indexes.
