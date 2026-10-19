"""Pure geometry for transformed mast surfaces and conservative winch clearance."""

from dataclasses import dataclass
import itertools
import math

import numpy as np


def unit(value):
    value = np.asarray(value, dtype=float)
    length = np.linalg.norm(value)
    if not np.isfinite(length) or length < 1e-10:
        raise ValueError("Degenerate direction")
    return value / length


def vector(value):
    return np.array([value[axis] for axis in "xyz"], dtype=float)


def quaternion(value):
    x, y, z, w = (value[axis] for axis in "xyzw")
    length = math.sqrt(x * x + y * y + z * z + w * w)
    if length < 1e-10:
        raise ValueError("Degenerate rotation")
    x, y, z, w = x / length, y / length, z / length, w / length
    return np.array(
        [
            [1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
            [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
            [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)],
        ]
    )


def transform_matrix(data):
    matrix = np.eye(4)
    matrix[:3, :3] = quaternion(data["m_LocalRotation"]) @ np.diag(
        vector(data["m_LocalScale"])
    )
    matrix[:3, 3] = vector(data["m_LocalPosition"])
    return matrix


def transform_points(matrix, points):
    return np.asarray(points) @ matrix[:3, :3].T + matrix[:3, 3]


def from_to(source, destination):
    a, b = unit(source), unit(destination)
    cosine = np.clip(a @ b, -1, 1)
    if cosine < -1 + 1e-8:
        axis = unit(np.cross(a, np.eye(3)[np.argmin(abs(a))]))
        return 2 * np.outer(axis, axis) - np.eye(3)
    cross = np.cross(a, b)
    skew = np.array(
        [[0, -cross[2], cross[1]], [cross[2], 0, -cross[0]], [-cross[1], cross[0], 0]]
    )
    return np.eye(3) + skew + skew @ skew / (1 + cosine)


@dataclass
class Box:
    """An oriented bounding box with orthonormal column axes."""

    center: np.ndarray
    axes: np.ndarray
    half: np.ndarray

    def corners(self):
        return (
            np.array(list(itertools.product((-1, 1), repeat=3)))
            * self.half
            @ self.axes.T
            + self.center
        )


def box_from_points(points, axes=None):
    axes = np.eye(3) if axes is None else axes
    local = np.asarray(points) @ axes
    low, high = local.min(axis=0), local.max(axis=0)
    return Box(center=((low + high) / 2) @ axes.T, axes=axes, half=(high - low) / 2)


def boxes_clear(a: Box, b: Box, clearance: float) -> bool:
    # Inflation is conservative: a separating plane after inflation proves the gap.
    axes = np.vstack(
        (
            a.axes.T,
            b.axes.T,
            np.cross(a.axes.T[:, None, :], b.axes.T[None, :, :]).reshape(-1, 3),
        )
    )
    lengths = np.linalg.norm(axes, axis=1)
    axes = axes[lengths > 1e-9] / lengths[lengths > 1e-9, None]
    ra = np.abs(axes @ a.axes) @ (a.half + clearance / 2)
    rb = np.abs(axes @ b.axes) @ (b.half + clearance / 2)
    return bool(np.any(np.abs(axes @ (b.center - a.center)) > ra + rb + 1e-7))


def swept_box(points, origin, rotation):
    local = (np.asarray(points) - origin) @ rotation
    radius = np.linalg.norm(local[:, :2], axis=1).max()
    low, high = local[:, 2].min(), local[:, 2].max()
    return Box(
        center=origin + rotation[:, 2] * (low + high) / 2,
        axes=rotation,
        half=np.array([radius, radius, (high - low) / 2]),
    )


def ray_surface(origin, direction, triangles):
    """Return the nearest positive triangle hit and its geometric normal."""
    triangles = np.asarray(triangles)
    edge1, edge2 = triangles[:, 1] - triangles[:, 0], triangles[:, 2] - triangles[:, 0]
    p = np.cross(np.broadcast_to(direction, edge2.shape), edge2)
    determinant = np.einsum("ij,ij->i", edge1, p)
    valid = abs(determinant) > 1e-10
    inverse = np.divide(1, determinant, out=np.zeros_like(determinant), where=valid)
    tvec = origin - triangles[:, 0]
    u = np.einsum("ij,ij->i", tvec, p) * inverse
    q = np.cross(tvec, edge1)
    v = q @ direction * inverse
    distance = np.einsum("ij,ij->i", edge2, q) * inverse
    valid &= (u >= -1e-8) & (v >= -1e-8) & (u + v <= 1 + 1e-8) & (distance > 1e-6)
    if not valid.any():
        return None
    index = np.argmin(np.where(valid, distance, np.inf))
    normal = unit(np.cross(edge1[index], edge2[index]))
    if normal @ direction > 0:
        normal = -normal
    return origin + direction * distance[index], normal


def triangles_intersect_box(triangles, box, tolerance=1e-5):
    """Triangle/box SAT; face-only contact within tolerance is permitted."""
    local = (triangles - box.center) @ box.axes
    half = np.maximum(0, box.half - tolerance)
    broad = np.all(local.max(axis=1) >= -half, axis=1) & np.all(
        local.min(axis=1) <= half, axis=1
    )
    for triangle in local[broad]:
        edges = np.roll(triangle, -1, axis=0) - triangle
        axes = [*np.eye(3), np.cross(edges[0], edges[1])]
        axes.extend(np.cross(edge, axis) for edge in edges for axis in np.eye(3))
        separated = False
        for axis in axes:
            if np.linalg.norm(axis) < 1e-10:
                continue
            projection = triangle @ axis
            radius = abs(axis) @ half
            if projection.min() > radius or projection.max() < -radius:
                separated = True
                break
        if not separated:
            return True
    return False
