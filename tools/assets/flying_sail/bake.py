"""Bake the open Flying Sail asset into normalized C# mesh data without saving Blender."""

import argparse
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[3]))
from tools.assets import sail_baking
from tools.assets.flying_sail.authoring import OBJECT_NAME, SOURCE_PATH


def bake() -> str:
    return sail_baking.bake(
        object_name=OBJECT_NAME,
        namespace="MoreSailwindSails.Sails.FishermansFlyingSail",
        class_name="FishermansFlyingSailBakedMesh",
        data_type="FishermansFlyingSailMeshData",
        source_comment="// Baked by mss assets bake flying_sail from the Flying Sail Blender authoring mesh.",
    )


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=SOURCE_PATH)
    parser.add_argument(
        "--check", action="store_true", help="Fail if output differs; write nothing."
    )
    args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    options = parser.parse_args(args)
    sail_baking.write_bake(source=bake(), output=options.output, check=options.check)


if __name__ == "__main__":
    main()
