# Winch placement diagnostics

Part of the [Controls reference](Controls). Use these tools to inspect native
mounting positions, preview authored fallbacks, and capture measurements. The
[placement guide](How-winches-are-placed) explains allocation;
[Fallback winch templates](Fallback-winch-templates) documents donor selection
and mounting measurements. The linked code is the source of truth.

Settings belong to `[Diagnostics]` in
`BepInEx/config/com.august.moresailwindsails.cfg`.

| Setting                   | Default          | Purpose                                            |
| ------------------------- | ---------------- | -------------------------------------------------- |
| `EnableWinchMountOverlay` | `false`          | Enables the overlay tool.                          |
| `ToggleWinchMountOverlay` | `F8`             | Selects a boat and shows or hides the overlay.     |
| `ToggleWinchModelPreview` | `F8 + LeftShift` | Toggles model previews while the overlay is open.  |
| `CaptureWinchPosition`    | `F9`             | Logs a boat surface measurement.                   |
| `ForceSheetFallbacks`     | `false`          | Skips native sheet positions after a game restart. |

Set a shortcut to `None` to disable that shortcut. Surface selection and capture
require normal play with menus closed.

## Viewing winch mounting points

With MoreSailwindSails installed, set this entry in
`BepInEx/config/com.august.moresailwindsails.cfg`:

```ini
[Diagnostics]
EnableWinchMountOverlay = true
```

Close menus, aim at a boat surface within **10 metres**, and press **F8**. Press
F8 again to hide the overlay. `ToggleWinchMountOverlay` changes the shortcut;
setting it to `None` disables that shortcut.

| Color  | Meaning                                |
| ------ | -------------------------------------- |
| Green  | Unused position on fitted supports     |
| Red    | Occupied, including hidden bound ropes |
| Cyan   | Unused position on unfitted supports   |
| Purple | Defined fallback placement             |

Green describes occupancy and support. A sail still needs a compatible template
and eligible native source or fallback. Sheets need a complete pair of
positions, and authored pairs must satisfy the fitted-span boundary.

Purple wireframe boxes mark every defined sheet and halyard fallback position on
the selected boat, including both sides of sheet pairs. Each box is 0.3 metres
across in boat-local coordinates, centered on the control position including its
placement offset, and moves with the boat. Boxes appear regardless of occupancy,
fitted supports, or template availability. The overlay lists their count
separately from winch locations. A purple box identifies a definition; it does
not indicate whether that position can currently be allocated.

## Previewing winch models

With the overlay open, press **Left Shift + F8** to toggle models. The
`ToggleWinchModelPreview` shortcut takes precedence over the overlay toggle.
Previews start off whenever a boat is selected; closing the overlay clears them.

Hidden native controls are drawn at their native transforms. Authored fallbacks
use the selected donor's native scale, mounting rotation, contact normal, and
offset. Child meshes and materials follow the donor, including baked skinned
geometry. Preview drawing does not create interactive winches, bind ropes, or
reserve positions.

Visible native models and coincident locations suppress duplicate previews;
authored previews take precedence over unused native aliases at the same
position. Purple fallback boxes remain visible with models on or off. The legend
reports how many fallback models are unavailable because usable donor geometry
could not be obtained. A missing model does not remove the authored marker, and
a visible preview does not establish placement eligibility.

## Forcing sheet fallbacks

To inspect placement using only authored sheet pairs, set:

```ini
[Diagnostics]
ForceSheetFallbacks = true
```

Restart the game after changing this setting. The plugin captures it once at
startup; reloading or editing the live configuration does not change the
allocator's snapshot. Set it to `false` and restart to restore native sheet
candidate selection.

This affects sheets for **both Fisherman's Stays and Fisherman's Flying Sails**.
At the cited revision, the configuration description mentions only Fisherman's
Stays, but both consumers use the same configured sheet resolver. Native sheet
placements are neither selected nor retained in forced mode. Halyard selection
is unchanged.

Donor availability, fitted supports, the
[fitted-span boundary](How-winches-are-placed#fitted-span-and-fallback-eligibility),
occupancy, and reservations still apply. If no eligible pair is free, sheets
remain hidden and placement retries. Forcing fallbacks does not create extra
positions. It works independently of the overlay and model-preview settings.

## Coordinate frames

| Value                              | Frame                                    |
| ---------------------------------- | ---------------------------------------- |
| Stay `ForePoint` / `AftPoint`      | Local to the respective physical mast    |
| Authored fallback control position | Local to the `BoatRefs` root             |
| Native control `localPosition`     | Local to that control's immediate parent |
| Position captured by F9            | Local to the `BoatRefs` root             |

For a normal scene transform, convert a world position with
`boat.transform.InverseTransformPoint(position: worldPoint)`. Convert a
mast-local endpoint to world space with
`mast.transform.TransformPoint(position: localPoint)`. Include parent rotation
and scale; copying a native control's `localPosition` into a boat-local field
can place it on the wrong side of the boat.

Sailwind also uses a separate walking-collision model. The capture tool maps
hits from that model through the visible boat model before returning boat-local
coordinates. It transforms surface normals with the inverse transpose so scaled
transforms are handled correctly.

## Capturing a proposed position

Aim at a solid boat surface within 10 metres during normal play and press
**F9**. `CaptureWinchPosition` changes this shortcut. The overlay does not need
to be enabled.

Entries beginning with `Winch position capture #` in `BepInEx/LogOutput.log`
give the boat, boat-local position and surface normal, hit model, hierarchy
path, collider identity, and distance. Capturing a point does not move or
reserve a control. It supplies a measurement for an authored fallback; that
fallback's rotation and supporting parts are defined separately in the boat's
control configuration.

## Generating halyard fallback positions

The `mss` command can automatically search installed boat geometry for clear
halyard fallback positions near native winch height. For example, from the
repository root:

```sh
./mss generate-fallback-halyards brig --mast 5
```

The command emits candidate `HalyardFallbackDefinition` entries for review and
incorporation into the boat's control configuration. It reads installed assets
without modifying them or automatically editing the configuration. Once the
definitions are incorporated into a built plugin, the overlay shows their
positions as purple boxes. See
[Fallback halyard generation](MSS-command#fallback-halyard-generation) for
supported boats, search options, output files, and handling unresolved masts.

## Source

This page describes revision `fcdf300`, whose plugin metadata version is
**0.3.0**.

- [Settings and startup snapshot](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Plugin.cs)
- [Sheet selection and retention](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/Placement/SheetingWinchPlacementResolver.cs)
- [Shared allocator configuration](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Controls/WinchControls.cs)
- [Overlay colors, model selection, and lifecycle](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Utils/WinchMountOverlay.cs)
- [Preview model collection](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Utils/WinchModelPreview.cs)
- [Mesh drawing and ownership](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Utils/WinchPreviewMesh.cs)
- [Preview transforms and duplicate positions](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Utils/WinchMountOverlayGeometry.cs)
- [Surface picking and frame conversion](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Utils/BoatSurfacePicker.cs)
- [Position logging](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Utils/LogFallbackWinchPlacement.cs)
- [Halyard generator options](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/tools/halyards/cli.py)
- [Halyard geometry search](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/tools/halyards/generate.py)
