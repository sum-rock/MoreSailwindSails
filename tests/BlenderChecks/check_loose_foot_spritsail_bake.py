"""Check cut-independent spritsail baking using disposable in-memory fixtures."""

from contextlib import contextmanager
import hashlib
from pathlib import Path
import runpy
import sys
import tempfile

import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
tool = runpy.run_path(str(ROOT / "tools/assets/bake_loose_foot_spritsail.py"))
from loose_foot_spritsail_authoring import CUTS, SpritsailCut

bake, collect, extract = tool["bake"], tool["collect"], tool["extract_part"]
conversion = tool["CONVERSION"]
asset = Path(bpy.data.filepath)
before = hashlib.sha256(asset.read_bytes()).digest()
source = bake()
assert source == bake(), "Non-deterministic bake"
assert source == CUTS["A"].source_path.read_text(), "Stale checked-in bake"
data = collect()
assert len(data["Sail"]["Vertices"]) == 320
assert len(data["Sail"]["SubmeshTriangles"][0]) == 285 * 6
assert set(tool["PARTS"]) == {"Sail", "Sprit", "SnotterAssembly", "RotationPlate"}
assert data["Sail"]["Materials"] == ["Cloth"]
assert data["Sprit"]["Materials"] == ["Metal", "DarkWood"]
assert data["RotationPlate"]["Materials"] == ["Metal", "DarkWood"]
assert data["SnotterAssembly"]["Materials"] == ["Metal"]
assert data["Clew"] == data["SheetVertex"]
assert (
    Vector(data["Sail"]["Vertices"][data["Clew"]]) - Vector(data["SheetLineAttachment"])
).length < 1e-6

for name, sources in tool["PARTS"].items():
    part = data[name]
    root = bpy.data.objects[sources[0]]
    # Rest pose recovers every evaluated source position, including the rod in its new frame.
    exported = [part["RestTransform"] @ Vector(v) for v in part["Vertices"]]
    for source_name in sources:
        obj = bpy.data.objects[source_name]
        evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = evaluated.to_mesh()
        try:
            for vertex in mesh.vertices:
                expected = conversion @ obj.matrix_world @ vertex.co
                assert min((v - expected).length for v in exported) < 5e-6, (
                    name,
                    source_name,
                )
        finally:
            evaluated.to_mesh_clear()
    assert (
        part["RestTransform"].translation - conversion @ root.matrix_world.translation
    ).length < 1e-6
    for triangles in part["SubmeshTriangles"]:
        for offset in range(0, len(triangles), 3):
            ids = triangles[offset : offset + 3]
            a, b, c = [Vector(part["Vertices"][i]) for i in ids]
            cross = (b - a).cross(c - a)
            assert cross.length > 1e-10
            assert all(cross.dot(Vector(part["Normals"][i])) > 0 for i in ids)

# Both source meshes contribute to the one rigid assembly; modifiers actually add thickness.
assembly = data["SnotterAssembly"]
for source_name in ("Snotter", "SpritMountRod"):
    alone = extract("Hardware", (source_name,))
    assert len(assembly["SubmeshTriangles"][0]) > len(alone["SubmeshTriangles"][0])
for name in ("Sprit", "RotationPlate", "SnotterAssembly"):
    part = data[name]
    channels = {}
    for p, n, uv in zip(part["Vertices"], part["Normals"], part["UV"]):
        ns, uvs = channels.setdefault(p, (set(), set()))
        ns.add(n)
        uvs.add(uv)
    assert any(len(ns) > 1 for ns, _ in channels.values()), "Lost split normals"
    assert any(len(uvs) > 1 for _, uvs in channels.values()), "Lost UV seams"

for name, parent in (("ReefLineAttachment", "Sprit"), ("SheetLineAttachment", "Sail")):
    actual = data[parent]["RestTransform"] @ Vector(data[name])
    expected = conversion @ bpy.data.objects[name].matrix_world.translation
    assert (actual - expected).length < 3e-6, "Lost parent-inverse transform"


@contextmanager
def edit_object(name):
    obj = bpy.data.objects[name]
    mesh = obj.data if obj.type == "MESH" else None
    if mesh:
        obj.data = mesh.copy()
    parent = obj.parent
    inverse = obj.matrix_parent_inverse.copy()
    location, rotation, scale = (
        obj.location.copy(),
        obj.rotation_euler.copy(),
        obj.scale.copy(),
    )
    modifiers = [(m, m.show_viewport, m.show_render) for m in obj.modifiers]
    try:
        yield obj
    finally:
        if mesh:
            modified = obj.data
            obj.data = mesh
            bpy.data.meshes.remove(modified)
        obj.name = name
        obj.parent = parent
        obj.matrix_parent_inverse = inverse
        obj.location, obj.rotation_euler, obj.scale = location, rotation, scale
        for m in list(obj.modifiers):
            if m not in [record[0] for record in modifiers]:
                obj.modifiers.remove(m)
        for m, viewport, render in modifiers:
            m.show_viewport, m.show_render = viewport, render
        bpy.context.view_layer.update()


def reject(name, edit):
    with edit_object(name) as obj:
        edit(obj)
        bpy.context.view_layer.update()
        try:
            bake()
        except ValueError as error:
            assert name in str(error), str(error)
        else:
            raise AssertionError(f"Accepted invalid {name} edit")
    assert bake() == source, f"Fixture leaked: {name}"


for name in ("Sail", "Sprit", "Snotter", "SpritMountRod", "RotationPlate"):
    reject(name, lambda o: setattr(o, "name", "Missing"))
    reject(name, lambda o: o.data.uv_layers.remove(o.data.uv_layers.active))
    reject(name, lambda o: setattr(o.scale, "x", 0))
    reject(name, lambda o: setattr(o.data.vertices[0].co, "x", float("nan")))
    reject(name, lambda o: o.modifiers.new(name="Unsupported", type="BEVEL"))
    reject(name, lambda o: o.data.vertices.add(count=1))
reject("Sail", lambda o: setattr(o.data.vertices[10].co, "x", 0.1))
reject("Sail", lambda o: o.data.polygons[0].flip())
reject("Sprit", lambda o: [p.flip() for p in o.data.polygons])
reject("Sail", lambda o: setattr(o.data.uv_layers.active.data[0], "uv", (3, 3)))
reject("Sprit", lambda o: setattr(o.modifiers[0], "show_render", False))
reject("Sprit", lambda o: setattr(o.data.polygons[0], "material_index", 99))
reject("SheetLineAttachment", lambda o: setattr(o, "parent", bpy.data.objects["Sprit"]))
reject("ReefLineAttachment", lambda o: setattr(o, "name", "Missing"))
reject("SheetLineAttachment", lambda o: setattr(o.location, "z", o.location.z + 1))

with edit_object("Mast") as obj:
    obj.location = (33, 88, -19)
    obj.scale = (0, 0, 0)
    assert bake() == source, "Reference mast affected the export"

with edit_object("SpritMountRod") as obj:
    obj.location.y += 0.1
    bpy.context.view_layer.update()
    changed = collect()
    assert changed["SnotterAssembly"]["Vertices"] != assembly["Vertices"]
    assert changed["SnotterAssembly"]["RestTransform"] == assembly["RestTransform"]

with edit_object("Snotter") as obj:
    obj.modifiers[0].thickness *= 1.5
    try:
        assert collect()["SnotterAssembly"]["Vertices"] != assembly["Vertices"], (
            "Solidify ignored"
        )
    finally:
        obj.modifiers[0].thickness /= 1.5

# A different cut has different dimensions, resolution and vertex order, using the same worker.
with tempfile.TemporaryDirectory(prefix="mss-spritsail-") as directory:
    output = Path(directory) / "MkB.cs"
    CUTS["B"] = SpritsailCut(
        asset_path=Path(directory) / "MkB.blend",
        source_path=output,
        class_name="LooseFootedSpritsailMkBBakedAsset",
    )
    try:
        with edit_object("Sail") as sail, edit_object("SheetLineAttachment") as sheet:
            vertices = [
                (0, -10 + c * 2.5, c * 0.5 + r * (12 + c * 1.5) / 3)
                for c in range(5)
                for r in range(4)
            ]
            faces = [
                (c * 4 + r, c * 4 + r + 1, (c + 1) * 4 + r + 1, (c + 1) * 4 + r)
                for c in range(4)
                for r in range(3)
            ]
            sail.data.clear_geometry()
            sail.data.from_pydata(vertices, [], faces)
            uv = sail.data.uv_layers.get("ClothUV") or sail.data.uv_layers.new(
                name="ClothUV"
            )
            for loop in sail.data.loops:
                point = sail.data.vertices[loop.vertex_index].co
                uv.data[loop.index].uv = (
                    (point.y + 10) * 0.045 + 0.1,
                    point.z * 0.045 + 0.05,
                )
            sheet.matrix_world = Matrix.Translation(
                sail.matrix_world @ Vector(vertices[0])
            )
            bpy.context.view_layer.update()
            assert len(collect()["Sail"]["Vertices"]) == 20
            second_source = bake("B")
            assert "LooseFootedSpritsailMkBBakedAsset" in second_source
            assert second_source == bake("B")
    finally:
        del CUTS["B"]
    args = sys.argv
    try:
        for stale in (None, "stale"):
            if stale is not None:
                output.write_text(stale)
            sys.argv = ["blender", "--", "--output", str(output), "--check"]
            try:
                tool["main"]()
            except RuntimeError:
                pass
            else:
                raise AssertionError("Accepted stale output")
            assert not output.exists() if stale is None else output.read_text() == stale
        sys.argv = ["blender", "--", "--output", str(output)]
        tool["main"]()
        assert output.read_text() == source
        sys.argv.append("--check")
        tool["main"]()
    finally:
        sys.argv = args

assert hashlib.sha256(asset.read_bytes()).digest() == before, "Authoring file changed"
assert bake() == source
print(
    "PASS: spritsail cuts, geometry, rigid assembly, materials, transforms, anchors, validation and read-only checks."
)
