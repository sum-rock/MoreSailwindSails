"""Bake the authored OBJ into the embedded snotter mesh using Blender triangulation.

Run from the repository root: blender -b --python tools/convert_snotter.py
Only the generated assets/snotter/snotter.bytes file is written.
"""

from pathlib import Path
import struct
import re
import math
import sys

import bpy


root = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(root))
from tools.snotter_asset import coincident_faces

positions, texcoords, normals, faces = [], [], [], []
parts = []
part = None
material = None
face_materials = []
seen_faces = set()
duplicates = 0
face_lines = []
for line_number, line in enumerate((root / "assets/snotter/snotter.obj").read_text().splitlines(), 1):
    fields = line.split()
    if not fields:
        continue
    if fields[0] == "o":
        part = {"Sleave": 0, "Bolt": 1, "Mounting": 2}.get(" ".join(fields[1:]))
    elif fields[0] == "usemtl":
        name = " ".join(fields[1:])
        match = re.fullmatch(r"(DarkWood|Metal)(?:\.[0-9]+)?", name)
        if not match:
            raise ValueError(f"Unknown material {name!r}; use DarkWood or Metal.")
        material = {"DarkWood": 0, "Metal": 1}[match.group(1)]
    elif fields[0] == "v":
        positions.append(tuple(map(float, fields[1:4])))
    elif fields[0] == "vt":
        texcoords.append(tuple(map(float, fields[1:3])))
    elif fields[0] == "vn":
        normals.append(tuple(map(float, fields[1:4])))
    elif fields[0] == "f":
        face = [tuple(int(index) - 1 for index in corner.split("/")) for corner in fields[1:]]
        if any(len(corner) != 3 or min(corner) < 0 for corner in face):
            raise ValueError("Re-export with UVs, normals and positive OBJ indices.")
        if part is None:
            raise ValueError("Expected Sleave, Bolt and Mounting mesh objects.")
        if material is None:
            raise ValueError("Every face must have a DarkWood or Metal material assignment.")
        # Remove only exact duplicates with matching part, material, UVs and normals.
        key = (part, material, tuple(sorted(
            (positions[v], texcoords[uv], normals[n]) for v, uv, n in face
        )))
        if key in seen_faces:
            duplicates += 1
            continue
        seen_faces.add(key)
        faces.append(face)
        face_lines.append((part, line_number, tuple(positions[v] for v, _, _ in face)))
        face_materials.append(material)
        parts.extend([part] * len(face))

if set(parts) != {0, 1, 2}:
    raise ValueError("The export must contain Sleave, Bolt and Mounting faces.")
for part, first, second in coincident_faces(face_lines):
    name = ("Sleave", "Bolt", "Mounting")[part]
    print(f"WARNING: {name} faces at OBJ lines {first} and {second} occupy the same "
          "positions with different attributes; both are retained. Resolve in Blender.")
if any(not all(math.isfinite(v) for v in normal)
       or abs(sum(v * v for v in normal) - 1) > 0.001 for normal in normals):
    raise ValueError("Re-export with finite, unit-length surface normals.")

mesh = bpy.data.meshes.new(name="Snotter conversion")
try:
    mesh.from_pydata(positions, [], [[corner[0] for corner in face] for face in faces])
    mesh.calc_loop_triangles()
    # Separate face corners retain UV seams and authored smooth/sharp normals.
    corners = [corner for face in faces for corner in face]
    triangles = [tuple(triangle.loops) for triangle in mesh.loop_triangles]
    materials = [face_materials[triangle.polygon_index] for triangle in mesh.loop_triangles]
    for triangle in mesh.loop_triangles:
        a, b, c = [mesh.vertices[mesh.loops[i].vertex_index].co for i in triangle.loops]
        cross = (b - a).cross(c - a)
        normal = normals[corners[triangle.loops[0]][2]]
        if cross.length_squared < 1e-12 or cross.dot(normal) <= 0:
            raise ValueError("Degenerate triangle or winding inconsistent with authored normals.")
    output = root / "assets/snotter/snotter.bytes"
    with output.open("wb") as stream:
        stream.write(struct.pack("<4sii", b"MSN5", len(corners), len(triangles) * 3))
        for (position, uv, normal), part in zip(corners, parts):
            stream.write(struct.pack("<8fi", *positions[position], *texcoords[uv],
                                     *normals[normal], part))
        for triangle, material in zip(triangles, materials):
            stream.write(struct.pack("<4i", *triangle, material))
    print(f"Wrote {output}: {len(corners)} vertices, {len(triangles)} triangles; "
          f"DarkWood={materials.count(0)}, Metal={materials.count(1)}; "
          f"omitted {duplicates} duplicate faces")
finally:
    bpy.data.meshes.remove(mesh)
