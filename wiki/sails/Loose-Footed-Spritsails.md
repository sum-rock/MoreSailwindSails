# Loose Footed Spritsails

Loose Footed Spritsails are mast-mounted sails with a free foot and no boom. The
luff is tied to the mast; a wooden sprit supports the peak from a snotter
assembly around the mast. The sprit and snotter turn with the sail, while the
rotation plate remains fixed in the mast frame.

Loose Footed Spritsails were introduced in **0.4.0**. This reference follows
revision `f453a17` (plugin metadata **0.4.1**). The linked code is the source of
truth.

## Cuts and fitting

| Shipyard name               | Saved prefab ID | Asset                                                           |
| --------------------------- | --------------- | --------------------------------------------------------------- |
| Loose Footed Spritsail Mk.A | 404             | Mk.A cloth and reef poses                                       |
| Loose Footed Spritsail Mk.B | 405             | Separately authored Mk.B cloth and reef poses                   |
| Loose Footed Spritsail Mk.C | 406             | Cloth with an integrated topsail and staged reef poses          |
| Loose Footed Spritsail Mk.D | 407             | Separately authored cloth, integrated topsail, and topsail spar |

All four appear under **Other** at shipyards. Select a physical mast, choose a
cut, then size and position it using the shipyard controls. There is no
boat-specific eligibility list and no aft supporting mast or Fisherman's Stay
requirement. Native mount restrictions still apply: spritsails cannot use stays
or square-only mounts. Eligible masts also need enough clearance and available
control positions.

The initial luff height is based on the native Full Gaff. New sails start at
unit scale, fully deployed, white (color index 11), and at the highest
installation position allowed by their topsail rise. Height and width can change
independently. The reported sail area is the baked triangle area multiplied by
the base scale squared and the fitted height and width scales. Hardware is
fitted separately from the stretched cloth; the snotter radius follows the mast
surface where it can be measured, with collider and authored-radius fallbacks.

Mk.C and Mk.D need room above the main sail for the topsail. The [mast support
resolver][support] includes active descendant sections from the boat's authored
ancestry map, provided the connecting sections are fitted. This lets the
mainsail use a lower mast while the topsail extends onto its connected topmast.
Without a known ancestry map, support is limited to the selected mast.

The upper installation limit is the lower of the selected mast's height and
available support height minus scaled topsail rise. The luff must fit below the
installation point, and the fully deployed topsail must fit above it; reefing
does not relax these limits. Shipyard collision strips follow the fitted sail
and sprit through the rotation sweep. Spritsails can coexist with square sails
on the same selected mast mount. For Fisherman's Staysails on the same physical
mast, [luff compatibility](Fishermans-Staysails#placement-and-coexistence)
allows crossing spars or cloth when the luffs neither touch nor overlap. Other
overlaps retain their checks.

Sources: [Mk.D registration][mkd], [prefab construction][prefab], [fitting
geometry][fitting], [rig][rig], and [shipyard patches][patches].

## Sheets, halyard, and reefing

Port and starboard sheets both terminate at the clew. Native jib controllers and
a hinge provide the live trim motion, constrained to the clearance angles found
by the shipyard. Both sheet winches must be placed before the sheets activate
and the sail can produce force.

The rig uses the [shared control allocator](How-winches-are-placed), requesting
native controls from its fitted mast and using eligible authored fallbacks. Its
neutral fitted span constrains sheet placement. Missing positions leave controls
hidden; they are retried as positions become available. The halyard is retried
independently, so a missing halyard does not retire working sheets.

The halyard runs from its winch through a mast guide to the sprit attachment.
The guide at the sail's mast order is preferred; otherwise the nearest eligible
regular or extension guide is used. Eligibility checks the route across the
reefing motion so taking in the line does not require it to lengthen. Three
sliding luff ties are drawn around the mast. Family-owned ropes use the shared
rope renderer, alongside native sheet ropes.

The normal reef control drives deployment (`currentUnroll`). Cloth and sprit
interpolate authored reef poses rather than using native cloth simulation or the
donor's furl animation. Mk.A and Mk.B each have five sampled poses. Mk.C has
nine samples with explicit reef fractions: its topsail is visible only above 80%
deployment, so it disappears at 20% reefing. Mk.D has eleven samples, including
identical cloth poses at 20% and 25% reefing. Its topsail and separate topsail
spar also disappear at 20% reefing; the spar follows its authored reef
transforms while visible. Both topsail cuts' force reduction uses remaining
projected cloth area, including the topsail while visible, instead of the native
linear deployment reduction.

At deployment of 0.001 or less, an installed sail is struck: the deployed cloth
and sprit are hidden, a bundled mesh is shown, and sheet and halyard endpoints
move to that bundle. Luff ties disappear. Wind torque and propulsion stop,
without resetting the sail's azimuth.

Sources: [control placement][rigging], [rope routes][ropes], [cloth
posing][cloth], [reef geometry][geometry], and [cut catalog][catalog].

## Price

All four cuts use the native **Other** category price: **9 × fitted sail area**,
before shipyard currency conversion. There is no separate spritsail surcharge or
fixed price per cut. Enlarging the sail increases its price with area; reefing
does not reduce its purchase price. Mk.C and Mk.D purchased areas include their
topsails.

For equal reported area, the native category prices compare as follows:

| Sail category                  | Base price per unit of area | Example: area 20 |
| ------------------------------ | --------------------------- | ---------------- |
| Loose Footed Spritsail (Other) | 9                           | 180              |
| Lateen                         | 10.35                       | 207              |
| Gaff                           | 11.25                       | 225              |
| Junk                           | 11.97                       | 239.4            |

Thus a spritsail is 20% cheaper than an equal-area gaff and about 25% cheaper
than an equal-area junk sail before conversion and rounding. Different cuts and
sizes can have different areas, so these are area-for-area comparisons, not
fixed shop prices.

Sources: [category and prefab setup][prefab], [reported area patch][patches],
and installed-game `Sail.GetSailPrice()`.

## Sailing force and bad tack

The sail retains native wind-force calculations, with Full Gaff upwind
efficiency and a **0.80 power multiplier**. On the bad tack it receives an
additional **0.90 multiplier**: 0.72 combined, before reefing and other native
factors. These are multipliers on native calculated power, not a promise of 80%
of a particular gaff sail's thrust or boat speed. For equal area at full
deployment, the power factors are:

| Sail                              | Power factor |
| --------------------------------- | ------------ |
| Native gaff                       | 0.85         |
| Loose Footed Spritsail, good tack | 0.80         |
| Native junk                       | 0.75         |
| Loose Footed Spritsail, bad tack  | 0.72         |

The spritsail's good-tack factor sits between gaff and junk; its bad-tack factor
is below both. With all other force inputs held equal, the good-tack factor is
about 94% of gaff's and 107% of junk's. These factors scale both forward and
sideways force. Wind angle, apparent wind speed, sail height, shadowing, upwind
efficiency, and deployment still affect the actual result. Native gaff and junk
reductions are applied in `Sail.ApplyForce()`; spritsails use Other, so neither
reduction stacks with the mod's multiplier.

Bad tack means apparent airflow presses the cloth across the sprit: the dot
product of apparent wind and the fitted frame's positive X direction is
positive. Flipping the fitting reverses that side. Zero dot product receives no
penalty.

The wind sensor uses baked, authored positions and orientations:

| Cuts          | Active authored pose                                                                            |
| ------------- | ----------------------------------------------------------------------------------------------- |
| Mk.A and Mk.B | `WindCenter` throughout deployment                                                              |
| Mk.C and Mk.D | `WindCenter` above 80% deployment; `WindCenterMain` at 80% or below, when the topsail is hidden |

The rig fits these poses during installation, resizing, and flipping. The sensor
lives under the resize hierarchy and follows the sail's live hinge motion. Its
local pose stays fixed between refits and topsail visibility changes; animated
corners, reef gathering, billow, and flutter do not move it. The switch to the
main-only pose is immediate at the existing 20% reefing boundary, and reverses
when the topsail reappears.

Native force uses the sensor's normal facing apparent wind. Mk.C/D still use
remaining projected cloth area for reefing power reduction; that area no longer
determines the sensor's center. Apparent wind is current wind minus boat
velocity, refreshed before native physics applies force. Unavailable sheets,
invalid fitting, closed sails, and shipyard operations clear cached force rather
than allowing stale propulsion. A closed sail also returns a finite
captured-force reading for HUD consumers.

By default, a bottom-right notice reads **Spritsail bad tack: −10% force** when
any sailing-ready, deployed spritsail on the player's boat is on the bad tack.
It stays visible for 30 seconds, then fades over two seconds. It rearms when no
such sail remains on the bad tack; menus and other hidden display states pause
the timer. In the BepInEx configuration, set
`[Interface] ShowSpritsailBadTackNotice = false` to hide it. This setting does
not change the penalty.

Sources: [sailing calculations][sailing], [physics refresh][physics], [power
patches][patches], [notice][notice], and [configuration][plugin].

## Appearance and optional integrations

Cloth is plain white initially and supports normal shipyard recoloring. The
struck bundle shares the sail's color. Shipyard Expansion texture handling
restricts the family to plain cloth, including normalization of saved patterned
selections. Visual billow changes side with wind pressure and flutter responds
to wind capture and shadowing; these visual offsets do not redefine the physical
rope attachment positions or authored wind sensor.

Shipyard Expansion is optional. When its supported API is present, its saved
flip toggle mirrors the fitting, cloth, and hardware and reverses the bad tack.
Its forward/backward tilt buttons are hidden for spritsails. An incompatible API
produces a warning instead of enabling that integration. SailInfo is also
optional; its control labels identify the family as **Loose Footed Spritsail**.

Sources: [cloth rendering][cloth], [flip integration][flip], [texture
integration][textures], and [SailInfo names][names].

## Implementation and asset ownership

Each cut supplies a stable prefab ID, display name, and baked asset factory to
one shared builder. It clones native brig jib prefab **110** for sheet and reef
controllers and uses Full Gaff **15** for initial height, upwind efficiency, and
wood material. Donor visuals, collision, and cloth animation are disabled in
favor of the authored meshes and fitting logic. Registration refuses to replace
an occupied prefab ID and cleans up assets after construction failure.

Blender sources live in `assets/loose_foot_spritsail/`; the cut catalog maps
them to generated C# assets under each cut's source directory. For example,
`./mss assets bake loose_foot_spritsail --cut D` bakes Mk.D from
`assets/loose_foot_spritsail/loose_foot_spritsail-mkD.blend`. The baker exports
cloth, reef samples, rigid hardware, the struck bundle, attachment data, and
wind-center poses; it does not save the Blender file. Runtime loading uses the
generated data. See the [MSS command reference](MSS-command#asset-baking) for
cut selection, output paths, and bake checks.

Every cut requires a `WindCenter` Empty; topsail cuts also require
`WindCenterMain` for the main-only state. The [collector][collector] reads these
markers at frame 0 relative to the authored `Sail` rest frame and exports
position and rotation. Markers must be unparented, unanimated, unconstrained,
and unit scale, with finite, invertible, unreflected transforms. Author the
sensor axes with right along the sail chord and forward up the luff; the bake
converts them to the runtime coordinate system. Runtime fitting preserves a
proper sensor rotation when the sail is flipped.

The prefab asset owner releases meshes and materials it creates. Instances
borrow those assets but own their dynamic cloth mesh and recolored material.
Control cleanup releases allocator reservations and owned winches, and destroys
detached controllers when the rig is destroyed. This separation avoids deleting
shared donor assets or leaving controls behind.

Existing automated coverage includes baked assets, fitting, cloth and reefing,
flutter, authored wind-center fitting and topsail switching, sailing
calculations, collision integration, and optional flip wiring. See the
[build guide](Building-and-contributing) for the check suites.

Sources: [builder][prefab], [asset ownership][assets], [cut catalog][catalog],
[baker][baker], [geometry checks][checks], and [assembly checks][assembly].

[prefab]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Sails/LooseFootedSpritsail/LooseFootedSpritsailPrefab.cs
[fitting]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Sails/LooseFootedSpritsail/LooseFootedSpritsailFittingGeometry.cs
[rig]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Sails/LooseFootedSpritsail/LooseFootedSpritsailRig.cs
[rigging]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Sails/LooseFootedSpritsail/LooseFootedSpritsailRigging.cs
[ropes]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Sails/LooseFootedSpritsail/LooseFootedSpritsailRopes.cs
[cloth]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Sails/LooseFootedSpritsail/LooseFootedSpritsailCloth.cs
[geometry]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Sails/LooseFootedSpritsail/LooseFootedSpritsailClothGeometry.cs
[sailing]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Sails/LooseFootedSpritsail/LooseFootedSpritsailSailingGeometry.cs
[physics]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Sails/LooseFootedSpritsail/LooseFootedSpritsailPhysics.cs
[notice]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Sails/LooseFootedSpritsail/LooseFootedSpritsailNotice.cs
[assets]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Sails/LooseFootedSpritsail/LooseFootedSpritsailAssets.cs
[patches]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Sails/LooseFootedSpritsail/Patches/LooseFootedSpritsailSailwindPatches.cs
[catalog]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/tools/assets/loose_foot_spritsail/authoring.py
[baker]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/tools/assets/loose_foot_spritsail/bake.py
[plugin]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Plugin.cs
[flip]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Compatibility/Patches/ShipyardExpansionSpritsailFlipPatch.cs
[textures]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Compatibility/Patches/ShipyardExpansionTexturesPatch.cs
[names]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Compatibility/Patches/SailInfoNamesPatch.cs
[checks]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/tests/GeometryChecks/LooseFootedSpritsail
[assembly]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/tests/AssemblyChecks/LooseFootedSpritsail
[mkd]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Sails/LooseFootedSpritsail/MkD/LooseFootedSpritsailMkD.cs
[support]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/src/Sails/LooseFootedSpritsail/LooseFootedSpritsailMastSupport.cs
[collector]:
  https://github.com/sum-rock/MoreSailwindSails/blob/f453a179075fcc3cf6675f98277e0cadaadcb6e4/tools/assets/loose_foot_spritsail/collect.py
