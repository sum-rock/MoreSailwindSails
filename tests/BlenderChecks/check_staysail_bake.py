"""Exercise baking and incompatible-edit rejection without saving or changing the asset."""

import argparse
from pathlib import Path
import sys


root = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(root))
sys.path.insert(0, str(Path(__file__).resolve().parent))
from tools.assets.staysail.authoring import source_path
from json_bake_checks import bake as bake_json

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--cut", choices=("A", "B", "C"), default="A")
args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
cut = parser.parse_args(args).cut


def bake():
    return bake_json(asset="staysail", cut=cut)


sys.path.insert(0, str(Path(__file__).resolve().parent))

import hashlib
import bpy
from tools.assets.staysail.authoring import SAMPLE_FRAMES
from tools.assets.staysail.collect import samples, collect_cut, collect_windcenter

asset = Path(bpy.data.filepath)
before = hashlib.sha256(asset.read_bytes()).digest()
marker = bpy.data.objects["WindCenter"]
span = bpy.data.objects[f"Fishermans Staysail Mk.{cut}"].data.vertices[24].co.x
original_location = marker.location.copy()
original_rotation = marker.rotation_euler.copy()
original = collect_windcenter(span=span)
try:
    marker.location.x += span * 0.1
    marker.rotation_euler.z = 1.5707963267948966
    bpy.context.view_layer.update()
    changed = collect_windcenter(span=span)
    assert abs(changed["Center"][2] - original["Center"][2] - 0.1) < 1e-6
    from mathutils import Quaternion, Vector

    x, y, z, w = changed["WindRotation"]
    assert (
        Quaternion((w, x, y, z)) @ Vector((1, 0, 0)) - Vector((0, -1, 0))
    ).length < 1e-6
finally:
    marker.location = original_location
    marker.rotation_euler = original_rotation
    bpy.context.view_layer.update()


def reject_marker(edit, restore):
    try:
        edit()
        bpy.context.view_layer.update()
        try:
            collect_windcenter(span=span)
        except ValueError:
            pass
        else:
            raise AssertionError("Accepted incompatible WindCenter")
    finally:
        restore()
        bpy.context.view_layer.update()


reject_marker(
    lambda: setattr(marker, "name", "MissingMarker"),
    lambda: setattr(marker, "name", "WindCenter"),
)
reject_marker(
    lambda: setattr(marker, "scale", (2, 1, 1)),
    lambda: setattr(marker, "scale", (1, 1, 1)),
)
reject_marker(
    lambda: setattr(
        marker, "parent", bpy.data.objects[f"Fishermans Staysail Mk.{cut}"]
    ),
    lambda: setattr(marker, "parent", None),
)
reject_marker(
    lambda: marker.animation_data_create(), lambda: marker.animation_data_clear()
)
reject_marker(
    lambda: marker.constraints.new(type="COPY_LOCATION"),
    lambda: marker.constraints.clear(),
)
assert collect_windcenter(span=span) == original
assert bake() == source_path(cut=cut).read_text()
poses = samples(cut=cut)
assert set(poses) == set(SAMPLE_FRAMES)
obj = bpy.data.objects[f"Fishermans Staysail Mk.{cut}"]
span = obj.data.vertices[24].co.x
bpy.context.scene.frame_set(0)
for frame in range(1, max(SAMPLE_FRAMES) + 1):
    bpy.context.scene.frame_set(frame)
    evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = evaluated.to_mesh()
    try:
        if frame in poses:
            assert poses[frame] == [
                (v.co.z / span, -v.co.y / span, v.co.x / span - 1)
                for v in mesh.vertices
            ]
    finally:
        evaluated.to_mesh_clear()
modifier = obj.modifiers.new(name="Invalid topology", type="SOLIDIFY")
try:
    try:
        collect_cut(cut=cut)
    except ValueError:
        pass
    else:
        raise AssertionError("Accepted topology-changing modifier")
finally:
    obj.modifiers.remove(modifier)
assert hashlib.sha256(asset.read_bytes()).digest() == before
print(
    f"PASS: Mk.{cut} JSON bake, frame samples, modifier rejection and source preservation."
)
