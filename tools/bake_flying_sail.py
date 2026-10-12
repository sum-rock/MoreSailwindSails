"""Bake the open Flying Sail asset into normalized C# mesh data without saving Blender."""

import argparse
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import sail_authoring
from flying_sail_authoring import HEIGHTS, OBJECT_NAME, SOURCE_PATH, WIDTH


def bake() -> str:
    return sail_authoring.bake(
        width=WIDTH, heights=HEIGHTS, object_name=OBJECT_NAME,
        namespace="MoreSailwindSails.Sails.FishermansFlyingSail",
        class_name="FishermansFlyingSailBakedMesh", data_type="FishermansFlyingSailMeshData",
        source_comment="// Baked by tools/bake_flying_sail.py from the Flying Sail Blender authoring mesh.",
    )


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=SOURCE_PATH)
    parser.add_argument("--check", action="store_true", help="Fail if output differs; write nothing.")
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    options = parser.parse_args(args)
    sail_authoring.write_bake(source=bake(), output=options.output, check=options.check)


if __name__ == "__main__":
    main()
