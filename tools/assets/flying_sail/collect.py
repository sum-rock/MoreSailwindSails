"""Collect the validated Flying Sail mesh from Blender."""

import math

import bpy
from mathutils import Matrix

COLUMNS = 24
ROWS = 32


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(f"Incompatible sail asset: {message}")


def collect() -> dict:
    object_name = "Fishermans Flying Sail"
    require(bpy.app.background, "run in background Blender")
    require(
        bpy.context.scene.unit_settings.scale_length == 1,
        "author at one metre per Blender unit",
    )
    require(bool(bpy.data.filepath), "open the saved .blend file before baking")
    obj = bpy.data.objects.get(object_name)
    require(
        obj is not None and obj.type == "MESH", f"missing named mesh: {object_name}"
    )
    require(obj.matrix_world == Matrix.Identity(4), "apply object transforms")
    require(
        not obj.modifiers and not obj.data.shape_keys,
        "modifiers and shape keys are unsupported",
    )
    mesh = obj.data
    require(
        len(mesh.vertices) == 825 and len(mesh.polygons) == 768,
        "retain the 24 x 32 quad grid",
    )
    uv = mesh.uv_layers.get("ClothUV")
    require(uv is not None, "missing ClothUV layer")
    for vertex in mesh.vertices:
        require(all(math.isfinite(n) for n in vertex.co), "non-finite vertex")

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
        require(
            canonical in expected_faces,
            "changed connectivity, winding or duplicate face",
        )
        expected_faces.remove(canonical)
    require(not expected_faces, "missing grid face")
    require(
        len(mesh.edges) == 2 * COLUMNS * ROWS + COLUMNS + ROWS, "loose or missing edges"
    )

    vertex_uvs = [None] * len(mesh.vertices)
    for loop in mesh.loops:
        value = uv.data[loop.index].uv
        require(all(math.isfinite(n) for n in value), "non-finite UV")
        previous = vertex_uvs[loop.vertex_index]
        require(
            previous is None or (previous - value).length < 0.0000001,
            "UV seams are unsupported",
        )
        vertex_uvs[loop.vertex_index] = value.copy()

    # Normalize by the stored span so the luff/aft endpoints remain exactly -1/0.
    # Blender (X,Y,Z) -> sail (Z,-Y,X-width). Export no material or native asset.
    span = mesh.vertices[COLUMNS].co.x
    require(span > 0, "sail span must be positive")
    vertices = [
        (p.co.z / span, -p.co.y / span, p.co.x / span - 1) for p in mesh.vertices
    ]

    return {
        "Vertices": vertices,
        "UV": [tuple(v) for v in vertex_uvs],
        "Triangles": triangles,
    }
