# Sailwind rigging modding reference

This wiki explains how to identify boat rigging, add custom stays, build sail
shapes and physics, and connect shipyard options and controls. It is for
Sailwind modders, with MoreSailwindSails as a working example.

## Find an answer

| I want to…                                             | Start here                                                 |
| ------------------------------------------------------ | ---------------------------------------------------------- |
| Look up a boat's mast and stay indexes                 | [Boat reference](Boats)                                    |
| Browse the sail families                               | [Sail reference](Sails)                                    |
| Understand Staysail IDs, shape, physics, and assets    | [Fisherman's Staysails](Fishermans-Staysails)              |
| Understand Flying Sail IDs, shape, physics, and assets | [Fisherman's Flying Sails](Fishermans-Flying-Sails)        |
| Understand Fisherman's Stay configuration              | [Fisherman's Stay](Fishermans-Stay)                        |
| Understand control placement and ownership             | [Controls reference](Controls)                             |
| Look up fallback donors and mounting measurements      | [Fallback winch templates](Fallback-winch-templates)       |
| Inspect winch positions or capture a fallback contact  | [Winch placement diagnostics](Winch-placement-diagnostics) |
| Check a mapping against my installed game              | [Boat inspection command](MSS-command#boat-inspection)     |
| Build this mod or contribute a correction              | [Developer Guide](Developer-Guide)                         |

## Controls

- [How winches are placed](How-winches-are-placed): native positions, authored
  fallbacks, fitted sail spans, occupancy, and shared reservations for both sail
  families.
- [Fallback winch templates](Fallback-winch-templates): donor array indexes,
  per-pair sheet templates, halyard templates, mounting frames, and runtime
  clones.
- [Winch placement diagnostics](Winch-placement-diagnostics): overlay colors,
  native-scale model previews, forced sheet fallbacks, and surface captures.

## Developer Guide

- [Building and contributing](Building-and-contributing): environment setup,
  builds, automated checks, and contributions.
- [Profiling](Profiling): CPU and frame capture settings, category selection,
  and timing, allocation, and GC reports.
- [mss command](MSS-command): asset tools, boat inspection, fallback generation,
  installation, releases, decompilation, and logs.

## What the references describe

The boat pages cover objects used by this mod's stay definitions. Each page
separates installed object names and providers from authored parent
relationships and custom stay mappings. They are a reference for those fittings,
not a complete inventory of every object on a boat.

Source links identify the inspected revision for each topic. The boat reference
links to revision
[`43540e5`](https://github.com/sum-rock/MoreSailwindSails/tree/43540e59195537d3f5504c983f4c48ee5a9317fb),
whose plugin version is **0.3.0**; other topics may cite newer revisions. Each
boat page lists the inspected game/mod versions and date. Available parts can
change between versions; the
[boat inspection command](MSS-command#boat-inspection) can check supported boats
against your installation.

For player instructions, see the
[project README](https://github.com/sum-rock/MoreSailwindSails#readme).
