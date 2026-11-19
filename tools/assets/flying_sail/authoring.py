"""Flying Sail object name and authoring paths."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OBJECT_NAME = "Fishermans Flying Sail"
ASSET_PATH = ROOT / "assets/flying_sail/fishermans_flying_sail.blend"
SOURCE_PATH = ROOT / "src/Sails/FishermansFlyingSail/FishermansFlyingSailBakedMesh.g.cs"
