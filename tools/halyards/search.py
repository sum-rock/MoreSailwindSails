"""Search bounded mast-surface rings for a deterministic, clear halyard seat."""

from dataclasses import dataclass
import math

import numpy as np

from tools.halyards.geometry import (
    Sphere,
    spheres_clear,
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
    template_radius = float(np.linalg.norm(template_points, axis=1).max())
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
    # Search at the reference level, then upward along the mast in 2 cm steps.
    for index in range(int(math.floor(height_range / 0.02 + 1e-8)) + 1):
        shift = index * 0.02
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
            rotation = from_to(template_rotation[:, 2], normal) @ template_rotation
            # Only a mounting-mast collision permits a small outward seating correction.
            for millimetres in range(6):
                adjusted_offset = float(np.float32(offset + millimetres * 0.001))
                position = contact + normal * adjusted_offset
                candidate = Sphere(center=position, radius=template_radius)
                if any(
                    not spheres_clear(a=candidate, b=obstacle, clearance=clearance)
                    for obstacle in obstacles
                ):
                    if millimetres == 0:
                        break
                    continue
                points = template_points @ rotation.T + position
                box = swept_box(points, position, rotation)
                mast_collision = triangles_intersect_box(surface, box)
                structure_collision = len(structure) and triangles_intersect_box(
                    structure, box
                )
                if not mast_collision and not structure_collision:
                    return Seat(
                        contact=contact,
                        normal=normal,
                        source_normal=template_rotation[:, 2],
                        offset=adjusted_offset,
                        template_index=template_index,
                    )
                if millimetres == 0 and not mast_collision:
                    break
    return None
