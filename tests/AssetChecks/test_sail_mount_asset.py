"""Validate the baked sail mount's freshness, geometry and runtime material contract."""

from collections import defaultdict
import hashlib
import math
from pathlib import Path
import struct
import unittest


class SailMountAssetChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        folder = Path(__file__).resolve().parents[2] / "assets/sail_mount"
        cls.source = (folder / "SailMount.obj").read_bytes()
        cls.payload = (folder / "sail_mount.bytes").read_bytes()
        cls.magic, cls.vertex_count, cls.index_count = struct.unpack_from("<4sII", cls.payload)
        vertex_end = 44 + cls.vertex_count * 36
        cls.vertices = list(struct.iter_unpack("<8fI", cls.payload[44:vertex_end]))
        cls.triangles = list(struct.iter_unpack("<4I", cls.payload[vertex_end:]))

    def test_layout_and_source_freshness(self):
        self.assertEqual(self.magic, b"MSL1")
        self.assertEqual(self.payload[12:44], hashlib.sha256(self.source).digest(),
                         "SailMount.obj changed; rerun tools/convert_sail_mount.py.")
        self.assertEqual(self.index_count % 3, 0)
        self.assertEqual(len(self.payload), 44 + self.vertex_count * 36 + self.index_count // 3 * 16)
        # OBJ polygons, including decimated n-gons, must survive triangulation.
        expected = sum(len(line.split()) - 3 for line in self.source.decode().splitlines()
                       if line.startswith("f "))
        self.assertEqual(len(self.triangles), expected)

    def test_finite_geometry_normals_indices_and_winding(self):
        referenced = set()
        for vertex in self.vertices:
            self.assertTrue(all(map(math.isfinite, vertex[:8])))
            self.assertAlmostEqual(sum(x * x for x in vertex[5:8]), 1, delta=0.001)
        for *indices, material in self.triangles:
            self.assertTrue(all(0 <= i < self.vertex_count for i in indices))
            referenced.update(indices)
            a, b, c = [self.vertices[i] for i in indices]
            u = [b[i] - a[i] for i in range(3)]
            v = [c[i] - a[i] for i in range(3)]
            cross = (u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2],
                     u[0] * v[1] - u[1] * v[0])
            self.assertGreater(sum(x * x for x in cross), 1e-20)
            self.assertGreater(sum(cross[i] * (a[i + 5] + b[i + 5] + c[i + 5])
                                   for i in range(3)), 0)
        self.assertEqual(referenced, set(range(self.vertex_count)))

    def test_parts_keep_independent_material_regions(self):
        slots = defaultdict(set)
        for *indices, material in self.triangles:
            parts = {self.vertices[i][8] for i in indices}
            self.assertEqual(len(parts), 1, "Triangle crosses independently posed parts.")
            slots[parts.pop()].add(material)
        self.assertEqual(set(slots), set(range(15)))
        self.assertEqual(slots[0], {0})
        for eyelet in range(1, 8):
            self.assertEqual(slots[eyelet], {1, 2})
        for rope in range(8, 15):
            self.assertEqual(slots[rope], {3})

    def test_authored_position_uv_and_normal_values_are_preserved(self):
        # Compare float32 values to allow only serialization rounding, not remapping.
        source_values = {"v": set(), "vt": set(), "vn": set()}
        for line in self.source.decode().splitlines():
            fields = line.split()
            if fields and fields[0] in source_values:
                values = tuple(map(float, fields[1:]))
                source_values[fields[0]].add(struct.pack(f"<{len(values)}f", *values))
        for vertex in self.vertices:
            self.assertIn(struct.pack("<3f", *vertex[:3]), source_values["v"])
            self.assertIn(struct.pack("<2f", *vertex[3:5]), source_values["vt"])
            self.assertIn(struct.pack("<3f", *vertex[5:8]), source_values["vn"])
