"""Exercise baking and incompatible-edit rejection without saving or changing the asset."""

import argparse
from pathlib import Path
import runpy
import sys


root = Path(__file__).resolve().parents[2]
tool = runpy.run_path(str(root / "tools/assets/bake_staysail.py"))
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--cut", choices=("A", "B", "C"), default="A")
args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
cut = parser.parse_args(args).cut


def bake():
    return tool["bake"](cut=cut)


sys.path.insert(0, str(Path(__file__).resolve().parent))
from sail_bake_checks import check

check(
    bake=bake,
    source_path=tool["source_path"](cut=cut),
    object_name=f"Fishermans Staysail Mk.{cut}",
    main=tool["main"],
    cli_args=("--cut", cut),
)
