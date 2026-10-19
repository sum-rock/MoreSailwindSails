"""Exercise clearance and surface search with synthetic geometry, not game assets."""

import math

import numpy as np
import pytest

from tools.halyards.geometry import (
    Box,
    boxes_clear,
    from_to,
    quaternion,
    ray_surface,
    swept_box,
    transform_matrix,
    transform_points,
    triangles_intersect_box,
)
from tools.halyards.search import search


def cylinder(radius=0.3, height=3, sides=24):
    points = [
        np.array(
            [
                radius * math.cos(i * 2 * math.pi / sides),
                y,
                radius * math.sin(i * 2 * math.pi / sides),
            ]
        )
        for y in (0, height)
        for i in range(sides)
    ]
    triangles = []
    for i in range(sides):
        j = (i + 1) % sides
        triangles.extend(
            [
                [points[i], points[j], points[j + sides]],
                [points[i], points[j + sides], points[i + sides]],
            ]
        )
    return np.array(triangles)


def arguments():
    return dict(
        axis_origin=np.zeros(3),
        axis=np.array([0.0, 1.0, 0.0]),
        surface=cylinder(),
        template_points=np.array([[-0.07, -0.07, -0.05], [0.07, 0.07, 0.05]]),
        template_rotation=np.eye(3),
        template_origin=np.array([0.0, 1.0, 0.35]),
        offset=0.05,
        template_index=0,
        obstacles=[],
        structure=np.empty((0, 3, 3)),
        clearance=0.02,
        height_range=0.5,
    )


def test_surface_search_is_repeatable_and_seated():
    first = search(**arguments())
    second = search(**arguments())
    assert first is not None
    np.testing.assert_array_equal(first.contact, second.contact)
    assert abs(first.contact[1] - 1) < 1e-6
    assert np.linalg.norm(first.normal) == pytest.approx(1)
    assert np.linalg.norm(first.contact[[0, 2]]) == pytest.approx(0.3, abs=1e-6)
    assert np.dot(first.contact[[0, 2]], first.normal[[0, 2]]) > 0


def test_raked_scaled_surface_search():
    args = arguments()
    rotation = quaternion(dict(x=0.0, y=0.0, z=math.sin(0.2), w=math.cos(0.2)))
    shift = np.array([10.0, 2.0, -4.0])
    args["surface"] = (args["surface"] * np.array([1.5, 2.0, 1.5])) @ rotation.T + shift
    args["axis_origin"] = shift
    args["axis"] = rotation[:, 1]
    args["template_origin"] = np.array([0.0, 2.0, 0.5]) @ rotation.T + shift
    seat = search(**args)
    assert seat is not None
    assert (seat.contact - shift) @ args["axis"] == pytest.approx(2, abs=1e-5)
    assert abs(seat.normal @ args["axis"]) < 1e-5


def test_exhaustion_does_not_emit_unsafe_seat():
    args = arguments()
    args["obstacles"] = [Box(np.array([0.0, 1.0, 0.0]), np.eye(3), np.ones(3) * 5)]
    assert search(**args) is None


def test_search_moves_around_obstacle():
    args = arguments()
    baseline = search(**args)
    args["obstacles"] = [Box(baseline.contact, np.eye(3), np.array([0.15, 0.2, 0.15]))]
    seat = search(**args)
    assert seat is not None and np.linalg.norm(seat.contact - baseline.contact) > 0.2
    assert seat.contact[1] == pytest.approx(1)


def test_clearance_uses_shape_and_rotation_sweep():
    box = swept_box(
        np.array([[0.3, 0.0, -0.05], [-0.3, 0.0, 0.05]]), np.zeros(3), np.eye(3)
    )
    assert box.half[1] == pytest.approx(0.3)
    other = Box(np.array([0.0, 0.35, 0.0]), np.eye(3), np.array([0.02, 0.02, 0.02]))
    assert boxes_clear(box, other, 0.02)
    assert not boxes_clear(box, other, 0.04)


def test_static_triangle_inside_box_is_collision():
    triangle = np.array([[[-0.1, 0.0, 0.0], [0.1, 0.0, 0.0], [0.0, 0.1, 0.0]]])
    assert triangles_intersect_box(triangle, Box(np.zeros(3), np.eye(3), np.ones(3)))
    assert not triangles_intersect_box(
        triangle + 5, Box(np.zeros(3), np.eye(3), np.ones(3))
    )


def test_quaternion_opposite_normals_and_transform():
    rotation = from_to(np.array([0.0, 0.0, 1.0]), np.array([0.0, 0.0, -1.0]))
    np.testing.assert_allclose(rotation @ [0, 0, 1], [0, 0, -1], atol=1e-8)
    data = dict(
        m_LocalPosition=dict(x=1, y=2, z=3),
        m_LocalScale=dict(x=2, y=3, z=4),
        m_LocalRotation=dict(x=0, y=0, z=0, w=1),
    )
    np.testing.assert_allclose(
        transform_points(transform_matrix(data), [[1, 1, 1]]), [[3, 5, 7]]
    )


def test_ray_miss_and_nearest_surface():
    surface = cylinder()
    assert (
        ray_surface(np.array([2.0, 4.0, 0.0]), np.array([-1.0, 0.0, 0.0]), surface)
        is None
    )
    contact, normal = ray_surface(
        np.array([2.0, 1.0, 0.0]), np.array([-1.0, 0.0, 0.0]), surface
    )
    assert contact[0] == pytest.approx(0.3)
    assert normal[0] > 0.9
