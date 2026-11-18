"""Check the Flying Sail JSON pipeline without saving its source asset."""

from pathlib import Path
import sys

root = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(root))
sys.path.insert(0, str(Path(__file__).resolve().parent))
from tools.assets.flying_sail.authoring import SOURCE_PATH, OBJECT_NAME
from json_bake_checks import bake
from sail_bake_checks import check

check(
    bake=lambda: bake(asset="flying_sail"),
    source_path=SOURCE_PATH,
    object_name=OBJECT_NAME,
)
