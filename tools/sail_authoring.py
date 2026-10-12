"""Shared flat sail grid generation, validation and deterministic C# mesh export."""

import math
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector

COLUMNS = 24
ROWS = 32
UV_MARGIN = 0.05


def generate(*, output: Path, width: float, heights: tuple, object_name: str) -> None:
    # Reset only a disposable background session, never an open editing session.
    if not bpy.app.background:
        raise RuntimeError("Run this generator with blender --background.")
    bpy.ops.wm.read_factory_settings(use_empty=True)

    head_rise, foot_fore, foot_aft = heights
    bottom = min(foot_fore, foot_aft)
    height = head_rise - bottom
    vertices = []
    faces = []
    stride = COLUMNS + 1

    # X runs fore to aft, Z points up, and the forward head is the origin.
    # Row-major order retains the current procedural mesh's grid correspondence.
    for row in range(ROWS + 1):
        v = row / ROWS
        for column in range(COLUMNS + 1):
            u = column / COLUMNS
            top_z = head_rise * u
            bottom_z = foot_fore * (1.0 - u) + foot_aft * u
            vertices.append((width * u, 0.0, top_z * (1.0 - v) + bottom_z * v))
            if row < ROWS and column < COLUMNS:
                i = row * stride + column
                # Face Blender's front view (normal -Y); retain editable quads.
                faces.append((i, i + stride, i + stride + 1, i + 1))

    mesh = bpy.data.meshes.new(name=object_name + " Cloth")
    mesh.from_pydata(vertices=vertices, edges=[], faces=faces)
    mesh.update()

    # A single uniform scale preserves the cut's proportions in a square texture.
    # Center the island inside 0..1 without stretching it to fill a rectangle.
    uv_scale = (1.0 - 2.0 * UV_MARGIN) / max(width, height)
    uv_left = (1.0 - width * uv_scale) / 2.0
    uv_bottom = (1.0 - height * uv_scale) / 2.0
    uv_layer = mesh.uv_layers.new(name="ClothUV")
    for loop in mesh.loops:
        x, _, z = vertices[loop.vertex_index]
        uv_layer.data[loop.index].uv = (
            uv_left + x * uv_scale,
            uv_bottom + (z - bottom) * uv_scale,
        )

    sail = bpy.data.objects.new(name=object_name, object_data=mesh)
    bpy.context.scene.collection.objects.link(sail)
    material = bpy.data.materials.new(name="Neutral Cloth")
    material.diffuse_color = (0.8, 0.78, 0.72, 1.0)
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = material.diffuse_color
    shader.inputs["Roughness"].default_value = 0.85
    mesh.materials.append(material)

    bpy.context.view_layer.objects.active = sail
    sail.select_set(state=True)
    sail.show_wire = True
    sail.show_all_edges = True
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0

    # Open ready for inspection, centered on the outline with the grid visible.
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                view = area.spaces.active.region_3d
                view.view_rotation = Quaternion((1.0, 0.0, 0.0), math.pi / 2.0)
                view.view_perspective = "ORTHO"
                view.view_location = Vector((width / 2.0, 0.0, (head_rise + bottom) / 2.0))
                view.view_distance = max(width, height) * 1.5

    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(output))
    print(f"Saved {output}: {len(mesh.vertices)} vertices, {len(mesh.polygons)} quads.")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(f"Incompatible sail asset: {message}")


def bake(*, width: float, heights: tuple, object_name: str, namespace: str, class_name: str, data_type: str, source_comment: str) -> str:
    require(bpy.app.background, "run in background Blender")
    require(bool(bpy.data.filepath), "open the saved .blend file before baking")
    obj = bpy.data.objects.get(object_name)
    require(obj is not None and obj.type == "MESH", f"missing named mesh: {object_name}")
    require(obj.matrix_world == Matrix.Identity(4), "apply object transforms")
    require(not obj.modifiers and not obj.data.shape_keys, "modifiers and shape keys are unsupported")
    mesh = obj.data
    require(len(mesh.vertices) == 825 and len(mesh.polygons) == 768, "retain the 24 x 32 quad grid")
    uv = mesh.uv_layers.get("ClothUV")
    require(uv is not None, "missing ClothUV layer")
    rise, foot_fore, foot_aft = heights
    bottom = min(foot_fore, foot_aft)
    height = rise - bottom
    uv_scale = (1 - 2 * UV_MARGIN) / max(width, height)
    uv_bottom = (1 - height * uv_scale) / 2
    uv_left = (1 - width * uv_scale) / 2

    # Validate correspondence, not just counts: the runtime skinning uses this grid.
    for row in range(ROWS + 1):
        v = row / ROWS
        for col in range(COLUMNS + 1):
            u = col / COLUMNS
            point = mesh.vertices[row * (COLUMNS + 1) + col].co
            expected = Vector((width * u, 0, rise * u * (1 - v) + (foot_fore * (1 - u) + foot_aft * u) * v))
            require(all(math.isfinite(n) for n in point), "non-finite vertex")
            require((point - expected).length < 0.00001, "retain the nominal flat cut and vertex order")

    expected_faces = set()
    triangles = []
    for row in range(ROWS):
        for col in range(COLUMNS):
            i = row * (COLUMNS + 1) + col
            below = i + COLUMNS + 1
            expected_faces.add((i, below, below + 1, i + 1))
            # Same diagonal and winding as the current runtime grid.
            triangles.extend((i, below, i + 1, i + 1, below, below + 1))
    for face in mesh.polygons:
        indices = tuple(face.vertices)
        require(len(indices) == 4, "retain quad faces")
        start = indices.index(min(indices))
        canonical = indices[start:] + indices[:start]
        require(canonical in expected_faces, "changed connectivity, winding or duplicate face")
        expected_faces.remove(canonical)
    require(not expected_faces, "missing grid face")
    require(len(mesh.edges) == 2 * COLUMNS * ROWS + COLUMNS + ROWS, "loose or missing edges")

    vertex_uvs = [None] * len(mesh.vertices)
    for loop in mesh.loops:
        point = mesh.vertices[loop.vertex_index].co
        value = uv.data[loop.index].uv
        expected = Vector((uv_left + point.x * uv_scale, uv_bottom + (point.z - bottom) * uv_scale))
        require(all(math.isfinite(n) for n in value), "non-finite UV")
        require((value - expected).length < 0.000002, "retain continuous uniform planar UVs with a 5% margin")
        previous = vertex_uvs[loop.vertex_index]
        require(previous is None or (previous - value).length < 0.0000001, "UV seams are unsupported")
        vertex_uvs[loop.vertex_index] = value.copy()

    # Normalize by the stored span so the luff/aft endpoints remain exactly -1/0.
    # Blender (X,Y,Z) -> sail (Z,-Y,X-width). Export no material or native asset.
    span = mesh.vertices[COLUMNS].co.x
    vertices = [(p.co.z / span, -p.co.y / span, p.co.x / span - 1) for p in mesh.vertices]

    def number(value: float) -> str:
        return ("0" if value == 0 else format(value, ".9g")) + "f"

    def vector(values, kind: str) -> str:
        return f"new {kind}({', '.join(number(n) for n in values)}),"

    lines = [
        "// <auto-generated />",
        source_comment,
        "// Supplies fresh normalized positions, planar UVs and fixed topology for each cut.",
        "using UnityEngine;",
        "",
        f"namespace {namespace}",
        "{",
        f"    internal static class {class_name}",
        "    {",
        f"        internal static {data_type} Create() =>",
        f"            new {data_type}",
        "            {",
    ]
    for name, kind, values in (
        ("Vertices", "Vector3", vertices),
        ("UV", "Vector2", vertex_uvs),
        ("Triangles", "int", triangles),
    ):
        lines.extend((f"                {name} = new {kind}[]", "                {"))
        lines.extend("                    " + (f"{value}," if kind == "int" else vector(value, kind)) for value in values)
        lines.append("                },")
    lines.extend(("            };", "    }", "}", ""))
    return "\n".join(lines)


def write_bake(*, source: str, output: Path, check: bool) -> None:
    if check:
        if not output.is_file() or output.read_text(encoding="utf-8") != source:
            raise RuntimeError(f"Stale baked mesh: regenerate {output}")
        print(f"PASS: baked source matches the open asset: {output}")
    else:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(source, encoding="utf-8")
        print(f"Baked: 825 vertices, 1536 triangles -> {output}")
