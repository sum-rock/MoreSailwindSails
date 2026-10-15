"""Bake the open staysail Blender file into deterministic, normalized C# mesh data."""

import argparse
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import sail_authoring
from staysail_authoring import CUTS, WIDTH, dimensions, source_path


def bake(cut: str = "A") -> str:
    return sail_authoring.bake(
        width=WIDTH,
        heights=dimensions(cut=cut),
        object_name=f"Fishermans Staysail Mk.{cut}",
        namespace=f"MoreSailwindSails.Sails.FishermansStaysail.Mk{cut}",
        class_name=f"FishermansStaysailMk{cut}BakedMesh",
        data_type="FishermansStaysailMeshData",
        source_comment=f"// Baked by tools/bake_staysail.py from the Mk.{cut} Blender authoring mesh.",
    )


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cut", choices=CUTS, default="A")
    parser.add_argument("--output", type=Path)
    parser.add_argument(
        "--check", action="store_true", help="Fail if output differs; write nothing."
    )
    args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    options = parser.parse_args(args)
    options.output = options.output or source_path(cut=options.cut)
    source = bake(cut=options.cut)
    sail_authoring.write_bake(source=source, output=options.output, check=options.check)


if __name__ == "__main__":
    main()
