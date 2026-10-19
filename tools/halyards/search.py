"""Search bounded mast-surface rings for a deterministic, clear halyard seat."""

from dataclasses import dataclass
import math

import numpy as np

from tools.halyards.geometry import (
    boxes_clear,
    from_to,
    ray_surface,
    swept_box,
    triangles_intersect_box,
    unit,
)


@dataclass
class Seat:
    """A boat-local mounting contact with the existing C# configuration fields."""

    contact: np.ndarray
    normal: np.ndarray
    source_normal: np.ndarray
    offset: float
    template_index: int


def search(
    *,
    axis_origin,
    axis,
    surface,
    template_points,
    template_rotation,
    template_origin,
    offset,
    template_index,
    obstacles,
    structure,
    clearance,
    height_range,
):
    axis = unit(axis)
    # Boat aft is the first radial direction where possible, independent of asset order.
    radial = np.array([0.0, 0.0, -1.0])
    radial -= axis * (radial @ axis)
    if np.linalg.norm(radial) < 1e-6:
        radial = np.array([1.0, 0.0, 0.0]) - axis * axis[0]
    radial = unit(radial)
    tangent = unit(np.cross(axis, radial))
    native_height = (template_origin - axis_origin) @ axis
    vertices = surface.reshape(-1, 3)
    projected = vertices - axis_origin
    radius = (
        np.linalg.norm(projected - np.outer(projected @ axis, axis), axis=1).max() + 1
    )
    shifts = [0.0]
    for index in range(1, int(math.floor(height_range / 0.05 + 1e-8)) + 1):
        shifts.extend([index * 0.05, -index * 0.05])
    for shift in shifts:
        center = axis_origin + axis * (native_height + shift)
        for degrees in range(0, 360, 5):
            radians = math.radians(degrees)
            direction = radial * math.cos(radians) + tangent * math.sin(radians)
            hit = ray_surface(center + direction * radius, -direction, surface)
            if hit is None:
                continue
            contact, normal = hit
            if abs(normal @ axis) > 0.35:
                continue
            # Check values after float32 serialization, as used by the Unity configuration.
            contact = contact.astype(np.float32).astype(float)
            normal = unit(normal.astype(np.float32).astype(float))
            position = contact + normal * float(np.float32(offset))
            rotation = from_to(template_rotation[:, 2], normal) @ template_rotation
            points = template_points @ rotation.T + position
            box = swept_box(points, position, rotation)
            if any(not boxes_clear(box, obstacle, clearance) for obstacle in obstacles):
                continue
            if triangles_intersect_box(surface, box) or (
                len(structure) and triangles_intersect_box(structure, box)
            ):
                continue
            return Seat(
                contact=contact,
                normal=normal,
                source_normal=template_rotation[:, 2],
                offset=offset,
                template_index=template_index,
            )
    return None
