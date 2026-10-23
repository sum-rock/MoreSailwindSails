"""Exercise clearance and surface search with synthetic geometry, not game assets."""

import math

import numpy as np
import pytest

from tools.halyards.geometry import (
    Box,
    Sphere,
    spheres_clear,
    sphere_from_points,
    from_to,
    quaternion,
    ray_surface,
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
    args["obstacles"] = [Sphere(center=np.array([0.0, 1.0, 0.0]), radius=5)]
    assert search(**args) is None


def test_search_moves_around_obstacle():
    args = arguments()
    baseline = search(**args)
    args["obstacles"] = [Sphere(center=baseline.contact, radius=0.2)]
    seat = search(**args)
    assert seat is not None and np.linalg.norm(seat.contact - baseline.contact) > 0.2
    assert seat.contact[1] == pytest.approx(1)


def test_mesh_radius_uses_origin_and_full_three_dimensional_extent():
    origin = np.array([10.0, 20.0, 30.0])
    sphere = sphere_from_points(
        points=np.array([[0.03, 0.04, 0.12], [-0.1, 0, 0]]) + origin,
        origin=origin,
    )
    np.testing.assert_allclose(sphere.center, origin)
    assert sphere.radius == pytest.approx(0.13)


def test_radius_clearance_includes_both_winches_and_gap():
    a = Sphere(center=np.zeros(3), radius=0.125)
    b = Sphere(center=np.array([0.25, 0, 0]), radius=0.125)
    assert spheres_clear(a=a, b=b, clearance=0)
    assert not spheres_clear(a=a, b=b, clearance=0.01)
    b.center[0] = 0.26
    assert spheres_clear(a=a, b=b, clearance=0.01)
    b.center[0] = 0.259
    assert not spheres_clear(a=a, b=b, clearance=0.01)


def test_diagonal_gap_does_not_reserve_square_box_corners():
    a = Sphere(center=np.zeros(3), radius=0.1)
    b = Sphere(center=np.array([0.16, 0.16, 0]), radius=0.1)
    assert spheres_clear(a=a, b=b, clearance=0.01)


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


@pytest.mark.parametrize(
    "height_range, opening, expected",
    [
        (2.0, 1.0, True),
        (2.0, 1.02, True),
        (2.0, 3.0, True),
        (2.0, 0.98, False),
        (0.0, 1.0, True),
        (0.0, 1.02, False),
        (0.03, 1.04, False),
    ],
)
def test_search_height_levels_are_upward_in_two_cm_steps(
    monkeypatch, height_range, opening, expected
):
    import importlib

    search_module = importlib.import_module("tools.halyards.search")
    heights = []

    def surface_with_opening(origin, direction, triangles):
        heights.append(float(origin[1]))
        if abs(origin[1] - opening) > 1e-8:
            return None
        return ray_surface(origin=origin, direction=direction, triangles=triangles)

    monkeypatch.setattr(search_module, "ray_surface", surface_with_opening)
    args = arguments()
    args["height_range"] = height_range
    args["clearance"] = 0.01
    result = search(**args)
    assert (result is not None) == expected
    if expected:
        assert result.contact[1] == pytest.approx(opening)
    levels = np.unique(heights)
    last = opening if expected else 1 + math.floor(height_range / 0.02 + 1e-8) * 0.02
    np.testing.assert_allclose(
        levels, 1 + np.arange(round((last - 1) / 0.02) + 1) * 0.02
    )


@pytest.mark.parametrize("required_mm", [0, 1, 3, 5, 6])
def test_outward_retry_uses_smallest_clear_offset(monkeypatch, required_mm):
    import importlib

    module = importlib.import_module("tools.halyards.search")
    args = arguments()
    args["height_range"] = 0
    calls = []
    # Isolate one surface candidate so failed retries cannot escape to another angle.
    hits = iter([(np.array([0.0, 1.0, -0.3]), np.array([0.0, 0.0, -1.0]))])
    monkeypatch.setattr(module, "ray_surface", lambda *args: next(hits, None))

    def intersects(triangles, box):
        displacement = -box.center[2] - 0.3
        calls.append(displacement)
        return displacement < args["offset"] + required_mm * 0.001 - 1e-8

    monkeypatch.setattr(module, "triangles_intersect_box", intersects)
    result = search(**args)
    if required_mm <= 5:
        assert result is not None
        expected = float(np.float32(args["offset"] + required_mm * 0.001))
        assert result.offset == expected
        assert calls[-1] == pytest.approx(expected, abs=2e-8)
    else:
        assert result is None
    assert len(calls) == min(required_mm + 1, 6)
    assert np.diff(calls) == pytest.approx(np.full(len(calls) - 1, 0.001), abs=2e-8)


@pytest.mark.parametrize(
    "blocker", ["initial winch", "retry winch", "structure", "structure only"]
)
def test_outward_retry_rechecks_obstacles_and_requires_mast_collision(
    monkeypatch, blocker
):
    import importlib

    module = importlib.import_module("tools.halyards.search")
    args = arguments()
    args["height_range"] = 0
    args["obstacles"] = [Sphere(center=np.zeros(3), radius=0.1)]
    args["structure"] = np.ones((1, 3, 3))
    hits = iter([(np.array([0.0, 1.0, -0.3]), np.array([0.0, 0.0, -1.0]))])
    monkeypatch.setattr(module, "ray_surface", lambda *args: next(hits, None))
    attempted = []

    def clear(*, a, b, clearance):
        delta = -a.center[2] - 0.3 - args["offset"]
        attempted.append(delta)
        return blocker != "initial winch" and not (
            blocker == "retry winch" and delta > 0.0005
        )

    def intersects(triangles, box):
        delta = -box.center[2] - 0.3 - args["offset"]
        if triangles is args["surface"]:
            return blocker != "structure only" and delta < 0.0005
        return blocker in ("structure", "structure only")

    monkeypatch.setattr(module, "spheres_clear", clear)
    monkeypatch.setattr(module, "triangles_intersect_box", intersects)
    assert search(**args) is None
    assert len(attempted) == (
        1 if blocker in ("initial winch", "structure only") else 6
    )
