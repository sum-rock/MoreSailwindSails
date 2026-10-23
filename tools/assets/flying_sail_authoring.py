"""Nominal Flying Sail dimensions and authoring outputs; preserve the installed base size."""

import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WIDTH = 4.6
RISE = WIDTH * math.tan(math.radians(20))
HEIGHTS = (RISE, -2 * WIDTH, -2 * WIDTH - RISE)
OBJECT_NAME = "Fishermans Flying Sail"
ASSET_PATH = ROOT / "assets/flying_sail/fishermans_flying_sail.blend"
SOURCE_PATH = ROOT / "src/Sails/FishermansFlyingSail/FishermansFlyingSailBakedMesh.g.cs"
