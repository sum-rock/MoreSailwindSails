"""Generate the editable flat Flying Sail with planar UVs in background Blender."""

import argparse
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from sail_authoring import generate
from flying_sail_authoring import ASSET_PATH, HEIGHTS, OBJECT_NAME, WIDTH

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ASSET_PATH)
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    generate(output=parser.parse_args(args).output.resolve(), width=WIDTH,
             heights=HEIGHTS, object_name=OBJECT_NAME)
