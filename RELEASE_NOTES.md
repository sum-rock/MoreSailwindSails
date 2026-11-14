# MoreSailwindSails 4.0.0

- Added **Loose Footed Spritsails** with four variations.
- **VirtualCrew can now discover Fisherman's Stays and their fitted sails.**
  Updated sail hinge anchors to preserve correct attachment under the boat
  model.
- **Fisherman's Staysails can now share a mast with gaffs or spritsails whose
  spars cross the staysail cloth**, provided their luffs do not touch or
  overlap.
- Added **two sheet-winch fallback pairs on Brig, three on Sanbuq and two on
  Caelanor**, providing more control positions for crowded sail plans.
- Guarded native control attachment against missing or occupied winch slots,
  preserving existing sail controls while assigning Flying Sail controls.
- Fixed **SailInfo's optional winch coloring during loading** attempting to
  access unavailable winch slots for sails managed by the mod.
