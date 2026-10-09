"""Bake the authored sail mount with Blender triangulation; no textures required.

Run from the repository root:
blender -b --factory-startup --python-exit-code 1 --python tools/convert_sail_mount.py
Only assets/sail_mount/sail_mount.bytes is written. See docs/DEVELOPMENT.md
for the binary layout and part/material IDs.
"""

from collections import Counter
import hashlib
import math
from pathlib import Path
import re
import struct

import bpy


def bake():
    root = Path(__file__).resolve().parents[1]
    source = (root / "assets/sail_mount/SailMount.obj").read_bytes()
    positions, uvs, normals, faces = [], [], [], []
    face_parts, face_materials, face_lines = [], [], []
    part, material = None, None
    loose_edges = 0

    for line_number, line in enumerate(source.decode().splitlines(), start=1):
        fields = line.partition("#")[0].split()
        if not fields:
            continue
        kind = fields[0]
        if kind == "o":
            name = " ".join(fields[1:])
            if name == "LuffEdge":
                part = 0
            elif match := re.fullmatch(r"Eyelet_0([1-7])", name):
                part = int(match[1])
            elif match := re.fullmatch(r"Rope_?0([1-7])", name):
                part = 7 + int(match[1])
            else:
                raise ValueError(f"Line {line_number}: unexpected object {name!r}.")
        elif kind in ("v", "vt", "vn"):
            size = 2 if kind == "vt" else 3
            values = tuple(map(float, fields[1:]))
            if len(values) != size or not all(map(math.isfinite, values)):
                raise ValueError(f"Line {line_number}: invalid {kind} coordinates.")
            if kind == "vn" and abs(sum(v * v for v in values) - 1) > 0.001:
                raise ValueError(f"Line {line_number}: normal must have unit length.")
            {"v": positions, "vt": uvs, "vn": normals}[kind].append(values)
        elif kind == "usemtl":
            material = " ".join(fields[1:])
        elif kind == "l":
            loose_edges += 1
        elif kind == "f":
            if part is None:
                raise ValueError(f"Line {line_number}: face has no named object.")
            if part == 0:
                slot = 0  # SailCloth, selected by object identity.
            elif part >= 8:
                slot = 3  # Rope, ignoring inherited OBJ material state.
            else:
                match = re.fullmatch(r"(Brass|ThickCloth)(?:\.[0-9]+)?", material or "")
                if not match:
                    raise ValueError(f"Line {line_number}: eyelet needs Brass/ThickCloth tags.")
                slot = {"Brass": 1, "ThickCloth": 2}[match[1]]
            face = []
            for token in fields[1:]:
                indices = token.split("/")
                if len(indices) != 3 or not all(indices):
                    raise ValueError(f"Line {line_number}: export UVs and normals.")
                corner = tuple(int(index) - 1 for index in indices)
                if any(i < 0 or i >= len(data)
                       for i, data in zip(corner, (positions, uvs, normals))):
                    raise ValueError(f"Line {line_number}: invalid positive OBJ indices.")
                face.append(corner)
            if len(face) < 3:
                raise ValueError(f"Line {line_number}: face needs at least three corners.")
            faces.append(face)
            face_parts.append(part)
            face_materials.append(slot)
            face_lines.append(line_number)
        elif kind not in ("mtllib", "s", "g"):
            raise ValueError(f"Line {line_number}: unsupported OBJ record {kind!r}.")

    if set(face_parts) != set(range(15)):
        raise ValueError("Expected LuffEdge, seven eyelets and seven ropes with faces.")
    for eyelet in range(1, 8):
        if {m for p, m in zip(face_parts, face_materials) if p == eyelet} != {1, 2}:
            raise ValueError(f"Eyelet_{eyelet:02d} must contain both Brass and ThickCloth.")

    mesh = bpy.data.meshes.new(name="Sail mount bake")
    try:
        mesh.from_pydata(positions, [], [[c[0] for c in face] for face in faces])
        mesh.calc_loop_triangles()
        corners = [corner for face in faces for corner in face]
        vertices, triangles, lookup = [], [], {}
        for triangle in mesh.loop_triangles:
            polygon = triangle.polygon_index
            part = face_parts[polygon]
            a, b, c = [mesh.vertices[index].co for index in triangle.vertices]
            cross = (b - a).cross(c - a)
            shading = [normals[corners[loop][2]] for loop in triangle.loops]
            average = tuple(sum(n[i] for n in shading) for i in range(3))
            if cross.length_squared < 1e-20 or cross.dot(average) <= 0:
                raise ValueError(
                    f"Line {face_lines[polygon]}: degenerate triangle or inconsistent winding."
                )
            indices = []
            for loop in triangle.loops:
                position, uv, normal = corners[loop]
                vertex = (*positions[position], *uvs[uv], *normals[normal], part)
                # Share only identical attributes within the same independently posed part.
                if vertex not in lookup:
                    lookup[vertex] = len(vertices)
                    vertices.append(vertex)
                indices.append(lookup[vertex])
            triangles.append((*indices, face_materials[polygon]))
    finally:
        bpy.data.meshes.remove(mesh)

    payload = bytearray(struct.pack("<4sII", b"MSL1", len(vertices), len(triangles) * 3))
    payload.extend(hashlib.sha256(source).digest())
    for vertex in vertices:
        payload.extend(struct.pack("<8fI", *vertex))
    for triangle in triangles:
        payload.extend(struct.pack("<4I", *triangle))
    output = root / "assets/sail_mount/sail_mount.bytes"
    output.write_bytes(payload)
    print(f"Wrote {output}: {len(vertices)} vertices, {len(triangles)} triangles, "
          f"{len(payload)} bytes; ignored {loose_edges} loose line records.")
    print("Triangle material slots (SailCloth=0, Brass=1, ThickCloth=2, Rope=3):",
          dict(sorted(Counter(t[3] for t in triangles).items())))


if __name__ == "__main__":
    bake()
