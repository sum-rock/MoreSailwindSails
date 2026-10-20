# Fisherman's Flying Sails

The Fisherman's Flying Sail is a four-corner trapezoid hoisted from the deck on
a physical mast, with an aft mast supplying support. It appears in the
shipyard's **Other** category and uses the physical mast's native sail slot.

## Prefab ID and fitting

| Identity             | Value                   |
| -------------------- | ----------------------- |
| Prefab sail ID       | **400**                 |
| Native donor sail ID | **110**                 |
| Display name         | Fisherman's Flying Sail |
| Category             | `SailCategory.other`    |

The [builder][prefab] registers the sail at `PrefabsDirectory.sails[400]` and
sets `Sail.prefabIndex` to 400. This is a saved sail identity, separate from
boat mast indexes. Registration rejects an occupied destination and runs after
optional Shipyard Expansion setup and before All Sails in All Shipyards caches
its inventory.

The forward mast must be a supported, fitted physical mast. The boat's authored
support pairs identify eligible aft masts; the [rigging resolver][rigging] finds
active guides on their connected sections and selects the nearest valid pair by
horizontal guide separation. Sail height, span, guide clearance, and shipyard
collision checks determine whether the fitting succeeds.

## Shape and movement

At the default size, width `W` is **one third of donor 110's install height**.
The luff is `2 × W`, the head rises 20° aft, and the foot falls 20° aft, giving
an aft edge approximately `2.728 × W` long. New selections start at 100% native
scale; saved sizes are restored normally.

Two fixed **0.4572 m (18-inch)** ties hold the forward corners beyond the mast
surface. The sail pivots around this offset luff. At full deployment the upper
aft corner follows **85%** of the sheeting angle, while coupled foot/leech
fitting constrains the lower corner. The head response fades during lowering.

The [rest mesh][geometry] has a circular-arc belly **20% of W** deep, carried
from head to foot. The luff bows toward the mast, capped at **0.2286 m (9
inches)**; shrinking reduces the bow while enlarging retains the cap. A **24 ×
32** panel grid uses twelve spanwise shaping intervals. Only the four corners
are pinned, allowing the luff and interior Cloth to move within bounded travel.
Scale-dependent constraints preserve clearance from the mast.

Apparent wind drives the smoothed belly and tack changes. These changes move the
skinning bones without swapping the initialized mesh, UVs, or bind poses.
Separate neutral collision strips follow the offset pivot during shipyard
checks; visual billow is not used as the installation envelope.

## Physics and hoisting

The [aerodynamic frame][aerodynamics] uses the four posed corners to calculate
the luff axis, sail chord, normal, and area-weighted center of two triangles.
The rig refreshes Sailwind's wind sensor before native wind-force calculation. A
scoped patch supplies the normal facing with the apparent wind as the force
direction, while native code retains the force-magnitude calculation. The
propulsion frame follows the posed sail rather than individual Cloth ripples.

| Setting                                    | Value in this rig                         |
| ------------------------------------------ | ----------------------------------------- |
| Maximum trim travel                        | ±40°, retaining tighter controller limits |
| Sail Rigidbody mass / angular drag         | `0.1` / `1`                               |
| Cloth bending / stretching stiffness       | `0.15` / `0.99`                           |
| Cloth solver frequency                     | `120`                                     |
| Cloth world velocity / acceleration scales | Both `0`                                  |
| WindCloth damping range                    | `0.08–0.45`                               |

The donor supplies Cloth friction, gravity, and serialized wind response. A
separate nine-point center-plane mesh and collider participate in native wind
shielding.

Lowering brings the sail back to the deck. The rig uses `currentUnroll` to move
its corners through the hoist and chooses the visible representation:

| `currentUnroll`              | Visible representation            |
| ---------------------------- | --------------------------------- |
| `0.02` and below             | Sail hidden; ropes parked         |
| Above `0.02` to below `0.98` | Procedurally posed hoisting panel |
| `0.98` and above             | Deployed Cloth                    |

Flying Sail controls bind through the physical mast's native control arrays and
sail ordering. The custom stay allocator described in
[staysail control discovery](How-winches-are-placed#how-staysail-sheets-and-halyards-are-found)
belongs to sails on Fisherman's Stays. The Flying Sail restores its own rope
routes and travel limits after native attachment updates.

## Assets and ownership

The editable mesh is [`assets/flying_sail/fishermans_flying_sail.blend`][asset].
Its nominal flat cut is 4.6 units wide with a 9.2-unit luff. Blender X points
aft, Z points up, and the panel lies at Y = 0. The baker validates the outline,
grid, transforms, and planar UVs, then writes
`FishermansFlyingSailBakedMesh.g.cs`. Runtime geometry scales those arrays and
adds the belly and luff bow.

From the repository root, bake an edited asset and check the generated data
with:

```sh
./mss assets bake flying_sail
./mss assets bake flying_sail --check
```

Commit the Blender asset and generated C# together. The plugin uses the baked
arrays without loading Blender files or a separate sail asset bundle.

Construction happens beneath an inactive prefab container, replacing the donor's
Cloth component with fresh Cloth for the new topology. The container owns the
cloth, wind-shadow, and extracted corner-knot meshes. Installed sails share
these read-only meshes, with their own bones and Cloth state; removing one sail
must not destroy the shared meshes. The container releases them when it is
destroyed.

Native recoloring creates a private material shared by the deployed and hoisting
renderers and the hidden color reference. New sails use palette **11** and a
plain white texture. Optional Shipyard Expansion texture handling keeps the same
blank-white appearance while preserving recoloring.

Custom sheet routes, shared spans, and fixed ties follow the game's
`Settings.clothRopes` option. [Routed ropes][ropes] use six-sided tubes with a
borrowed native material when enabled, otherwise flat lines. Each rope owns and
releases its reusable tube mesh. Corner knots are extracted once from a private
baked copy of the native knot geometry; the original donor mesh and materials
are retained.

## Implementation reference

- [Shared rig: Cloth setup, bone motion, hoisting, and mesh ownership][rig]
- [Mast frame, fixed ties, and upper-corner response][frame]
- [Native integration: force direction, controls, fitting, and
  registration][patches]
- [Plain cloth and recoloring][appearance]
- [Corner-knot extraction][knots] and [authoring dimensions][authoring]
- [Fisherman's Staysails](Fishermans-Staysails) for the stay-mounted family

[prefab]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansFlyingSail/FishermansFlyingSailPrefab.cs
[rigging]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansFlyingSail/FishermansFlyingSailRigging.cs
[geometry]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansFlyingSail/FishermansFlyingSailGeometry.cs
[aerodynamics]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansFlyingSail/FishermansFlyingSailAerodynamics.cs
[asset]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/assets/flying_sail/fishermans_flying_sail.blend
[ropes]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Visuals/RoutedRope.cs
[rig]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansFlyingSail/FishermansFlyingSailRig.cs
[frame]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansFlyingSail/FishermansFlyingSailFrameGeometry.cs
[patches]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansFlyingSail/Patches/FlyingSailSailwindPatches.cs
[appearance]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansFlyingSail/FishermansFlyingSailAppearance.cs
[knots]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Sails/FishermansFlyingSail/FishermansFlyingSailKnots.cs
[authoring]:
  https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/tools/assets/flying_sail_authoring.py
