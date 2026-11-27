# Fisherman's Staysails

Mk.A, Mk.B, and Mk.C share one rig and differ in their authored sail cut. They
fit the custom Fisherman's Stay mounts in the **Staysails** category. The stay
provides the attachment points and owns the sheet and halyard controls.

This reference follows revision `3639f89` (plugin metadata **0.4.1**). The
linked code is the source of truth.

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

The shared [mesh fitter][fitting] scales each baked cut and adjusts its head to
the stay slope. The source uses a 20° head; fitted slopes range from 0–80°, with
positive luff and leech lengths required. The luff follows the forward mast's
axis, including on raked masts.

[Authored animation][animation] supplies the sail's sheeting and reefing shapes.
The rig interpolates baked sheet poses and reef sequences for tight and eased
sheets, then adapts the surface to the fitted width and head slope. Sheet ease
comes from the current hinge angle relative to its permitted travel. Apparent
wind selects the tack with a deadband and a smoothed transition between sides.

The runtime updates the visible mesh directly. Unity Cloth and WindCloth remain
disabled; the replacement Cloth component exists for native references. Visual
flutter is layered over the authored surface without moving the corner rope
attachments or wind center. A separate sample mesh and collider support native
wind shielding.

### Placement and coexistence

[Fitting][installation] reserves 15 cm below the stay head and ahead of the aft
mast. A luff attached at a topmast can extend down the connected, fitted lower
mast sections recorded in the boat's ancestry map. The support calculation stops
at missing or unusable parents and does not count overlapping sections twice.
The sail must still fit the supported mast length and the span between masts.

Square sails can coexist on the forward physical mast, including its other
sections. For gaffs and spritsails on that same physical mast, [luff
compatibility][luff] permits crossing spars or cloth only when the two luffs
neither touch nor overlap. Other collisions retain their normal checks. The
[stay configuration](Fishermans-Stay#fitting-conditions) determines which mast
pairs are available; a variant label alone does not identify its actual
attachment sections.

## Physics and reefing

Each cut exports an authored `WindCenter` position and orientation. Fitting
scales and adjusts the marker position to the stay slope while retaining its
authored axes. The sensor sits beneath the sail's resize hierarchy, so resizing
and live hinge rotation carry it with the sail. Its local pose stays fixed
through sheeting animation, reefing, tacking, and flutter; it is not recomputed
from animated corners. The [force-direction patch][patches] faces the sensor's
normal toward apparent wind while native Sailwind calculates force magnitude.

Reefing gathers the authored panel upward toward its head. Every positive
`currentUnroll` uses the animated panel, including partial reefing. At zero
deployment, the [native furled bundle][reefing] is drawn along the physical stay
over the fitted sail span. The donor's reef animation is not sampled or played
at runtime.

The
[stay's allocated halyard](How-winches-are-placed#how-staysail-sheets-and-halyards-are-found)
drives reefing. Native rope rendering is retained, with attachments following
the authored corners on separate leaf transforms. When fully furled, the sheet
and halyard attachments meet the aft end of the bundle.

The stay's [control adapter](Controls#consumer-contract-and-ownership) submits
the fitted sail's existing rope controllers to the shared allocator. The
[rig][rig] supplies the neutral fitted width projected along boat-local Z so
[sheet fallbacks](How-winches-are-placed#fitted-span-and-fallback-eligibility)
remain aft of the fitted span. Resizing can invalidate a placement; live trim,
reefing, and billow do not change the span. While fitted geometry is
unavailable, authored sheet fallbacks are deferred.

## Assets and ownership

Editable cuts live in `assets/staysails/fishermans_staysail_mka.blend`,
`fishermans_staysail_mkb.blend`, and `fishermans_staysail_mkc.blend`. The
[authoring definitions][authoring] use a nominal width of 13.8 units and the cut
dimensions above. Blender X points aft and Z points up; the base panel lies at Y
= 0.

The [collector][collector] validates the 24 × 32 quad grid, applied object
transforms, and seam-free `ClothUV` layer. Animation samples must retain the
same topology. It evaluates the authored simulation sequentially to export sheet
poses and both reef sequences into each mark's `*BakedMesh.g.cs` arrays. Runtime
fitting reprojects the flat cut's UVs with a 5% margin.

Each asset must also contain a `WindCenter` Empty with a finite transform and
unit scale, without a parent, animation, or constraints. Its position and
orientation are exported with the mesh and animation data. This marker is the
authoring control for the native wind sensor.

From the repository root, bake one edited cut and check its generated data with:

```sh
./mss assets bake staysail --cut A
./mss assets bake staysail --cut A --check
```

Use `B` or `C` for the other cuts. Commit the edited Blender asset and its
generated C# together. The plugin consumes those arrays; it does not load the
Blender files or require a separate sail asset bundle at runtime.

The [prefab builder][prefab] owns its template meshes beneath an inactive
container. Each fitted instance owns its fitted cloth and shadow mesh copies and
destroys them when removed. Animation updates vertices without replacing
topology or rewriting bind poses.

Native recoloring supplies the current material for the animated panel and
furled bundle. New sails use palette **11** and a plain white texture; optional
Shipyard Expansion texture handling keeps these sails on its blank-white
selection. Borrowed game assets remain intact.

## Implementation reference

- [Shared rig: fitting, wind sensor, animation, and rendering][rig]
- [Tack selection][fixed-head] and [visual flutter][flutter]
- [Native integration: fitting, force direction, scaling, and
  registration][patches]
- [Plain cloth and recoloring][appearance]
- [Fisherman's Flying Sails](Fishermans-Flying-Sails) for the deck-hoisted
  family

[mk-a]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/src/Sails/FishermansStaysail/MkA/FishermansStaysailMkA.cs
[mk-b]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/src/Sails/FishermansStaysail/MkB/FishermansStaysailMkB.cs
[mk-c]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/src/Sails/FishermansStaysail/MkC/FishermansStaysailMkC.cs
[authoring]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/tools/assets/staysail/authoring.py
[collector]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/tools/assets/staysail/collect.py
[fitting]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/src/Sails/FishermansStaysail/FishermansStaysailMeshFitting.cs
[animation]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/src/Sails/FishermansStaysail/FishermansStaysailAnimation.cs
[installation]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/src/Sails/FishermansStaysail/FishermansStaysailInstallationGeometry.cs
[luff]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/src/Sails/FishermansStaysail/FishermansStaysailLuffCompatibility.cs
[reefing]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/src/Sails/FishermansStaysail/FishermansStaysailReefing.cs
[prefab]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/src/Sails/FishermansStaysail/FishermansStaysailPrefab.cs
[rig]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/src/Sails/FishermansStaysail/FishermansStaysailRig.cs
[fixed-head]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/src/Sails/FishermansStaysail/FishermansStaysailFixedHead.cs
[flutter]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/src/Sails/FishermansStaysail/FishermansStaysailFlutter.cs
[appearance]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/src/Sails/FishermansStaysail/FishermansStaysailAppearance.cs
[patches]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/src/Sails/FishermansStaysail/Patches/StaysailSailwindPatches.cs
