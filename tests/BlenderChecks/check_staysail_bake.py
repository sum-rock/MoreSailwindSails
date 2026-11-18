"""Exercise baking and incompatible-edit rejection without saving or changing the asset."""

import argparse
from pathlib import Path
import runpy
import sys


root = Path(__file__).resolve().parents[2]
tool = runpy.run_path(str(root / "tools/assets/staysail/bake.py"))
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--cut", choices=("A", "B", "C"), default="A")
args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
cut = parser.parse_args(args).cut


def bake():
    return tool["bake"](cut=cut)


sys.path.insert(0, str(Path(__file__).resolve().parent))
from sail_bake_checks import check

if cut == "B":
    # The animated asset must retain its hooks and simulation during checks.
    import bpy
    from tools.assets.staysail.animation import samples

    source = bake()
    assert source == bake(), "Animated bake is not deterministic"
    assert source == tool["source_path"](cut=cut).read_text(), "Stale Mk.B bake"
    poses = samples()
    assert set(poses) == {
        60,
        90,
        120,
        200,
        240,
        280,
        320,
        360,
        400,
        420,
        500,
        540,
        580,
        620,
        660,
        700,
        720,
    }
    obj = bpy.data.objects[f"Fishermans Staysail Mk.{cut}"]
    bpy.context.scene.frame_set(420)
    evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    point = evaluated.data.vertices[824].co
    span = obj.data.vertices[24].co.x
    assert (
        max(
            abs(a - b)
            for a, b in zip(
                poses[420][824], (point.z / span, -point.y / span, point.x / span - 1)
            )
        )
        < 1e-6
    )
    modifier = obj.modifiers.new(name="Invalid topology", type="SOLIDIFY")
    try:
        try:
            bake()
        except ValueError:
            pass
        else:
            raise AssertionError("Accepted topology-changing modifier")
    finally:
        obj.modifiers.remove(modifier)
    print(
        "PASS: Mk.B deterministic animation bake, frame samples, final clew and modifier rejection."
    )
else:
    check(
        bake=bake,
        source_path=tool["source_path"](cut=cut),
        object_name=f"Fishermans Staysail Mk.{cut}",
        main=tool["main"],
        cli_args=("--cut", cut),
    )
