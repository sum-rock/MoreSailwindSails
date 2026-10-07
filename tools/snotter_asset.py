"""Pure asset checks shared by the Blender baker and offline asset tests."""

import math


def coincident_faces(polygons):
    """Report identical face positions within a part, regardless of shading data.

    Entries are (part, source_line, positions). Retain the first line as the
    reference so additional copies all point back to the same authored face.
    Adjacent faces and matching faces on independently moving parts are allowed.
    """
    seen = {}
    overlaps = []
    for part, line, positions in polygons:
        key = (part, tuple(sorted(positions)))
        if key in seen:
            overlaps.append((part, seen[key], line))
        else:
            seen[key] = line
    return overlaps


def axis_distance(triangle):
    """Minimum distance from a triangle to the source Y mast axis."""
    points = [(p[0], p[2]) for p in triangle]
    crosses = [a[0] * b[1] - a[1] * b[0]
               for a, b in zip(points, points[1:] + points[:1])]
    if abs(sum(crosses)) > 1e-12 and (min(crosses) >= 0 or max(crosses) <= 0):
        return 0.0
    distances = []
    for a, b in zip(points, points[1:] + points[:1]):
        dx, dz = b[0] - a[0], b[1] - a[1]
        length = dx * dx + dz * dz
        t = max(0, min(1, -(a[0] * dx + a[1] * dz) / length)) if length else 0
        distances.append(math.hypot(a[0] + t * dx, a[1] + t * dz))
    return min(distances)


def measure_fit(part_triangles, head_inner):
    """Measure geometry; use the authored marker for the ambiguous bolt head seam."""
    sleeve = [t for part, t in part_triangles if part == 0]
    bolt = [p for part, t in part_triangles if part == 1 for p in t]
    mounting = [p for part, t in part_triangles if part == 2 for p in t]
    inner = math.floor(min(axis_distance(t) for t in sleeve) * 10000) / 10000
    outer = math.ceil(max(math.hypot(p[0], p[2]) for t in sleeve for p in t) * 10000) / 10000
    mount = max(math.hypot(p[0], p[2]) for p in mounting)
    start, end = min(-p[0] for p in bolt), max(-p[0] for p in bolt)
    if inner <= 0 or not start < head_inner < end:
        raise ValueError("Invalid mast opening or bolt head marker.")
    if not any(abs(-p[0] - head_inner) < 1e-6 for p in bolt):
        raise ValueError("The bolt head marker no longer matches an exported vertex plane; update snotter.fit.json.")
    return inner, outer, mount, start, head_inner, end
