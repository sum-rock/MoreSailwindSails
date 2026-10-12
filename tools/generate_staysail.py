"""Generate an editable, flat staysail cut and planar UVs in background Blender."""

import argparse
import math
from pathlib import Path
import sys

import bpy
from mathutils import Quaternion, Vector


sys.path.insert(0, str(Path(__file__).resolve().parent))
from staysail_authoring import COLUMNS, CUTS, ROWS, UV_MARGIN, WIDTH, asset_path, dimensions


def generate(output: Path, cut: str = "A") -> None:
    # Reset only a disposable background session, never an open editing session.
    if not bpy.app.background:
        raise RuntimeError("Run this generator with blender --background.")
    bpy.ops.wm.read_factory_settings(use_empty=True)

    head_rise, foot_fore, foot_aft = dimensions(cut=cut)
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
            vertices.append((WIDTH * u, 0.0, top_z * (1.0 - v) + bottom_z * v))
            if row < ROWS and column < COLUMNS:
                i = row * stride + column
                # Face Blender's front view (normal -Y); retain editable quads.
                faces.append((i, i + stride, i + stride + 1, i + 1))

    mesh = bpy.data.meshes.new(name=f"Fishermans Staysail Mk.{cut} Cloth")
    mesh.from_pydata(vertices=vertices, edges=[], faces=faces)
    mesh.update()

    # A single uniform scale preserves the cut's proportions in a square texture.
    # Center the island inside 0..1 without stretching it to fill a rectangle.
    uv_scale = (1.0 - 2.0 * UV_MARGIN) / max(WIDTH, height)
    uv_left = (1.0 - WIDTH * uv_scale) / 2.0
    uv_bottom = (1.0 - height * uv_scale) / 2.0
    uv_layer = mesh.uv_layers.new(name="ClothUV")
    for loop in mesh.loops:
        x, _, z = vertices[loop.vertex_index]
        uv_layer.data[loop.index].uv = (
            uv_left + x * uv_scale,
            uv_bottom + (z - bottom) * uv_scale,
        )

    sail = bpy.data.objects.new(name=f"Fishermans Staysail Mk.{cut}", object_data=mesh)
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
                view.view_location = Vector((WIDTH / 2.0, 0.0, (head_rise + bottom) / 2.0))
                view.view_distance = max(WIDTH, height) * 1.5

    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(output))
    print(f"Saved {output}: {len(mesh.vertices)} vertices, {len(mesh.polygons)} quads.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cut", choices=CUTS, default="A")
    parser.add_argument("--output", type=Path)
    arguments = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    options = parser.parse_args(arguments)
    generate(output=(options.output or asset_path(cut=options.cut)).resolve(), cut=options.cut)
