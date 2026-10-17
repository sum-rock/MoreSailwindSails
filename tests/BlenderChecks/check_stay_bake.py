"""Exercise stay baking with temporary in-memory edits; never save the authoring asset."""

import hashlib
import math
from pathlib import Path
import runpy
import sys
import tempfile

import bpy

ROOT = Path(__file__).resolve().parents[2]
tool = runpy.run_path(str(ROOT / "tools/bake_stay.py"))
bake, extract, main = tool["bake"], tool["extract"], tool["main"]
asset = Path(bpy.data.filepath)
before = hashlib.sha256(asset.read_bytes()).digest()
source = bake()
assert source == bake(), "Bake is not deterministic"
assert source == tool["SOURCE_PATH"].read_text(encoding="utf-8"), (
    "Stale checked-in bake"
)


def near(actual, expected):
    assert math.isclose(actual, expected, abs_tol=1e-6), (actual, expected)


rope = extract(name="Rope", rope=True)
collar = extract(name="ForestayCollar", rope=False)
near(rope["dimensions"]["RopeLength"], 1)
near(rope["dimensions"]["RopeRadius"], 0.02)
near(collar["dimensions"]["CollarInnerRadius"], 0.2037)
near(collar["dimensions"]["CollarOuterRadius"], 0.21)
near(collar["dimensions"]["CollarHalfHeight"], 0.05)
near(min(p[2] for p in rope["Vertices"]), -1)
near(max(p[2] for p in rope["Vertices"]), 0)
assert len(rope["Triangles"]) == 124 * 3
assert len(collar["Triangles"]) == 256 * 3, "Solidify was not evaluated"
for data in (rope, collar):
    corners = {}
    for point, normal, uv in zip(data["Vertices"], data["Normals"], data["UV"]):
        normals, uvs = corners.setdefault(point, (set(), set()))
        normals.add(normal)
        uvs.add(uv)
    assert any(len(n) > 1 for n, _ in corners.values()), "Flat shading was lost"
    assert any(len(u) > 1 for _, u in corners.values()), "UV seams were lost"


def edit_case(*, name, label, edit, verify=None):
    obj = bpy.data.objects[name]
    original = obj.data
    location, rotation, scale = (
        obj.location.copy(),
        obj.rotation_euler.copy(),
        obj.scale.copy(),
    )
    modifiers = list(obj.modifiers)
    obj.data = original.copy()
    try:
        edit(obj)
        bpy.context.view_layer.update()
        if verify is not None:
            verify()
        else:
            try:
                bake()
            except ValueError:
                pass
            else:
                raise AssertionError(f"Accepted invalid edit: {label}")
    finally:
        modified = obj.data
        obj.data = original
        obj.name = name
        obj.location, obj.rotation_euler, obj.scale = location, rotation, scale
        for modifier in list(obj.modifiers):
            if modifier not in modifiers:
                obj.modifiers.remove(modifier)
        bpy.data.meshes.remove(modified)
        bpy.context.view_layer.update()
    assert bake() == source, f"Edit leaked into source: {label}"


def same_bake():
    assert bake() == source, "Presentation pose changed the reusable meshes"


for name in tool["OBJECT_NAMES"]:
    edit_case(
        name=name, label="missing object", edit=lambda o: setattr(o, "name", "Missing")
    )
    edit_case(
        name=name,
        label="missing UV",
        edit=lambda o: o.data.uv_layers.remove(o.data.uv_layers.active),
    )
    edit_case(
        name=name,
        label="unsupported modifier",
        edit=lambda o: o.modifiers.new(name="Bevel", type="BEVEL"),
    )
    edit_case(
        name=name,
        label="non-finite vertex",
        edit=lambda o: setattr(o.data.vertices[0].co, "x", float("nan")),
    )
    edit_case(
        name=name,
        label="non-finite UV",
        edit=lambda o: setattr(
            o.data.uv_layers.active.data[0], "uv", (float("nan"), 0)
        ),
    )
    edit_case(
        name=name, label="loose vertex", edit=lambda o: o.data.vertices.add(count=1)
    )
    edit_case(
        name=name, label="reflected scale", edit=lambda o: setattr(o.scale, "x", -1)
    )
    edit_case(
        name=name, label="shape keys", edit=lambda o: o.shape_key_add(name="Basis")
    )
    edit_case(
        name=name,
        label="translation",
        edit=lambda o: setattr(o, "location", (12, 3, -9)),
        verify=same_bake,
    )
    edit_case(
        name=name,
        label="rotation",
        edit=lambda o: setattr(o, "rotation_euler", (0.8, -0.3, 1.2)),
        verify=same_bake,
    )

edit_case(
    name="AftstayCollar",
    label="mismatched collars",
    edit=lambda o: setattr(o.scale, "z", o.scale.z * 2),
)
edit_case(
    name="Rope", label="inconsistent winding", edit=lambda o: o.data.polygons[0].flip()
)


def check_scaled_rope():
    data = extract(name="Rope", rope=True)
    near(data["dimensions"]["RopeLength"], 2)
    near(data["dimensions"]["RopeRadius"], 0.02)


edit_case(
    name="Rope",
    label="baked scale",
    edit=lambda o: setattr(o.scale, "x", o.scale.x * 2),
    verify=check_scaled_rope,
)

with tempfile.TemporaryDirectory(prefix="mss-stay-bake-") as directory:
    output = Path(directory) / "mesh.g.cs"
    args = sys.argv
    try:
        sys.argv = ["blender", "--", "--check", "--output", str(output)]
        for stale in (None, "stale source"):
            if stale is not None:
                output.write_text(stale, encoding="utf-8")
            try:
                main()
            except RuntimeError:
                pass
            else:
                raise AssertionError("Stale output was accepted")
            assert not output.exists() if stale is None else output.read_text() == stale
        sys.argv = ["blender", "--", "--output", str(output)]
        main()
        assert output.read_text() == source
        sys.argv.insert(2, "--check")
        main()
    finally:
        sys.argv = args

assert hashlib.sha256(asset.read_bytes()).digest() == before, "Authoring file changed"
assert bake() == source, "Checks changed the in-memory authoring geometry"
print(
    "PASS: stay dimensions, Solidify, seams/shading, canonical frames, deterministic bake, invalid asset rejection and read-only freshness checks."
)
