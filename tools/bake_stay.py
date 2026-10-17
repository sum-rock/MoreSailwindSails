"""Bake the authored stay into reusable C# rope/collar data without saving Blender."""

import argparse
import math
from pathlib import Path
import sys

import bmesh
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
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


def number(value: float) -> str:
    return ("0" if abs(value) < 1e-9 else format(value, ".9g")) + "f"


def mesh_lines(*, method: str, data: dict) -> list[str]:
    lines = [
        f"        internal static FishermansStayMeshData {method}() =>",
        "            new FishermansStayMeshData",
        "            {",
    ]
    for name, kind in (
        ("Vertices", "Vector3"),
        ("Normals", "Vector3"),
        ("UV", "Vector2"),
        ("Triangles", "int"),
    ):
        lines.extend((f"                {name} = new {kind}[]", "                {"))
        for value in data[name]:
            text = (
                str(value)
                if kind == "int"
                else f"new {kind}({', '.join(number(n) for n in value)})"
            )
            lines.append(f"                    {text},")
        lines.append("                },")
    lines.extend(("            };", ""))
    return lines


def bake() -> str:
    require(
        bpy.context.scene.unit_settings.scale_length == 1,
        "Author at one metre per Blender unit",
    )
    bpy.context.view_layer.update()
    rope = extract(name=OBJECT_NAMES[0], rope=True)
    collar = extract(name=OBJECT_NAMES[1], rope=False)
    aft = extract(name=OBJECT_NAMES[2], rope=False)
    require(
        mesh_lines(method="CreateCollar", data=collar)
        == mesh_lines(method="CreateCollar", data=aft)
        and collar["dimensions"] == aft["dimensions"],
        "Fore and aft collars must have matching canonical geometry, normals and UVs",
    )
    lines = [
        "// <auto-generated />",
        "// Baked by tools/bake_stay.py from assets/stays/stay.blend.",
        "// Supplies independent rope/collar mesh arrays and measured reference dimensions in metres.",
        "using UnityEngine;",
        "",
        "namespace MoreSailwindSails.Stays.FishermansStay",
        "{",
        "    internal static class FishermansStayBakedMesh",
        "    {",
    ]
    for name, value in (rope["dimensions"] | collar["dimensions"]).items():
        lines.append(f"        internal const float {name} = {number(value)};")
    lines.append("")
    lines.extend(mesh_lines(method="CreateRope", data=rope))
    lines.extend(mesh_lines(method="CreateCollar", data=collar))
    lines.pop()
    lines.extend(("    }", "}", ""))
    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=SOURCE_PATH)
    parser.add_argument(
        "--check", action="store_true", help="Fail if output differs; write nothing."
    )
    options = parser.parse_args(
        sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    )
    source = bake()
    if options.check:
        if (
            not options.output.is_file()
            or options.output.read_text(encoding="utf-8") != source
        ):
            raise RuntimeError(f"Stale baked mesh: regenerate {options.output}")
        print(f"PASS: baked stay matches {options.output}")
    else:
        options.output.parent.mkdir(parents=True, exist_ok=True)
        options.output.write_text(source, encoding="utf-8")
        print(f"Baked rope and reusable collar -> {options.output}")


if __name__ == "__main__":
    main()
