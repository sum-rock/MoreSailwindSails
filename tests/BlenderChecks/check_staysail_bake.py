"""Exercise baking and incompatible-edit rejection without saving or changing the asset."""

import argparse
from pathlib import Path
import runpy
import sys
import tempfile

import bpy


root = Path(__file__).resolve().parents[2]
tool = runpy.run_path(str(root / "tools/bake_staysail.py"))
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--cut", choices=("A", "B", "C"), default="A")
args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
cut = parser.parse_args(args).cut


def bake():
    return tool["bake"](cut=cut)

source = bake()
assert source == bake(), "Bake is not deterministic"
assert source == tool["source_path"](cut=cut).read_text(encoding="utf-8"), "Stale checked-in bake"
obj = bpy.data.objects[f"Fishermans Staysail Mk.{cut}"]
original = obj.data


def reject(label, edit):
    obj.data = original.copy()
    try:
        edit(obj)
        try:
            bake()
        except ValueError:
            pass
        else:
            raise AssertionError(f"Accepted incompatible edit: {label}")
    finally:
        modified = obj.data
        obj.data = original
        obj.location = (0, 0, 0)
        obj.modifiers.clear()
        bpy.context.view_layer.update()
        bpy.data.meshes.remove(modified)


def move_object(obj):
    obj.location.x = 1
    bpy.context.view_layer.update()


def add_vertex(obj):
    obj.data.vertices.add(count=1)


def flip_face(obj):
    face = obj.data.polygons[0]
    face.vertices = tuple(reversed(face.vertices))


reject("object transform", move_object)
reject("vertex count", add_vertex)
reject("non-planar surface", lambda o: setattr(o.data.vertices[10].co, "y", 0.1))
reject("vertex correspondence", lambda o: setattr(o.data.vertices[10].co, "x", 0.1))
reject("non-finite vertex", lambda o: setattr(o.data.vertices[10].co, "z", float("nan")))
reject("UV distortion", lambda o: setattr(o.data.uv_layers[0].data[0], "uv", (0, 0)))
reject("missing UV layer", lambda o: o.data.uv_layers.remove(o.data.uv_layers[0]))
reject("winding", flip_face)
reject("modifier", lambda o: o.modifiers.new(name="Solidify", type="SOLIDIFY"))
assert bake() == source, "Checks changed the source asset"

# Exercise the actual CLI check path and confirm stale checks never overwrite output.
with tempfile.TemporaryDirectory(prefix="mss-bake-check-") as directory:
    output = Path(directory) / "mesh.g.cs"
    original_args = sys.argv
    try:
        sys.argv = ["blender", "--", "--cut", cut, "--check", "--output", str(output)]
        for stale in (None, "stale source"):
            if stale is not None:
                output.write_text(stale, encoding="utf-8")
            try:
                tool["main"]()
            except RuntimeError:
                pass
            else:
                raise AssertionError("Stale check unexpectedly passed")
            assert (not output.exists()) if stale is None else output.read_text() == stale
        output.write_text(source, encoding="utf-8")
        tool["main"]()
    finally:
        sys.argv = original_args
print(f"PASS: Mk.{cut} deterministic bake, incompatible edit rejection and read-only freshness checks.")
