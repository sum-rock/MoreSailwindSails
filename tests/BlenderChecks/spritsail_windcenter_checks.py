"""Check authored sensor transforms and invalid markers without saving the source asset."""

from contextlib import contextmanager
import math

import bpy
from mathutils import Euler, Quaternion, Vector


@contextmanager
def edit_marker(marker):
    name = marker.name
    location = marker.location.copy()
    rotation = marker.rotation_euler.copy()
    scale = marker.scale.copy()
    try:
        yield marker
    finally:
        marker.name = name
        marker.parent = None
        marker.constraints.clear()
        marker.animation_data_clear()
        marker.location = location
        marker.rotation_euler = rotation
        marker.scale = scale
        bpy.context.view_layer.update()


def check(*, tool, data, cut):
    sail = bpy.data.objects["Sail"]
    conversion = tool["CONVERSION"]
    has_top = cut in ("C", "D")
    assert ("WindCenterMain" in data) == has_top
    names = ("WindCenter", "WindCenterMain") if has_top else ("WindCenter",)
    for name in names:
        marker = bpy.data.objects[name]
        pose = data[name]
        position = data["Sail"]["RestTransform"] @ Vector(pose["Position"])
        assert (position - conversion @ marker.matrix_world.translation).length < 1e-5
        x, y, z, w = pose["Rotation"]
        rotation = Quaternion((w, x, y, z))
        assert abs(rotation.magnitude - 1) < 1e-6
        for axis, expected in (
            ((1, 0, 0), (0, 0, -1)),
            ((0, 1, 0), (-1, 0, 0)),
            ((0, 0, 1), (0, 1, 0)),
        ):
            assert (rotation @ Vector(axis) - Vector(expected)).length < 1e-6

        # Export authored translations and rotations, rather than hard-coded defaults.
        with edit_marker(marker):
            marker.location += Vector((1, 2, 3))
            marker.rotation_euler = Euler((0.2, 0.4, -0.8))
            bpy.context.view_layer.update()
            changed = tool["windcenter"](name)
            assert changed != pose
            x, y, z, w = changed["Rotation"]
            rotation = Quaternion((w, x, y, z))
            local = (
                sail.matrix_world.to_quaternion().inverted()
                @ marker.matrix_world.to_quaternion()
            )
            for axis, sign in (((1, 0, 0), 1), ((0, 1, 0), -1), ((0, 0, 1), 1)):
                expected = conversion.to_3x3() @ (local @ Vector(axis)) * sign
                assert (rotation @ Vector(axis) - expected).length < 1e-6
            expected = conversion @ (
                sail.matrix_world.inverted() @ marker.matrix_world.translation
            )
            assert (Vector(changed["Position"]) - expected).length < 1e-6

        edits = (
            lambda obj: setattr(obj, "name", "MissingWindCenter"),
            lambda obj: setattr(obj, "parent", sail),
            lambda obj: setattr(obj, "scale", (2, 1, 1)),
            lambda obj: setattr(obj, "scale", (-1, 1, 1)),
            lambda obj: setattr(obj.location, "x", math.nan),
            lambda obj: obj.animation_data_create(),
            lambda obj: obj.constraints.new(type="COPY_LOCATION"),
        )
        for index, edit in enumerate(edits):
            with edit_marker(marker):
                edit(marker)
                bpy.context.view_layer.update()
                try:
                    tool["collect_cut"](cut=cut)
                except ValueError as error:
                    assert name in str(error), str(error)
                else:
                    raise AssertionError(f"Accepted invalid {name} edit {index}")
        assert tool["windcenter"](name) == pose
    if has_top:
        assert data["WindCenterMain"]["Position"][1] < data["WindCenter"]["Position"][1]
        assert data["WindCenterMain"]["Rotation"] == data["WindCenter"]["Rotation"]
    print(
        f"PASS: Mk.{cut} windcenter positions, sensor axes, authored edits and invalid-marker rejection."
    )
