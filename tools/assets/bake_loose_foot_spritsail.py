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
REEF_FRAMES = (10, 60, 120, 180, 240)
MATERIALS = ("Cloth", "Metal", "DarkWood", "Black")
STRUCK_PARTS = ("Sprit.struck", "Sail.struck", "Tie.01", "Tie.02", "Tie.03", "Tie.04")


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
            not ancestor.constraints
            and (
                not ancestor.animation_data
                or ancestor.name
                in ("Sail", "Topsail", "Sprit", "Spar", "SheetLineAttachment")
            ),
            f"{name}: constraints or animation outside Sail/Topsail/Sprit/Spar/SheetLineAttachment are unsupported",
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
                (
                    name in ("Sail", "Topsail")
                    and m.type in ("HOOK", "CLOTH", "COLLISION")
                    or name not in ("Sail", "Topsail")
                    and (
                        m.type in ("SOLIDIFY", "DECIMATE")
                        or (name in STRUCK_PARTS and m.type == "SMOOTH")
                    )
                )
                and m.show_viewport
                and m.show_render
                for m in obj.modifiers
            ),
            f"{name}: expected enabled cloth Hook/Cloth/Collision, hardware Solidify/Decimate or struck Smooth",
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
    # Follow quad rows from the foot so tapered outer edges need no vertical columns.
    neighbors = {i: set() for i in range(len(points))}
    for edge in mesh.edges:
        a, b = edge.vertices
        neighbors[a].add(b)
        neighbors[b].add(a)
    corners = [i for i, adjacent in neighbors.items() if len(adjacent) == 2]
    require(len(corners) == 4, "Sail: expected four grid corners")
    tack, throat = sorted(
        sorted(corners, key=lambda i: points[i].y)[-2:], key=lambda i: points[i].z
    )
    clew, peak = sorted(
        [i for i in corners if i not in (tack, throat)], key=lambda i: points[i].z
    )
    foot = [clew]
    while foot[-1] != tack:
        candidates = neighbors[foot[-1]] - set(foot)
        low = points[clew]
        direction = points[tack] - low
        candidates = [
            i for i in candidates if (points[i] - low).cross(direction).length < 1e-4
        ]
        require(len(candidates) == 1, "Sail: missing straight foot boundary")
        foot.append(candidates[0])
    grid = [foot]
    used = set()
    while grid[-1][0] != peak:
        row = grid[-1]
        following = [None] * len(row)
        for c in range(len(row) - 1):
            matches = [
                f
                for f in mesh.polygons
                if f.index not in used
                and row[c] in f.vertices
                and row[c + 1] in f.vertices
            ]
            require(len(matches) == 1, "Sail: incomplete quad rows")
            face = matches[0]
            used.add(face.index)
            for offset in (0, 1):
                current = row[c + offset]
                other = [
                    i for i in face.vertices if i not in row and i in neighbors[current]
                ]
                require(len(other) == 1, "Sail: invalid quad connectivity")
                following[c + offset] = other[0]
        require(all(i is not None for i in following), "Sail: incomplete row")
        grid.append(following)
        require(len(grid) <= len(points), "Sail: cyclic grid")
    columns = [list(column) for column in zip(*grid)]
    rows = len(grid)
    width = points[tack].y - points[clew].y
    require(width > 1e-5, "Sail: zero width")
    fractions = [
        (points[i] - points[clew]).length / (points[peak] - points[clew]).length
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
    dy = points[peak].y - points[clew].y
    dz = points[peak].z - points[clew].z
    foot_dz = points[tack].z - points[clew].z
    basis_z = (
        (values[peak] - values[clew]) * width - (values[tack] - values[clew]) * dy
    ) / (dz * width - foot_dz * dy)
    basis_y = (values[tack] - values[clew] - basis_z * foot_dz) / width
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
    cloth = name in ("Sail", "Topsail")
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
                if not cloth and obj.name != "Sail.struck":
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
                        material in MATERIALS,
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
                # Folded smooth surfaces can have corner normals beyond a triangle's
                # hemisphere. Topology and nondegeneracy are checked independently.
                require(
                    name == "Struck"
                    or all(cross.dot(Vector(normals[i])) > 0 for i in triangles[-3:]),
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
    # matrix_local / location alone lose the Keep Transform parent-inverse offset.
    return tuple(
        CONVERSION
        @ (
            bpy.data.objects[parent].matrix_world.inverted()
            @ obj.matrix_world.translation
        )
    )


def collect(
    reef_frames=REEF_FRAMES,
    generate_cloth_uv=False,
    topsail=False,
    spar=False,
    topsail_hide_frame=140,
):
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
    bpy.context.scene.frame_set(0)
    bpy.context.view_layer.update()
    sail = object_named("Sail", "MESH")
    # Assets without a dedicated cloth map receive proportional planar UVs in memory.
    if generate_cloth_uv and sail.data.uv_layers.get("ClothUV") is None:
        layer = sail.data.uv_layers.new(name="ClothUV")
        points = [v.co for v in sail.data.vertices]
        min_y, min_z = min(p.y for p in points), min(p.z for p in points)
        extent = max(max(p.y for p in points) - min_y, max(p.z for p in points) - min_z)
        require(extent > 0, "Sail: empty UV extent")
        for loop in sail.data.loops:
            point = points[loop.vertex_index]
            layer.data[loop.index].uv = (
                (point.y - min_y) / extent,
                (point.z - min_z) / extent,
            )
    corners = cloth_grid(sail)
    data = {name: extract_part(name, sources) for name, sources in PARTS.items()}
    data["Struck"] = extract_part("Struck", STRUCK_PARTS)
    alignment = (
        object_named("Mast", "MESH").matrix_world
        @ object_named("Mast.struck", "MESH").matrix_world.inverted()
    )
    data["Struck"]["RestTransform"] = (
        CONVERSION @ alignment @ CONVERSION @ data["Struck"]["RestTransform"]
    )
    for field in ("ReefLineAttachment", "SheetLineAttachment"):
        obj = object_named(field + ".struck", "EMPTY")
        data["Struck" + field] = tuple(
            CONVERSION
            @ (
                bpy.data.objects["Sprit.struck"].matrix_world.inverted()
                @ obj.matrix_world.translation
            )
        )
    # The mounting rod may extend inside the collar radius; it must not drive mast fitting.
    collar = extract_part("Snotter", ("Snotter",))
    points = [collar["RestTransform"] @ Vector(v) for v in collar["Vertices"]]
    data["CollarInnerRadius"] = min(math.hypot(p.x, p.z) for p in points)
    require(
        math.isfinite(data["CollarInnerRadius"]) and data["CollarInnerRadius"] > 0,
        "Snotter: missing measurable inner radius",
    )
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
    if topsail:
        add_topsail(data)
    if spar:
        data["Spar"] = extract_part("Spar", ("Spar",))
    data["ReefPoses"] = reef_poses(
        sail,
        reef_frames=reef_frames,
        topsail=topsail,
        spar=spar,
        topsail_hide_frame=topsail_hide_frame,
    )
    bpy.context.scene.frame_set(0)
    return data


def reef_poses(
    sail, reef_frames=REEF_FRAMES, topsail=False, spar=False, topsail_hide_frame=140
):
    """Evaluate both cloth simulations together in one forward pass, without rewinding."""
    scene = bpy.context.scene
    inverse = sail.matrix_world.inverted()
    sprit = bpy.data.objects["Sprit"]
    rest = CONVERSION @ sprit.matrix_world @ CONVERSION
    rest_rotation = rest.to_quaternion()
    pivot = sprit.matrix_world.translation.copy()
    scale = sprit.matrix_world.to_scale()
    topology = [tuple(p.vertices) for p in sail.data.polygons]
    top = bpy.data.objects["Topsail"] if topsail else None
    top_topology = [tuple(p.vertices) for p in top.data.polygons] if top else None
    poses = {}
    for frame in range(reef_frames[-1] + 1):
        scene.frame_set(frame)
        if top:
            require(
                topsail_hide_frame is None
                or top.hide_render == (frame >= topsail_hide_frame),
                f"Topsail: expected removal at frame {topsail_hide_frame}",
            )
            if top.hide_viewport:
                top.hide_viewport = False
        depsgraph = bpy.context.evaluated_depsgraph_get()
        evaluated = sail.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        try:
            require(
                len(mesh.vertices) == len(sail.data.vertices)
                and [tuple(p.vertices) for p in mesh.polygons] == topology,
                f"Sail: topology changed at frame {frame}",
            )
            points = [
                tuple(CONVERSION @ inverse @ evaluated.matrix_world @ v.co)
                for v in mesh.vertices
            ]
            require(
                all(finite(p) for p in points),
                f"Sail: non-finite pose at frame {frame}",
            )
            matrix = sprit.evaluated_get(depsgraph).matrix_world
            require(
                all(finite(row) for row in matrix)
                and (matrix.translation - pivot).length < 1e-5
                and (matrix.to_scale() - scale).length < 1e-5,
                f"Sprit: animation must rotate about a fixed hinge at frame {frame}",
            )
            if frame in reef_frames:
                rotation = (
                    CONVERSION @ matrix @ CONVERSION
                ).to_quaternion() @ rest_rotation.inverted()
                if spar:
                    spar_matrix = (
                        bpy.data.objects["Spar"].evaluated_get(depsgraph).matrix_world
                    )
                    require(
                        all(finite(row) for row in spar_matrix)
                        and spar_matrix.to_3x3().determinant() > 1e-10,
                        f"Spar: invalid transform at frame {frame}",
                    )
                poses[frame] = (
                    points,
                    (rotation.x, rotation.y, rotation.z, rotation.w),
                    CONVERSION
                    @ bpy.data.objects["Spar"].evaluated_get(depsgraph).matrix_world
                    @ CONVERSION
                    if spar
                    else None,
                )
        finally:
            evaluated.to_mesh_clear()
        if top:
            evaluated_top = top.evaluated_get(depsgraph)
            top_mesh = evaluated_top.to_mesh()
            try:
                require(
                    len(top_mesh.vertices) == len(top.data.vertices)
                    and [tuple(p.vertices) for p in top_mesh.polygons] == top_topology,
                    "Topsail: changed topology",
                )
                if frame in reef_frames:
                    points = [
                        tuple(CONVERSION @ inverse @ evaluated_top.matrix_world @ v.co)
                        for v in top_mesh.vertices
                    ]
                    require(all(finite(v) for v in points), "Topsail: non-finite pose")
                    poses[frame][0].extend(points)
            finally:
                evaluated_top.to_mesh_clear()
    return [poses[frame] if spar else poses[frame][:2] for frame in reef_frames]


def add_topsail(data):
    """Append the authored topsail in main-sail local space; preserve one cloth renderer."""
    scene = bpy.context.scene
    scene.frame_set(0)
    sail = bpy.data.objects["Sail"]
    top = object_named("Topsail", "MESH")
    layer = top.data.uv_layers.get("ClothUV") or top.data.uv_layers.new(name="ClothUV")
    points = [v.co for v in top.data.vertices]
    low_y, low_z = min(p.y for p in points), min(p.z for p in points)
    extent = max(max(p.y for p in points) - low_y, max(p.z for p in points) - low_z)
    require(extent > 0, "Topsail: empty UV extent")
    for loop in top.data.loops:
        p = points[loop.vertex_index]
        layer.data[loop.index].uv = ((p.y - low_y) / extent, (p.z - low_z) / extent)
    part = extract_part("Topsail", ("Topsail",))
    relative = data["Sail"]["RestTransform"].inverted() @ part["RestTransform"]
    mesh = data["Sail"]
    start = len(mesh["Vertices"])
    data["TopsailVertexStart"] = start
    data["TopsailTriangleStart"] = len(mesh["SubmeshTriangles"][0])
    mesh["Vertices"].extend(tuple(relative @ Vector(v)) for v in part["Vertices"])
    mesh["Normals"].extend(
        tuple((relative.to_3x3().inverted().transposed() @ Vector(v)).normalized())
        for v in part["Normals"]
    )
    mesh["UV"].extend(part["UV"])
    require(part["Materials"] == ["Cloth"], "Topsail: expected cloth")
    mesh["SubmeshTriangles"][0].extend(i + start for i in part["SubmeshTriangles"][0])
    for field, operation in (("BoundsMin", min), ("BoundsMax", max)):
        mesh[field] = tuple(operation(v[a] for v in mesh["Vertices"]) for a in range(3))


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
    data = collect(
        reef_frames=definition.reef_frames,
        generate_cloth_uv=definition.generate_cloth_uv,
        topsail=definition.topsail,
        spar=definition.spar,
        topsail_hide_frame=definition.topsail_hide_frame,
    )
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
    if definition.reef_fractions is not None:
        lines.append(
            "                ReefFractions = new float[] { "
            + ", ".join(number(v) for v in definition.reef_fractions)
            + " },"
        )
        lines.append(
            f"                TopsailVertexStart = {data['TopsailVertexStart']},"
        )
        lines.append(
            f"                TopsailTriangleStart = {data['TopsailTriangleStart']},"
        )
    lines.extend(
        (
            "                ReefPoses = new LooseFootedSpritsailReefPose[]",
            "                {",
        )
    )
    for pose in data["ReefPoses"]:
        points, rotation = pose[:2]
        spar_matrix = pose[2] if definition.spar else None
        lines.extend(
            (
                "                    new LooseFootedSpritsailReefPose",
                "                    {",
                f"                        SpritRotation = {vector(rotation, 'Quaternion')},",
                *(
                    [
                        f"                        SparTransform = new Matrix4x4({', '.join(vector(spar_matrix.col[i], 'Vector4') for i in range(4))}),"
                    ]
                    if spar_matrix is not None
                    else []
                ),
                "                        Vertices = new Vector3[]",
                "                        {",
            )
        )
        lines.extend(
            f"                            {vector(point)}," for point in points
        )
        lines.extend(("                        },", "                    },"))
    lines.append("                },")
    for name in (*PARTS, "Struck", *(("Spar",) if definition.spar else ())):
        lines.extend(part_lines(name, data[name]))
    lines.append(
        f"                CollarInnerRadius = {number(data['CollarInnerRadius'])},"
    )
    for name in (
        "ReefLineAttachment",
        "SheetLineAttachment",
        "StruckReefLineAttachment",
        "StruckSheetLineAttachment",
    ):
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
