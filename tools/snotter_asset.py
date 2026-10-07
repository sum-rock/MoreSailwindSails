"""Pure asset checks shared by the Blender baker and offline asset tests."""


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
