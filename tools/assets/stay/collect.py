"""Bake the authored stay into reusable C# rope/collar data without saving Blender."""

import math
from pathlib import Path
import sys

import bmesh
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[3]
SOURCE_PATH = ROOT / "src/Stays/FishermansStay/FishermansStayBakedMesh.g.cs"
OBJECT_NAMES = ("Rope", "ForestayCollar", "AftstayCollar")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def finite(values) -> bool:
    return all(math.isfinite(value) for value in values)


def extract(*, name: str, rope: bool) -> dict:
    """Evaluate in object space, retaining scale but discarding the assembly pose."""
    obj = bpy.data.objects.get(name)
    require(obj is not None and obj.type == "MESH", f"Missing mesh object: {name}")
    require(obj.parent is None, f"{name}: parent transforms are unsupported")
    require(not obj.constraints and not obj.animation_data, f"{name}: animated object")
    require(obj.data.shape_keys is None, f"{name}: shape keys are unsupported")
    require(
        finite(obj.scale) and all(value > 0 for value in obj.scale),
        f"{name}: scale must be finite and positive",
    )
    require(
        all(finite(row) for row in obj.matrix_world), f"{name}: non-finite transform"
    )
    require(
        all(finite(vertex.co) for vertex in obj.data.vertices),
        f"{name}: non-finite source vertex",
    )
    require(
        all(
            not rope
            and modifier.type == "SOLIDIFY"
            and modifier.show_viewport
            and modifier.show_render
            for modifier in obj.modifiers
        ),
        f"{name}: only enabled collar Solidify modifiers are supported",
    )
    evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = evaluated.to_mesh(
        preserve_all_data_layers=True, depsgraph=bpy.context.evaluated_depsgraph_get()
    )
    try:
        require(
            len(mesh.vertices) > 0 and len(mesh.polygons) > 0, f"{name}: empty mesh"
        )
        require(mesh.uv_layers.active is not None, f"{name}: missing UV map")
        require(all(finite(v.co) for v in mesh.vertices), f"{name}: non-finite vertex")
        topology = bmesh.new()
        try:
            topology.from_mesh(mesh)
            require(
                all(edge.is_manifold and edge.is_contiguous for edge in topology.edges)
                and all(vertex.link_faces for vertex in topology.verts),
                f"{name}: mesh must be closed with consistent winding and no loose vertices",
            )
            require(topology.calc_volume(signed=True) > 0, f"{name}: inward winding")
        finally:
            topology.free()

        points = [
            Vector(tuple(v.co[i] * obj.scale[i] for i in range(3)))
            for v in mesh.vertices
        ]
        low = Vector(tuple(min(p[i] for p in points) for i in range(3)))
        high = Vector(tuple(max(p[i] for p in points) for i in range(3)))
        center = (low + high) / 2
        size = high - low
        require(all(value > 1e-6 for value in size), f"{name}: degenerate dimensions")

        # Both maps reflect handedness. Reverse triangle winding below as well.
        # Authored rope X -> stay Z; collar Z remains its mast axis.
        def direction(value):
            return Vector(
                (value.y, -value.z, value.x) if rope else (value.x, -value.y, value.z)
            )

        def position(value):
            result = direction(value - center)
            if rope:
                result.z -= size.x / 2
            return result

        canonical = [position(p) for p in points]
        radii = [math.hypot(p.x, p.y) for p in canonical]
        if rope:
            require(
                size.x >= 0.25,
                f"{name}: rope must run along local X and be at least 0.25 m long",
            )
            require(
                max(radii) - min(radii) < 1e-5,
                f"{name}: expected a straight circular rope",
            )
            dimensions = {"RopeLength": size.x, "RopeRadius": max(radii)}
        else:
            inner, outer = min(radii), max(radii)
            require(
                inner > 0 and outer - inner > 1e-6,
                f"{name}: missing collar wall thickness",
            )
            require(
                all(min(abs(r - inner), abs(r - outer)) < 1e-5 for r in radii)
                and all(abs(abs(p.z) - size.z / 2) < 1e-5 for p in canonical),
                f"{name}: expected a circular band around local Z",
            )
            dimensions = {
                "CollarInnerRadius": inner,
                "CollarOuterRadius": outer,
                "CollarHalfHeight": size.z / 2,
            }

        mesh.calc_loop_triangles()
        vertices, normals, uv, triangles = [], [], [], []
        indices = {}
        for triangle in mesh.loop_triangles:
            # Preserve UV seams and flat/split normals using complete corner identity.
            for loop_index in reversed(triangle.loops):
                vertex_index = mesh.loops[loop_index].vertex_index
                normal = mesh.corner_normals[loop_index].vector
                normal = direction(
                    Vector(tuple(normal[i] / obj.scale[i] for i in range(3)))
                ).normalized()
                texcoord = mesh.uv_layers.active.data[loop_index].uv
                require(
                    finite(normal) and normal.length > 0.99, f"{name}: invalid normal"
                )
                require(finite(texcoord), f"{name}: non-finite UV")
                point = canonical[vertex_index]
                key = (tuple(point), tuple(normal), tuple(texcoord))
                if key not in indices:
                    indices[key] = len(vertices)
                    vertices.append(tuple(point))
                    normals.append(tuple(normal))
                    uv.append(tuple(texcoord))
                triangles.append(indices[key])
            a, b, c = [Vector(vertices[i]) for i in triangles[-3:]]
            cross = (b - a).cross(c - a)
            require(cross.length > 1e-12, f"{name}: degenerate triangle")
            require(
                all(cross.dot(Vector(normals[i])) > 0 for i in triangles[-3:]),
                f"{name}: normals disagree with winding",
            )
        return {
            "Vertices": vertices,
            "Normals": normals,
            "UV": uv,
            "Triangles": triangles,
            "dimensions": dimensions,
        }
    finally:
        evaluated.to_mesh_clear()


def collect():
    require(
        bpy.context.scene.unit_settings.scale_length == 1,
        "Author at one metre per Blender unit",
    )
    bpy.context.view_layer.update()
    data = {
        "Rope": extract(name=OBJECT_NAMES[0], rope=True),
        "Collar": extract(name=OBJECT_NAMES[1], rope=False),
        "AftCollar": extract(name=OBJECT_NAMES[2], rope=False),
    }

    def canonical(mesh):
        return [
            (
                field,
                [
                    v
                    if field == "Triangles"
                    else tuple("0" if abs(n) < 1e-9 else format(n, ".9g") for n in v)
                    for v in mesh[field]
                ],
            )
            for field in ("Vertices", "Normals", "UV", "Triangles")
        ]

    require(
        canonical(data["Collar"]) == canonical(data["AftCollar"])
        and data["Collar"]["dimensions"] == data["AftCollar"]["dimensions"],
        "Fore and aft collars must have matching canonical geometry, normals and UVs",
    )
    del data["AftCollar"]
    return data
