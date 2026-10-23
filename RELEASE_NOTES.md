- **Improved winch placement** for Staysails and Flying Sails, with shared
  position reservations and native controls preferred over fallbacks.
- **Added and refined sheet and halyard fallbacks**, including base-game boat
  halyards and revised Brig, Chronian, Gloriana and Caelanor placements.
  Controls use boat-specific models and measured mounting positions.
- **Sheet fallbacks now account for sail size**, moving farther aft when needed
  as sails grow and retaining valid positions when sails shrink.
- **Staysails can now coexist with square sails on their forward mast** without
  the shipyard rejecting the combination.
- **Rebuilt Fisherman's Stay visuals** with 4 cm rope and metal collars fitted
  to mast surfaces, without inherited donor collision geometry.
- Reduced winch discovery work, temporary allocations and duplicate stay meshes.
- Added purple fallback markers, native-scale control previews and a
  `ForceSheetFallbacks` diagnostic setting.
- Consolidated developer and maintainer tools under **`mss`** and moved
  technical documentation to the local wiki.
