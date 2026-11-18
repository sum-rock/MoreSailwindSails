"""Collect validated Blender data and write a private JSON handoff to the host renderer."""

import argparse
import importlib
import json
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))


def collect(*, asset, cut):
    if asset in ("flying_sail", "staysail"):
        from tools.assets.grid_collect import collect as collect_grid

        return collect_grid(asset=asset, cut=cut)
    module = importlib.import_module(f"tools.assets.{asset}.collect")
    return (
        module.collect_cut(cut=cut)
        if asset == "loose_foot_spritsail"
        else module.collect()
    )


def write(*, data, output):
    from mathutils import Matrix, Vector, Quaternion

    def convert(value):
        if isinstance(value, Matrix):
            return [list(column) for column in value.col]
        if isinstance(value, (Vector, Quaternion)):
            return list(value)
        raise TypeError(f"Unsupported extracted value: {type(value).__name__}")

    # Serialize before writing, so invalid numeric data cannot leave a partial handoff.
    source = json.dumps(data, default=convert, allow_nan=False)
    output.write_text(source, encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--asset",
        choices=("flying_sail", "staysail", "stay", "loose_foot_spritsail"),
        required=True,
    )
    parser.add_argument("--cut", default="A")
    parser.add_argument("--output", type=Path, required=True)
    options = parser.parse_args(sys.argv[sys.argv.index("--") + 1 :])
    write(data=collect(asset=options.asset, cut=options.cut), output=options.output)


if __name__ == "__main__":
    main()
