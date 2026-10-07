"""Checks authoring diagnostics without Blender or game assemblies."""

import unittest

from tools.snotter_asset import coincident_faces


class OverlapChecks(unittest.TestCase):
    def test_reordered_face_corners_are_reported_by_source_line(self):
        quad = ((0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0))
        # Shading attributes and OBJ indices deliberately do not enter this key.
        polygons = [(2, 20, quad), (2, 90, tuple(reversed(quad)))]
        self.assertEqual(coincident_faces(polygons), [(2, 20, 90)])

    def test_adjacent_faces_and_independent_parts_are_not_duplicates(self):
        a = ((0, 0, 0), (1, 0, 0), (0, 1, 0))
        b = ((1, 0, 0), (1, 1, 0), (0, 1, 0))
        self.assertEqual(coincident_faces([(0, 1, a), (0, 2, b), (2, 3, a)]), [])

    def test_all_copies_refer_to_the_first_face(self):
        face = ((0, 0, 0), (1, 0, 0), (0, 1, 0))
        self.assertEqual(
            coincident_faces([(2, line, face) for line in (2, 4, 8)]),
            [(2, 2, 4), (2, 2, 8)],
        )
