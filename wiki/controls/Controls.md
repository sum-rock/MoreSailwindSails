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
| Overlay, model previews, surface capture, and forced fallbacks       | [Winch placement diagnostics](Winch-placement-diagnostics)                                  |

[Fisherman's Stays](Fishermans-Stay) and
[Fisherman's Flying Sails](Fishermans-Flying-Sails) own cloned sheet and halyard
controls through the same boat-level allocator. It retains valid placements,
normally tries compatible native positions first when another placement is
needed, then tries authored fallbacks. Both families share position
reservations.

| Consumer                   | Native candidates                                           | Halyard fallback mast  |
| -------------------------- | ----------------------------------------------------------- | ---------------------- |
| Sail on a Fisherman's Stay | Unoccupied native stays matching both supporting base masts | Aft physical base mast |
| Fisherman's Flying Sail    | Controls on its fitted physical mast                        | Fitted physical mast   |

The [source-selection rules](How-winches-are-placed) determine which controls
can supply a position. Authored sheet pairs also depend on the sail's
[neutral fitted span](How-winches-are-placed#fitted-span-and-fallback-eligibility).
The
[forced-fallback setting](Winch-placement-diagnostics#forcing-sheet-fallbacks)
skips native sheet positions for both families.

## Consumer contract and ownership

`ISailControlRig` is an internal adapter contract, not a public plugin API.
`TryGetControlRequest` supplies a `SailControlRequest` containing the owner,
boat, fitted supports, native source policy, fitted sheet span, and existing
rope controllers. `OnControlsChanged` receives a `SailControlSet` containing the
owned left, right, and reef winches when controls are published or cleared.

`WinchControls.Register` associates one consumer with its owner on the boat. The
manager refreshes requests, releases unused claims, then retains or finds
placements. An unavailable request or inactive support suspends placement.
`Unregister` releases reservations, retires the owner's clones, and publishes an
empty control set.

The stay adapter publishes clones into its dedicated `Mast` arrays. Flying Sails
bind their own rope controllers to owned clones while preserving the physical
mast's native arrays. After native attachment, Flying Sail integration releases
only bindings to participating Flying Sail controllers before restoring their
routes and limits. Disabling or destroying a Flying Sail releases its controls;
destroying a stay unregisters its adapter.

Use the [boat reference](Boats) to identify mast and stay indexes. Use the
[sail reference](Sails) for sail prefab IDs and fitting rules; those identities
are separate from control donor array indexes.

## Source

The code at revision `fcdf300` (plugin metadata **0.3.0**) is the source of
truth:

- [Manager and consumer interface](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/WinchControls.cs)
- [Requests, published controls, and placement lifecycle](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Runtime/WinchGroup.cs)
- [Native source policies](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Discovery/NativeWinchInventory.cs)
- [Stay adapter](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Stays/FishermansStay/FishermansStayControls.cs)
- [Flying Sail ownership and binding handoff](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Sails/FishermansFlyingSail/FishermansFlyingSailRigging.cs)
- [Native attachment integration](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Sails/FishermansFlyingSail/Patches/FlyingSailSailwindPatches.cs)
