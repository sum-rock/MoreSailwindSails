# Fisherman's Staysails

Mk.A, Mk.B, and Mk.C share one rig and differ in their authored sail cut. They
fit the custom Fisherman's Stay mounts in the **Staysails** category. The stay
provides the attachment points and owns the sheet and halyard controls.

## Prefab IDs and fitting

| Sail         | Prefab sail ID | Luff length | Foot direction toward the aft mast |
| ------------ | -------------- | ----------- | ---------------------------------- |
| [Mk.A][mk-a] | **401**        | `W`         | Falls at 20°                       |
| [Mk.B][mk-b] | **402**        | `W`         | Level                              |
| [Mk.C][mk-c] | **403**        | `1.5 × W`   | Rises at 40°                       |

Here `W` is the cut's width, and the foot directions are measured in the
forward-mast frame before shipyard scaling. “Level” means perpendicular to that
mast, so the foot follows a raked mast's frame rather than remaining
world-level.

These are `Sail.prefabIndex` / `PrefabsDirectory.sails` identities saved by
Sailwind. They are separate from the
[stay mount indexes](Fishermans-Stay#mount-identity-and-brig-example) starting
at 128. Keep existing prefab IDs stable when changing the implementation.

All three prefabs are built from donor sail **110**. Registration refuses an
occupied destination slot and runs after optional Shipyard Expansion setup and
before All Sails in All Shipyards caches its inventory. New shipyard selections
start at **50% width and height** through native `Sail.LoadScale`; existing
saved sizes use the native restoration path.

## Shape and movement

The shared [mesh fitter][fitting] scales the baked cut and adjusts its head to
the stay slope. The template uses a 20° head; first binding fits an installed or
preview instance to its actual stay before enabling Cloth. The fitting range is
0–80°, with positive luff and leech lengths required.

The luff follows the forward mast. The upper aft corner is held leeward at **14°
× deployment**, independently of the lower sheets. Apparent wind selects the
tack using a ±0.6 m/s deadband, and side changes are smoothed. The lower aft
corner responds to sheeting; coupled foot/leech fitting keeps it within the
available edge lengths. As the sail reefs, that corner gathers back toward the
head angle.

The rest shape has a rounded spanwise belly with a head peak of **12% of W**,
tapering toward the foot. A **24 × 32** panel grid is skinned across six
spanwise shaping intervals. The whole luff and the two aft corners are pinned to
their skin targets; the remaining Cloth vertices have bounded travel around the
shaped surface. Tacking moves bones while retaining the initialized mesh, UVs,
and bind poses.

Fitting reserves **15 cm** below the stay head and ahead of the aft mast, checks
spar/span limits, and uses separate collision strips for the shipyard. A
targeted collision exemption permits contact with a square sail on the same
forward physical mast, including its topmast sections. Other sail contacts and
solid obstructions retain their checks.

## Physics and reefing

The [aerodynamic frame][aerodynamics] comes from the four posed corners. It sets
the native wind sensor's center to the area-weighted centroid of two triangles
and aligns the sensor with the luff, sail chord, and sail normal. Before native
wind-force calculation, the rig refreshes this frame. A scoped patch supplies
the sail normal facing with the apparent wind as the force direction; Sailwind
continues to calculate the force magnitude. Cloth ripples are visual simulation
around the shaped surface, while propulsion uses this corner-derived frame.

| Setting                                    | Value in this rig                         |
| ------------------------------------------ | ----------------------------------------- |
| Maximum trim travel                        | ±40°, retaining tighter controller limits |
| Sail Rigidbody mass / angular drag         | `0.1` / `1`                               |
| Cloth bending / stretching stiffness       | `0.4` / `0.99`                            |
| Cloth solver frequency                     | `120`                                     |
| Cloth world velocity / acceleration scales | Both `0`                                  |
| WindCloth damping range                    | `0.25–0.8`                                |

Cloth keeps the donor's friction, gravity setting, and serialized wind response.
A separate nine-point center-plane mesh and collider support native wind
shielding. The shadow-sample mesh is separate from the visible cloth mesh.

Reefing gathers the panel **upward toward its head**. The rig samples the native
brig-jib reef animation in an inactive hierarchy stripped of duplicate scripts,
renderers, Cloth, and colliders. Its rope motion provides the lift amount:

| `currentUnroll`        | Visible representation                         |
| ---------------------- | ---------------------------------------------- |
| Below `0.04`           | Native furled bundle fitted along the head     |
| `0.04` to below `0.98` | Skinned panel posed by the sampled reef motion |
| `0.98` and above       | Deployed Cloth                                 |

The
[stay's allocated halyard](How-winches-are-placed#how-staysail-sheets-and-halyards-are-found)
drives reefing. Native rope rendering is retained, with rope attachments on
separate leaf transforms so rope rotation does not rotate the shaping bones.

## Assets and ownership

Editable cuts live in `assets/staysails/fishermans_staysail_mka.blend`,
`fishermans_staysail_mkb.blend`, and `fishermans_staysail_mkc.blend`. The
[authoring definitions][authoring] use a nominal width of 13.8 units, a 20°
head, and the cut dimensions above. Blender X points aft, Z points up, and the
flat panel lies at Y = 0. The baker validates the grid, outline, transforms, and
planar UVs, then writes each mark's `*BakedMesh.g.cs` arrays. Runtime fitting
adds the belly and reprojects UVs with a 5% margin.

From the repository root, bake one edited cut and check its generated data with:

```sh
./mss assets bake staysail --cut A
./mss assets bake staysail --cut A --check
```

Use `B` or `C` for the other cuts. Commit the edited Blender asset and its
generated C# together. The plugin consumes those arrays; it does not load the
Blender files or require a separate sail asset bundle at runtime.

The [prefab builder][prefab] works beneath an inactive container and creates
fresh Cloth for the new topology. That container owns the template cloth and
shadow meshes. Each fitted instance then owns its slope-specific cloth and
shadow mesh copies and destroys them when removed. Live tacking and reefing do
not replace topology or rewrite bind poses.

Native recoloring provides a private material. The cloth, reefed panel, furled
bundle, and hidden color reference use the same current recolored material. New
sails use palette **11** and a plain white texture; optional Shipyard Expansion
texture handling keeps these sails on its blank-white selection. Borrowed donor
animation, bundle mesh, and shared game assets remain intact.

## Implementation reference

- [Shared rig: Cloth setup, fitted mesh ownership, shaping, and rendering][rig]
- [Fixed-head tack selection][fixed-head] and [reef animation sampling][reefing]
- [Native integration: fitting, force direction, scaling, and
  registration][patches]
- [Plain cloth and recoloring][appearance]
- [Fisherman's Flying Sails](Fishermans-Flying-Sails) for the deck-hoisted
  family

[mk-a]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansStaysail/MkA/FishermansStaysailMkA.cs
[mk-b]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansStaysail/MkB/FishermansStaysailMkB.cs
[mk-c]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansStaysail/MkC/FishermansStaysailMkC.cs
[fitting]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansStaysail/FishermansStaysailMeshFitting.cs
[aerodynamics]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansStaysail/FishermansStaysailAerodynamics.cs
[authoring]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/tools/assets/staysail_authoring.py
[prefab]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansStaysail/FishermansStaysailPrefab.cs
[rig]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansStaysail/FishermansStaysailRig.cs
[fixed-head]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansStaysail/FishermansStaysailFixedHead.cs
[reefing]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansStaysail/FishermansStaysailReefing.cs
[patches]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansStaysail/Patches/StaysailSailwindPatches.cs
[appearance]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansStaysail/FishermansStaysailAppearance.cs
