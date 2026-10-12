"""Generate an editable, flat staysail cut and planar UVs in background Blender."""

import argparse
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import sail_authoring
from staysail_authoring import CUTS, WIDTH, asset_path, dimensions


def generate(output: Path, cut: str = "A") -> None:
    sail_authoring.generate(output=output, width=WIDTH, heights=dimensions(cut=cut),
                            object_name=f"Fishermans Staysail Mk.{cut}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cut", choices=CUTS, default="A")
    parser.add_argument("--output", type=Path)
    arguments = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    options = parser.parse_args(arguments)
    generate(output=(options.output or asset_path(cut=options.cut)).resolve(), cut=options.cut)
