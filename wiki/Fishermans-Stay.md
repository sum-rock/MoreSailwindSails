# Fisherman's Stay

A Fisherman's Stay is a sail mount between two physical mast sections. It
provides the rigging for [Fisherman's Staysails](Fishermans-Staysails), with
attachment points, collars, a halyard guide, and sheet and halyard controls.
Each boat's authored configurations determine the supporting parts and the
shipyard conditions under which the stay is available.

## Supporting parts and controls

The [boat reference](Boats) lists supporting mast sections and donor stays. A
donor is an existing stay with `onlyStaysails = true`. Supporting sections must
have a `Mast` with `onlyStaysails = false` and a `CapsuleCollider`.

The `fore`, `aft`, `donor`, `required`, and `forbidden` fields refer to each
part's `Mast.orderIndex`. These numbers are the mast and stay indexes in the
boat reference, not positions in the shipyard menu or serialized asset object
IDs.

The donor supplies initial paired sheet templates, and the aft mast supplies the
initial halyard template. The final control positions are chosen separately; see
[how sheets and halyards are found](How-winches-are-placed#how-staysail-sheets-and-halyards-are-found).

The
[stay control adapter](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Stays/FishermansStay/FishermansStayControls.cs)
registers the mount with the
[shared allocator](Controls#consumer-contract-and-ownership) and publishes owned
clones into the mount's winch arrays. It supplies the fitted sail's rope
controllers and neutral sheet span. Other sail types fitted to the stay use a
zero span, keeping the forward-base-mast boundary. Removing the sail releases
placement claims; destroying the stay unregisters its controls. This adapter
description follows revision `fcdf300` (plugin metadata **0.3.0**).

For Brig, foremast **3** and main mast **5** support the stay. Donor **15** is
the middle top stay 1-1. Topmasts **55** and **56** have separate variants, so
this configuration excludes them.

## Attachment geometry

`forePoint` is local to the forward mast transform; `aftPoint` is local to the
aft mast transform. That mast's `InverseTransformPoint` converts a measured
world position into the required local coordinates. Boat-local measurements
first pass through the boat root into world space.

The spar's collider axis and dimensions determine whether the endpoints meet
their physical masts. The visible shaft determines the collar radius and any
collar offset. Collar offsets adjust the bands without moving the stay's
endpoints.

The halyard guide is selected by `guideIndex` from the aft mast's `mastReefAtt`
array, or from `mastReefAttExtension` when `extendedGuide` is true. The selected
transform must exist and be fitted. The common Fisherman's Stay geometry aims
for a 70-degree angle to the aft spar's downward axis, with a forward masthead
fallback when necessary. This is a design choice for these stays, not a
requirement imposed by Sailwind.

## Mount identity and Brig example

This is the existing configuration for mount **128**. `mountIndex` becomes the
stay's `Mast.orderIndex` and its slot in `BoatRefs.masts`. Custom mounts use
**128–255**, and each new mount needs a free slot on its boat. This range is a
MoreSailwindSails convention; other mods can occupy those slots too.

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
    forbidden: new[] { 55, 56 }
)
```

These types are internal to MoreSailwindSails. The example explains its source
configuration; it is not a public plugin API.

## Fitting conditions

The `required` list includes both endpoint sections and any necessary lower
sections. The boat reference's parent-mast relationships identify these
supporting sections: a topmast can depend on a lower mast even when their Unity
transforms are siblings. The `forbidden` list identifies conflicting options.
Exclusions can identify existing stays as well as physical spars; Chronian
excludes its Mizzen Royalstay from one custom-stay configuration.

Stay configurations belong to groups in each boat's stay definition. The shared
boat catalog supplies boat identity and physical ancestry; the stay catalog
collects the boat-specific stay definitions. Control fallback configuration is
separate from stay geometry; the [control guide](How-winches-are-placed)
explains when it is needed.

Missing donor or required parts, or a missing selected guide, make a
configuration unavailable. Missing excluded parts impose no restriction. Fitting
conditions are evaluated again as the shipyard changes parts, with checks
protecting an occupied stay and its supporting masts.

## Shipyard registration and saved selections

Registration adds Fisherman's Stays to the shipyard while preserving the meaning
of existing saves. It runs after `SaveableBoatCustomization.Awake` and optional
Shipyard Expansion setup, appending rigging groups to
`BoatCustomParts.availableParts`. Each group's option `0` is **None**;
subsequent options represent stay mounts in configuration order.

Saves identify a fitting by its mount ID, part-group position, and option
position. Those values must stay stable: inserting an option before a saved
selection changes what that selection means. Unavailable mounts retain a
placeholder that cannot be ordered, so missing optional rigging does not shift
later options.

Configurations sharing a `MountIndex` form one option. For example,
[Dhow](Boat-Dhow) uses mount **128** for both short and tall mainmasts; the stay
selects the attachment configuration matching the fitted parts. These are two
configurations of one fitting, not two saved choices.

Registration rejects occupied mount IDs and rolls back new stays and groups if
construction fails. Runtime and saved mast arrays and shipyard mast buttons
expand to at least **256** slots. Loading an older save or restoring a cancelled
order with no entry for an appended group selects **None**.

## Source

- [Brig's authored configurations](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/Boats/BrigStays.cs)
- [Geometry calculations](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/FishermansStayGeometry.cs)
- [Variant fields and allowed values](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/FishermansStayVariantDefinition.cs)
- [Reference and endpoint checks](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/FishermansStayReferences.cs)
- [Stay construction and fitting](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/src/Stays/FishermansStay/FishermansStay.cs)
- [Stay catalog](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Configuration/FishermansStayCatalog.cs)
- [Shared boat catalog](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/BoatConfiguration/BoatCatalog.cs)
- [Geometry checks for authored stays](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/tests/GeometryChecks/FishermansStay/StayChecks.cs)
- [Registration, placeholders, and rollback](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/FishermansStayRegistry.cs)
- [Registration, load, and shipyard patches](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/Patches/StaySailwindPatches.cs)
- [Missing snapshot entries](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/Stays/FishermansStay/FishermansStaySaveState.cs)
