"""Bake a spritsail cut, rigid hardware and local attachment points without saving Blender."""

import argparse
import math
from pathlib import Path
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from loose_foot_spritsail_authoring import CUTS, get_cut

# Reflect handedness while making Z-up authoring data Y-up in Unity.
CONVERSION = Matrix(((1, 0, 0, 0), (0, 0, 1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))
PARTS = {
    "Sail": ("Sail",),
    "Sprit": ("Sprit",),
    "SnotterAssembly": ("Snotter", "SpritMountRod"),
    "RotationPlate": ("RotationPlate",),
}
MATERIALS = ("Cloth", "Metal", "DarkWood")


def require(condition, message):
    if not condition:
        raise ValueError(f"Incompatible spritsail asset: {message}")


def finite(values):
    return all(math.isfinite(value) for value in values)


def object_named(name, kind):
    obj = bpy.data.objects.get(name)
    require(obj is not None and obj.type == kind, f"{name}: missing {kind} object")
    ancestor = obj
    while ancestor is not None:
        require(
            not ancestor.constraints and not ancestor.animation_data,
            f"{name}: constraints and animation are unsupported",
        )
        ancestor = ancestor.parent
    require(
        all(finite(row) for row in obj.matrix_world)
        and obj.matrix_world.to_3x3().determinant() > 1e-10,
        f"{name}: transform must be finite, invertible and unreflected",
    )
    if kind == "MESH":
        require(not obj.data.shape_keys, f"{name}: shape keys are unsupported")
        require(
            all(finite(v.co) for v in obj.data.vertices),
            f"{name}: non-finite source vertex",
        )
        require(
            all(
                name != "Sail"
                and m.type in ("SOLIDIFY", "DECIMATE")
                and m.show_viewport
                and m.show_render
                for m in obj.modifiers
            ),
            f"{name}: only enabled hardware Solidify/Decimate modifiers are supported",
        )
    return obj


def cloth_grid(obj):
    """Validate a flat bilinear quad grid without depending on vertex order or resolution."""
    mesh = obj.data
    require(len(mesh.vertices) >= 4, "Sail: empty cloth")
    bm = bmesh.new()
    try:
        bm.from_mesh(mesh)
        require(
            all(v.link_faces for v in bm.verts)
            and all(
                e.is_boundary or (e.is_manifold and e.is_contiguous) for e in bm.edges
            )
            and all(len(f.verts) == 4 and f.calc_area() > 1e-10 for f in bm.faces),
            "Sail: expected consistently wound quads without loose geometry",
        )
    finally:
        bm.free()
    points = [v.co.copy() for v in mesh.vertices]
    require(
        max(p.x for p in points) - min(p.x for p in points) < 1e-5,
        "Sail: cloth must be flat in local Y/Z",
    )
    columns = {}
    for i, point in enumerate(points):
        columns.setdefault(round(point.y, 5), []).append(i)
    columns = [
        sorted(ids, key=lambda i: points[i].z) for _, ids in sorted(columns.items())
    ]
    require(
        len(columns) >= 2 and len(columns[0]) >= 2, "Sail: missing grid rows/columns"
    )
    rows = len(columns[0])
    require(all(len(ids) == rows for ids in columns), "Sail: incomplete grid")
    clew, peak = columns[0][0], columns[0][-1]
    tack, throat = columns[-1][0], columns[-1][-1]
    width = points[tack].y - points[clew].y
    require(width > 1e-5, "Sail: zero width")
    require(points[peak].z - points[clew].z > 1e-5, "Sail: collapsed outer edge")
    fractions = [
        (points[i].z - points[clew].z) / (points[peak].z - points[clew].z)
        for i in columns[0]
    ]
    for ids in columns:
        u = (points[ids[0]].y - points[clew].y) / width
        low = points[clew].lerp(points[tack], u)
        high = points[peak].lerp(points[throat], u)
        require(high.z - low.z > 1e-5, "Sail: collapsed or inverted column")
        for i, fraction in zip(ids, fractions):
            require(
                (points[i] - low.lerp(high, fraction)).length < 2e-5,
                "Sail: irregular grid or non-linear outline",
            )
    expected = {
        frozenset(
            (columns[c][r], columns[c + 1][r], columns[c + 1][r + 1], columns[c][r + 1])
        )
        for c in range(len(columns) - 1)
        for r in range(rows - 1)
    }
    for face in mesh.polygons:
        key = frozenset(face.vertices)
        require(key in expected, "Sail: changed grid connectivity or duplicate face")
        expected.remove(key)
    require(not expected, "Sail: missing quad")
    uv = mesh.uv_layers.get("ClothUV")
    require(uv is not None, "Sail: missing ClothUV")
    values = [None] * len(points)
    for loop in mesh.loops:
        value = uv.data[loop.index].uv.copy()
        require(
            finite(value) and all(-1e-6 <= n <= 1 + 1e-6 for n in value),
            "Sail: UV outside texture square or non-finite",
        )
        old = values[loop.vertex_index]
        require(old is None or (old - value).length < 1e-6, "Sail: UV seam")
        values[loop.vertex_index] = value
    # One similarity transform covers the whole plane, allowing UV rotation/reflection.
    height = points[peak].z - points[clew].z
    basis_z = (values[peak] - values[clew]) / height
    basis_y = (
        values[tack] - values[clew] - basis_z * (points[tack].z - points[clew].z)
    ) / width
    require(
        basis_y.length > 1e-8
        and abs(basis_y.length - basis_z.length) < basis_y.length * 1e-4
        and abs(basis_y.dot(basis_z)) < basis_y.length * basis_z.length * 1e-4,
        "Sail: UV proportions are distorted",
    )
    for point, value in zip(points, values):
        expected_uv = (
            values[clew]
            + basis_y * (point.y - points[clew].y)
            + basis_z * (point.z - points[clew].z)
        )
        require((value - expected_uv).length < 2e-6, "Sail: non-proportional planar UV")
    return {"Tack": tack, "Throat": throat, "Peak": peak, "Clew": clew}


def extract_part(name, sources):
    """Evaluate sources and combine them around the first source's unchanged origin."""
    objects = [object_named(source, "MESH") for source in sources]
    root = objects[0]
    cloth = name == "Sail"
    vertices = (
        [tuple(CONVERSION.to_3x3() @ v.co) for v in root.data.vertices] if cloth else []
    )
    normals = [None] * len(vertices)
    uvs = [None] * len(vertices)
    by_material = {}
    indices = {}
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in objects:
        relative = (
            CONVERSION
            if obj == root
            else CONVERSION @ root.matrix_world.inverted() @ obj.matrix_world
        )
        normal_matrix = relative.to_3x3().inverted().transposed()
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh(preserve_all_data_layers=True, depsgraph=depsgraph)
        try:
            require(len(mesh.polygons) > 0, f"{obj.name}: empty mesh")
            topology = bmesh.new()
            try:
                topology.from_mesh(mesh)
                require(
                    all(v.link_faces for v in topology.verts),
                    f"{obj.name}: loose vertex",
                )
                if not cloth:
                    require(
                        all(e.is_manifold and e.is_contiguous for e in topology.edges),
                        f"{obj.name}: hardware must be closed with consistent winding",
                    )
                    # Joined hardware can contain disconnected closed components.
                    pending = set(topology.faces)
                    while pending:
                        stack = [next(iter(pending))]
                        component = set()
                        while stack:
                            face = stack.pop()
                            if face not in pending:
                                continue
                            pending.remove(face)
                            component.add(face)
                            stack.extend(
                                f
                                for e in face.edges
                                for f in e.link_faces
                                if f in pending
                            )
                        volume = sum(
                            f.verts[0].co.dot(f.verts[i].co.cross(f.verts[i + 1].co))
                            / 6
                            for f in component
                            for i in range(1, len(f.verts) - 1)
                        )
                        require(
                            volume > 1e-12,
                            f"{obj.name}: inward or zero-volume hardware component",
                        )
            finally:
                topology.free()
            layer = mesh.uv_layers.get("ClothUV") if cloth else mesh.uv_layers.active
            require(layer is not None, f"{obj.name}: missing UV map")
            mesh.calc_loop_triangles()
            for triangle in mesh.loop_triangles:
                if cloth:
                    material = "Cloth"
                else:
                    slot = triangle.material_index
                    require(
                        slot < len(obj.material_slots)
                        and obj.material_slots[slot].material is not None,
                        f"{obj.name}: missing material assignment",
                    )
                    material = obj.material_slots[slot].material.name
                    require(
                        material in MATERIALS[1:],
                        f"{obj.name}: unsupported material {material}",
                    )
                triangles = by_material.setdefault(material, [])
                for li in reversed(triangle.loops):
                    vi = mesh.loops[li].vertex_index
                    point = relative @ mesh.vertices[vi].co
                    normal = (
                        normal_matrix @ mesh.corner_normals[li].vector
                    ).normalized()
                    uv = layer.data[li].uv
                    require(
                        finite(point)
                        and finite(normal)
                        and normal.length > 0.99
                        and finite(uv),
                        f"{obj.name}: invalid evaluated vertex/normal/UV",
                    )
                    if cloth:
                        index = vi
                        normals[index], uvs[index] = tuple(normal), tuple(uv)
                    else:
                        key = (tuple(point), tuple(normal), tuple(uv))
                        if key not in indices:
                            indices[key] = len(vertices)
                            vertices.append(tuple(point))
                            normals.append(tuple(normal))
                            uvs.append(tuple(uv))
                        index = indices[key]
                    triangles.append(index)
                a, b, c = [Vector(vertices[i]) for i in triangles[-3:]]
                cross = (b - a).cross(c - a)
                require(
                    cross.length > 1e-10, f"{obj.name}: degenerate evaluated triangle"
                )
                require(
                    all(cross.dot(Vector(normals[i])) > 0 for i in triangles[-3:]),
                    f"{obj.name}: normals disagree with winding",
                )
        finally:
            evaluated.to_mesh_clear()
    materials = [m for m in MATERIALS if m in by_material]
    return {
        "RestTransform": CONVERSION @ root.matrix_world @ CONVERSION,
        "BoundsMin": tuple(min(p[a] for p in vertices) for a in range(3)),
        "BoundsMax": tuple(max(p[a] for p in vertices) for a in range(3)),
        "Vertices": vertices,
        "Normals": normals,
        "UV": uvs,
        "Materials": materials,
        "SubmeshTriangles": [by_material[m] for m in materials],
    }


def attachment(name, parent):
    obj = object_named(name, "EMPTY")
    require(
        obj.parent == bpy.data.objects[parent], f"{name}: must be parented to {parent}"
    )
    # matrix_local / location alone lose the Keep Transform parent-inverse offset.
    return tuple(
        CONVERSION @ (obj.parent.matrix_world.inverted() @ obj.matrix_world.translation)
    )


def collect():
    require(
        bpy.app.background and bool(bpy.data.filepath),
        "open the asset in background Blender",
    )
    if bpy.context.object and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    require(
        bpy.context.scene.unit_settings.scale_length == 1,
        "author at one metre per Blender unit",
    )
    bpy.context.view_layer.update()
    sail = object_named("Sail", "MESH")
    corners = cloth_grid(sail)
    data = {name: extract_part(name, sources) for name, sources in PARTS.items()}
    data.update(corners)
    data["ReefLineAttachment"] = attachment("ReefLineAttachment", "Sprit")
    data["SheetLineAttachment"] = attachment("SheetLineAttachment", "Sail")
    distances = [
        (Vector(v) - Vector(data["SheetLineAttachment"])).length
        for v in data["Sail"]["Vertices"]
    ]
    require(
        distances[corners["Clew"]] < 1e-5,
        "SheetLineAttachment: must coincide with the clew vertex",
    )
    data["SheetVertex"] = corners["Clew"]
    return data


def number(value):
    return ("0" if value == 0 else format(value, ".9g")) + "f"


def vector(values, kind="Vector3"):
    return f"new {kind}({', '.join(number(v) for v in values)})"


def part_lines(name, data):
    lines = [
        f"                {name} = new LooseFootedSpritsailMeshPart",
        "                {",
    ]
    matrix = data["RestTransform"]
    columns = [vector(matrix.col[i], "Vector4") for i in range(4)]
    lines.append("                    RestTransform = new Matrix4x4(")
    lines.extend(
        f"                        {column}{',' if i < 3 else ''}"
        for i, column in enumerate(columns)
    )
    lines.append("                    ),")
    for field in ("BoundsMin", "BoundsMax"):
        lines.append(f"                    {field} = {vector(data[field])},")
    for field, kind in (
        ("Vertices", "Vector3"),
        ("Normals", "Vector3"),
        ("UV", "Vector2"),
    ):
        lines.extend(
            (f"                    {field} = new {kind}[]", "                    {")
        )
        lines.extend(f"                        {vector(v, kind)}," for v in data[field])
        lines.append("                    },")
    lines.extend(
        (
            "                    Materials = new LooseFootedSpritsailMaterial[]",
            "                    {",
        )
    )
    lines.extend(
        f"                        LooseFootedSpritsailMaterial.{m},"
        for m in data["Materials"]
    )
    lines.extend(
        (
            "                    },",
            "                    SubmeshTriangles = new int[][]",
            "                    {",
        )
    )
    for triangles in data["SubmeshTriangles"]:
        lines.extend(("                        new int[]", "                        {"))
        # One index per line follows the existing generated mesh layout.
        lines.extend(f"                            {index}," for index in triangles)
        lines.append("                        },")
    lines.extend(("                    },", "                },"))
    return lines


def bake(cut="A"):
    definition = get_cut(cut)
    data = collect()
    lines = [
        "// <auto-generated />",
        f"// Baked by mss assets bake loose_foot_spritsail --cut {cut.upper()}.",
        "// Part-local metres, Y-up; rest transforms preserve the authored assembly and origins.",
        "using UnityEngine;",
        "",
        "namespace MoreSailwindSails.Sails.LooseFootedSpritsail",
        "{",
        f"    internal static class {definition.class_name}",
        "    {",
        "        internal static LooseFootedSpritsailAssetData Create() =>",
        "            new LooseFootedSpritsailAssetData",
        "            {",
    ]
    for name in PARTS:
        lines.extend(part_lines(name, data[name]))
    for name in ("ReefLineAttachment", "SheetLineAttachment"):
        lines.append(f"                {name} = {vector(data[name])},")
    for name in ("Tack", "Throat", "Peak", "Clew", "SheetVertex"):
        lines.append(f"                {name} = {data[name]},")
    return "\n".join([*lines, "            };", "    }", "}", ""])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cut", type=str.upper, choices=list(CUTS), default="A")
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check", action="store_true")
    args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    options = parser.parse_args(args)
    output = options.output or get_cut(options.cut).source_path
    source = bake(options.cut)
    if options.check:
        if not output.is_file() or output.read_text(encoding="utf-8") != source:
            raise RuntimeError(f"Stale baked spritsail: regenerate {output}")
        print(f"PASS: baked spritsail matches {output}")
    else:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(source, encoding="utf-8")
        print(f"Baked spritsail Mk.{options.cut}: {output}")


if __name__ == "__main__":
    main()
