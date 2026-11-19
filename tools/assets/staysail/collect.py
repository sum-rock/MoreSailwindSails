"""Collect validated staysail grids and sequential animation samples from Blender."""

import math

import bpy
from mathutils import Matrix
from tools.assets.staysail.authoring import SHEET_FRAMES, REEF_FRAMES, EASED_FRAMES

COLUMNS = 24
ROWS = 32


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(f"Incompatible sail asset: {message}")


def collect_mesh(
    *,
    object_name: str,
    animated: bool = False,
) -> dict:
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
        (not obj.modifiers or animated) and not obj.data.shape_keys,
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


def samples():
    obj = bpy.data.objects["Fishermans Staysail Mk.B"]
    require(
        all(
            m.type in ("HOOK", "CLOTH", "COLLISION") and m.show_viewport
            for m in obj.modifiers
        ),
        "expected enabled Hook/Cloth/Collision modifiers",
    )
    topology = [tuple(p.vertices) for p in obj.data.polygons]
    span = obj.data.vertices[24].co.x
    frames = set(SHEET_FRAMES + REEF_FRAMES + EASED_FRAMES)
    result = {}
    # Reset to before the simulation start; force evaluation on every frame.
    bpy.context.scene.frame_set(0)
    for frame in range(1, max(frames) + 1):
        bpy.context.scene.frame_set(frame)
        evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = evaluated.to_mesh()
        try:
            require(
                len(mesh.vertices) == 825
                and [tuple(p.vertices) for p in mesh.polygons] == topology,
                f"topology changed at frame {frame}",
            )
            if frame in frames:
                points = [
                    (v.co.z / span, -v.co.y / span, v.co.x / span - 1)
                    for v in mesh.vertices
                ]
                require(
                    all(math.isfinite(n) for p in points for n in p),
                    f"non-finite frame {frame}",
                )
                result[frame] = points
        finally:
            evaluated.to_mesh_clear()
    return result


def collect_cut(*, cut="A"):
    animated = cut == "B"
    data = collect_mesh(
        object_name=f"Fishermans Staysail Mk.{cut}",
        animated=animated,
    )
    data["Poses"] = []
    if animated:
        poses = samples()
        for reef, sheet in ((200, 60), (500, 120)):
            require(
                max(
                    abs(a - b)
                    for i in (0, 24, 800, 824)
                    for a, b in zip(poses[reef][i], poses[sheet][i])
                )
                < 1e-4,
                "deployed reef and sheet attachments disagree",
            )
            poses[reef] = poses[sheet]
        data["Poses"] = [(frame, points) for frame, points in poses.items()]
    return data
