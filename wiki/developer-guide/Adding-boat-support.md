# Adding boat support

This guide is for boat authors contributing support to MoreSailwindSails and
maintainers adding another boat later. It follows plugin **0.4.1** at revision
`2254dfe`, with the stay fitting guidance in section 3 updated to `3639f89`. The
linked source is the authority; check the current implementation when applying
the examples to a later version.

Boat support is authored in this repository. There is currently no public
registration API for another plugin. Adding a boat to the catalogs lets the
existing runtime create its Fisherman's Stays, fit the sails, allocate controls,
and protect supporting parts. Merely loading a boat plugin after
MoreSailwindSails does not register support.

## Files to add and catalogs to update

Each feature definition below refers to the shared `BoatDefinition` instance.
The controls configuration is optional.

| Add a file under `src/`                                   | Add its `Definition` to                   | Purpose                                                        |
| --------------------------------------------------------- | ----------------------------------------- | -------------------------------------------------------------- |
| `BoatConfiguration/Boats/MyBoat.cs`                       | [BoatCatalog.All][boat-catalog]           | Boat identity and physical mast ancestry                       |
| `Stays/FishermansStay/Configuration/Boats/MyBoatStays.cs` | [FishermansStayCatalog.All][stay-catalog] | Stay mounts, attachment measurements, and fitting requirements |
| `Controls/Configuration/Boats/MyBoatControls.cs`          | [BoatControlCatalog.All][control-catalog] | Additional sheet and halyard positions (optional)              |

The first two enable Fisherman's Stay construction. Additional sheet and halyard
positions are optional fallbacks for when usable native positions are occupied.
A boat can use its existing control positions without a `BoatControlCatalog`
entry. Spritsails use native fitting restrictions; their ability to span a lower
mast and its topmast comes from the shared ancestry map, without a separate
spritsail boat catalog.

## 1. Inventory the boat's installed rigging

Inspect the boat as supplied by the installed game and boat mod. Record the game
and mod versions alongside measurements. Upstream source can differ from the
installed assemblies and assets.

Collect:

- The exact name of the GameObject holding `BoatRefs`. Catalog matching is
  case-sensitive and strips trailing `(Clone)` suffixes; it does not use the
  boat's display name or plugin GUID.
- Every relevant physical mast section's `Mast.orderIndex`, its supporting lower
  section, `BoatPartOption`, transform, and `CapsuleCollider` dimensions and
  axis.
- Existing stay indexes, their `BoatPartOption.requires` dependencies, and their
  paired sheet winches. These are possible donors and native control sources.
- Physical mast reef guides (`mastReefAtt` and `mastReefAttExtension`) and winch
  arrays, including each donor's array index.
- The boat's `boatModel` and `walkCol` references, available part groups, and
  any occupied custom mast slots.

These indexes are `Mast.orderIndex` values, not menu positions or Unity asset
object IDs. Stay reference resolution scans `BoatCustomParts.availableParts`, so
relevant rigging must be registered there, not merely present in the hierarchy.
See [FishermansStayReferences][references].

The [boat inspection command](MSS-command#boat-inspection) can inventory its
listed boats. It does **not** discover an arbitrary new boat by name. For a new
boat, inspect its assets and registration code directly, or extend the
inspection tool's profiles separately. The fallback halyard generator has the
same limited boat coverage.

## 2. Define boat identity and mast ancestry

Follow [Brig's shared definition][brig]. For example, this hypothetical boat has
base masts 3 and 5 and topmasts 55 and 56:

```csharp
internal static readonly BoatDefinition Definition = new BoatDefinition(
    boatName: "BOAT my custom boat",
    mastParents: new Dictionary<int, int>
    {
        { 3, -1 },
        { 5, -1 },
        { 55, 3 },
        { 56, 5 },
    }
);
```

Put this in `MyBoat` in the `MoreSailwindSails.BoatConfiguration.Boats`
namespace, with `using System.Collections.Generic;`, then append
`MyBoat.Definition` to `BoatCatalog.All`.

`-1` means a physical base mast. Other values identify the immediately
supporting section. Include alternative fitted mast configurations and their
upper sections; do not put stay mounts in this map. Physical ancestry can differ
from Unity transform parenting. Every referenced parent must exist in the map,
and cycles are rejected by [BoatDefinition][boat-definition].

This map also lets control discovery resolve topmasts to their bases, permits
appropriate staysail/square-sail coexistence, and enables [spritsail topmast
height discovery][spritsail-support]. Incorrect ancestry can therefore affect
more than stay availability.

For spritsails, include the supported topmast chains and alternatives. Their
height resolver retains native single-mast height when no boat definition is
found. No new sail prefab or sail ID is needed for these boat additions.

## 3. Author Fisherman's Stay groups and variants

Use [CogStays][cog-stays] as a compact complete file example. Construct a
`FishermansStayDefinition` with
`boat: BoatConfiguration.Boats.MyBoat.Definition` and an ordered list of
`FishermansStayGroupDefinition` objects. Each group is one shipyard choice, such
as a foremast/mainmast pair, with alternative fitted rigs represented by its
variants. Append the definition to `FishermansStayCatalog.All`.

The following is an existing Brig variant, **not measurements to copy onto a new
boat**:

```csharp
new FishermansStayVariantDefinition(
    mountIndex: 128,
    donor: 15,
    label: "foremast 1 / main mast 1",
    fore: 3,
    forePoint: new Vector3(0.00000f, -0.00000f, -2.39540f),
    foreCollarRadius: 0.29678f,
    aft: 5,
    aftPoint: new Vector3(0.00000f, -0.00000f, 0.93843f),
    aftCollarRadius: 0.29678f,
    extendedGuide: false,
    guideIndex: 0,
    required: new[] { 3, 5 },
    forbidden: new int[0]
)
```

Choose fields using [the variant definition][variant] and [reference
resolution][references]:

- `mountIndex` must be a free slot from **128 through 255** on this boat.
  Existing boats conventionally allocate consecutive slots from 128; another mod
  may already occupy them. Donor, required, and forbidden indexes must be below
  128 under the current constructor rules.
- `donor` identifies an existing `Mast` with `onlyStaysails = true`. It supplies
  rope material, part pricing/mass, initial paired sheet templates, and
  attachment references. It is not the new mount, and its installation
  requirements are not copied. Its `MeshRenderer` and `BoatPartOption` must be
  usable.
- `fore` and `aft` are distinct physical mast sections with
  `onlyStaysails = false` and `CapsuleCollider`s. The luff follows the forward
  section and can extend down its usable connected parent sections; this is not
  a general deck-to-mast staysail mount.
- `forePoint` and `aftPoint` are local to their respective mast transforms.
  Convert a boat-local measurement through `boat.transform.TransformPoint(...)`,
  then through the target mast's `InverseTransformPoint(...)`. Do not treat
  boat-local coordinates as mast-local.
- Measure collar radii on the visible spars. Optional collar offsets move the
  collars without moving the sail endpoints. Endpoints must meet the physical
  spars; existing authoring targets a 70-degree angle to the aft spar's downward
  axis, using the forward masthead when the forward spar is too short.
- `guideIndex` selects the aft section's `mastReefAtt`, or
  `mastReefAttExtension` when `extendedGuide` is true. The guide must exist and
  be fitted. The aft mast must also supply a usable reef winch template.
- `required` includes both endpoint sections **and their supporting lower
  sections**. `forbidden` excludes genuinely conflicting mast or stay options. A
  fitted continuation above either endpoint does not by itself require an
  exclusion; lower stays can remain available with topmasts fitted.

For special geometry, `alignGuideHeightToAftAnchor` moves the effective halyard
guide to the aft anchor's axial height while retaining its radial offset. Follow
the [Caelanor definition][caelanor-stays] and its dedicated checks when this is
needed; it is not a substitute for measuring the anchors.

Missing required parts or guides leave unavailable options in place. Shared
`mountIndex` variants within a group represent different configurations of one
saved fitting; they must have the same label and donor and distinct endpoint
pairs. See [Fisherman's Stay](Fishermans-Stay) for configuration switching and
geometry details.

## 4. Optional: add sheet and halyard positions

Additional sheet and halyard positions are optional. The stay still needs the
usable control templates described above, but existing native positions can
provide its controls without authored fallbacks.

Native stays used as sheet sources need dependency chains reaching **both
physical base masts**. The allocator follows `BoatPartOption.requires`; it does
not infer the pair from names or proximity. Left and right controls must occupy
matching array indexes and have usable renderers and colliders. The construction
donor supplies initial templates, but does not fix the eventual placement.

If additional positions are needed for crowded sail plans, start from
[CogControls][cog-controls] or [ShroudControls][shroud-controls] for a boat
using rope coils, and add the new definition to `BoatControlCatalog.All`. Sheet
and halyard fallbacks can be supplied independently; neither is required for
boat registration. Registering an empty profile does not create additional
control positions.

To author fallback positions:

- Each `SheetFallbackDefinition` names a pair. Its `SheetFallbackPair` selects a
  donor by physical `templateMast`, `templateRole`, and `templateIndex`; the
  role identifies the donor array even if a reef-control model will serve as a
  sheet. Provide measured port/starboard contacts and normals, donor
  `sourceNormal`, mounting `offset`, and any destination supporting mast indexes
  in each seat's `supports` list.
- Contacts and mounting normals use the `BoatRefs` frame. Sheet fallbacks must
  sit far enough aft for the fitted sail span: both contact Z values must be at
  most `forwardBaseMastZ - fittedSpan`. Multiple pairs at different positions
  can accommodate different sizes and simultaneous sails. Positions are reserved
  across consumers; one pair is not unlimited capacity.
- Each `HalyardFallbackDefinition` identifies the requested physical base mast
  and a `HalyardFallbackSeat`. Its `templateIndex` selects that mast's
  `reefWinch` array. Stays request the aft base mast. Cover the relevant
  alternative mast configurations.

Use the [measurement tools](Winch-placement-diagnostics) to capture surface
contacts and normals. Record donor objects, versions, coordinate frames, and
measurement provenance in source comments. The
[fallback template reference](Fallback-winch-templates) explains mounting
offsets and donor selection; [How winches are placed](How-winches-are-placed)
explains eligibility and reservations. Existing definitions for another hull are
examples, not transferable mounting coordinates.

## 5. Preserve registration timing and saved identities

[Stay registration][stay-patches] runs after `SaveableBoatCustomization.Awake`
and the named Shipyard Expansion Harmony patches. The new boat's parts must
already exist when [FishermansStayRegistry.Register][registry] runs, before
saved sails need their mounts. If the boat mod creates parts later, integrate
with its actual creation hook and arrange the required ordering. Plugin startup
order alone does not establish per-boat initialization order. The registry
guards against repeated registration, so a late retry is not a general repair
for incomplete parts on the first call.

The registry creates mounts, controls, visuals, walking frames, and shipyard
groups. It owns their refresh and destruction. A PR normally adds authored
definitions rather than manually appending an existing third-party `Mast` to the
registry: its entries are complete internal `FishermansStay` objects.

Keep mount IDs, appended part-group positions, and option positions stable after
release. Each group reserves option zero for None. Append new choices instead of
inserting them before saved choices, and retain unavailable placeholders.
Coordinate ordering with the boat mod's own part groups. Do not renumber
existing sail prefab IDs or repurpose another mod's occupied mast slots.

## 6. Include measurements and checks in the PR

Add measured fixture data and coverage with the configuration:

- [StayMeasurements.txt][measurements] and [StayChecks][stay-checks] cover spar
  endpoints, ancestry prerequisites, geometry, and stable mount slots. The
  fixture stores boat name, mast index, ancestor indexes, a mast-to-boat matrix,
  visible bottom/top points, and guide position. Add rows for the new endpoint
  sections.
- [ProfileChecks][profile-checks] covers catalog consistency and ancestry.
  Review coverage for the new mast ancestry and optional parts.
- [NativeSeatProfileChecks][seat-checks] and
  [HalyardProfileChecks][halyard-checks] cover authored controls and their
  measured fixtures. When adding optional fallback positions, extend the
  applicable fixture mappings for the new boat and donors.

Some checks use explicit per-catalog counts, ordered boat mappings, aggregate
totals, and consecutive mount expectations. Update those expectations to account
for the new measured configurations; do not remove assertions merely to make an
addition pass. If occupied slots require nonconsecutive IDs, add an explicit
expected mapping that still protects saved identities.

[boat-catalog]:
  https://github.com/sum-rock/MoreSailwindSails/blob/2254dfecb6194e7bddc14cf83dfafbee35b67033/src/BoatConfiguration/BoatCatalog.cs
[stay-catalog]:
  https://github.com/sum-rock/MoreSailwindSails/blob/2254dfecb6194e7bddc14cf83dfafbee35b67033/src/Stays/FishermansStay/Configuration/FishermansStayCatalog.cs
[control-catalog]:
  https://github.com/sum-rock/MoreSailwindSails/blob/2254dfecb6194e7bddc14cf83dfafbee35b67033/src/Controls/Configuration/BoatControlCatalog.cs
[references]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/src/Stays/FishermansStay/FishermansStayReferences.cs
[brig]:
  https://github.com/sum-rock/MoreSailwindSails/blob/2254dfecb6194e7bddc14cf83dfafbee35b67033/src/BoatConfiguration/Boats/Brig.cs
[boat-definition]:
  https://github.com/sum-rock/MoreSailwindSails/blob/2254dfecb6194e7bddc14cf83dfafbee35b67033/src/BoatConfiguration/BoatDefinition.cs
[spritsail-support]:
  https://github.com/sum-rock/MoreSailwindSails/blob/2254dfecb6194e7bddc14cf83dfafbee35b67033/src/Sails/LooseFootedSpritsail/LooseFootedSpritsailMastSupport.cs
[cog-stays]:
  https://github.com/sum-rock/MoreSailwindSails/blob/2254dfecb6194e7bddc14cf83dfafbee35b67033/src/Stays/FishermansStay/Configuration/Boats/CogStays.cs
[variant]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/src/Stays/FishermansStay/Configuration/FishermansStayVariantDefinition.cs
[caelanor-stays]:
  https://github.com/sum-rock/MoreSailwindSails/blob/2254dfecb6194e7bddc14cf83dfafbee35b67033/src/Stays/FishermansStay/Configuration/Boats/CaelanorStays.cs
[cog-controls]:
  https://github.com/sum-rock/MoreSailwindSails/blob/2254dfecb6194e7bddc14cf83dfafbee35b67033/src/Controls/Configuration/Boats/CogControls.cs
[shroud-controls]:
  https://github.com/sum-rock/MoreSailwindSails/blob/2254dfecb6194e7bddc14cf83dfafbee35b67033/src/Controls/Configuration/Boats/ShroudControls.cs
[stay-patches]:
  https://github.com/sum-rock/MoreSailwindSails/blob/2254dfecb6194e7bddc14cf83dfafbee35b67033/src/Stays/FishermansStay/Patches/StaySailwindPatches.cs
[registry]:
  https://github.com/sum-rock/MoreSailwindSails/blob/2254dfecb6194e7bddc14cf83dfafbee35b67033/src/Stays/FishermansStay/FishermansStayRegistry.cs
[measurements]:
  https://github.com/sum-rock/MoreSailwindSails/blob/2254dfecb6194e7bddc14cf83dfafbee35b67033/tests/GeometryChecks/FishermansStay/StayMeasurements.txt
[stay-checks]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/tests/GeometryChecks/FishermansStay/StayChecks.cs
[profile-checks]:
  https://github.com/sum-rock/MoreSailwindSails/blob/3639f8900d57defaa76186eb41c46282c5950fff/tests/GeometryChecks/FishermansStay/ProfileChecks.cs
[seat-checks]:
  https://github.com/sum-rock/MoreSailwindSails/blob/2254dfecb6194e7bddc14cf83dfafbee35b67033/tests/GeometryChecks/Controls/NativeSeatProfileChecks.cs
[halyard-checks]:
  https://github.com/sum-rock/MoreSailwindSails/blob/2254dfecb6194e7bddc14cf83dfafbee35b67033/tests/GeometryChecks/Controls/HalyardProfileChecks.cs
