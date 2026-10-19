"""Measure full rendered winches in native and runtime-clone coordinate frames."""

import numpy as np

from tools.halyards.geometry import swept_box, transform_points


def control_points(*, boat, control):
    scene = boat.scene
    points = []
    for node in scene.descendants(control):
        mesh = scene.render_mesh(node, boat.frame)
        if mesh is not None:
            points.append(mesh[0])
    if not points:
        raise ValueError(f"No rendered geometry for control {scene.name(control)}")
    return np.concatenate(points)


def control_box(*, boat, control):
    scene = boat.scene
    return swept_box(
        control_points(boat=boat, control=control),
        (boat.frame @ scene.matrix(control))[:3, 3],
        boat.rotation_frame @ scene.rotation(control),
    )


def template_geometry(*, boat, control):
    scene = boat.scene
    # OwnedWinch clones root-local geometry with donor lossyScale and boat rotation.
    native_points = control_points(boat=boat, control=control)
    donor_matrix = boat.frame @ scene.matrix(control)
    local = transform_points(np.linalg.inv(donor_matrix), native_points)
    scale = np.linalg.norm(scene.matrix(control)[:3, :3], axis=0)
    if np.linalg.det(scene.matrix(control)[:3, :3]) <= 0:
        raise ValueError("Reflected control template is unsupported")
    local *= scale
    rotation = boat.rotation_frame @ scene.rotation(control)
    offset = -local[:, 2].min()
    if offset < 0:
        raise ValueError("Control mounting face is in front of its origin")
    return local, rotation, offset
