"""Check the Flying Sail bake without saving or changing its source asset."""

from pathlib import Path
import runpy
import sys

root = Path(__file__).resolve().parents[2]
tool = runpy.run_path(str(root / "tools/assets/bake_flying_sail.py"))
sys.path.insert(0, str(Path(__file__).resolve().parent))
from sail_bake_checks import check

check(
    bake=tool["bake"],
    source_path=tool["SOURCE_PATH"],
    object_name=tool["OBJECT_NAME"],
    main=tool["main"],
)
