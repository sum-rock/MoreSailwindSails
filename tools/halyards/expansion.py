"""Account for installed SE 0.12.1 geometry changes beyond serialized prefab data."""

import math
import numpy as np

from tools.halyards.controls import control_points
from tools.halyards.geometry import sphere_from_points, transform_points
from tools.halyards.scene import identity


def adapt(*, boat):
    """Keep native originals conservatively and add code-created control obstacles."""
    scene = boat.scene
    name = boat.profile.name
    # These controls are appended in SE's per-boat code, not in serialized mast arrays.
    additions = {
        "Brig": [
            (7, "rope_winch_mizzen0_reef (2)"),
            (6, "rope_winch_mizzen1_reef (2)"),
            (7, "rope_winch_mizzen_angle (4)"),
            (6, "rope_winch_mizzen_angle (4)"),
        ],
        "Jong": [
            (2, "winch_front_reef_extra"),
            (2, "winch_angle_main_front (3)"),
            (3, "winch_back_reef_extra"),
            (3, "winch_angle_main_back (3)"),
            (9, "winch_stay_main_reef (1)"),
            (8, "winch_stay_main_reef (1)"),
            (9, "winch_stay_front_left (3)"),
            (8, "winch_stay_front_left (3)"),
            (9, "winch_stay_front_right (3)"),
            (8, "winch_stay_front_right (3)"),
        ],
        "Sanbuq": [
            (13, "rope_winch_extension0_reef (1)"),
            (14, "rope_winch_extension1_reef (1)"),
        ],
    }
    for mast_id, path in additions.get(name, []):
        control = scene.component(
            scene.find(boat.se, "winches/" + path), "GPButtonRopeWinch"
        )
        key = identity(control)
        boat.controls[key] = control
        boat.usages.setdefault(key, set()).add(boat.option(boat.masts[mast_id]))
    if name == "Kakam":
        holder = scene.find(boat.model, "structure/mast_001/mast_holder_001")
        scene.set_local(
            holder,
            m_LocalPosition=dict(x=-1.29, y=0.0, z=-10.12),
            m_LocalScale=dict(x=0.87, y=0.87, z=0.94),
        )
        guide = scene.find(boat.model, "structure/mast/rope_holder_003")
        scene.set_local(
            guide,
            m_LocalPosition=dict(x=-3.0, y=0.0, z=-11.3),
            m_LocalRotation=dict(x=-math.sqrt(0.5), y=0.0, z=0.0, w=math.sqrt(0.5)),
        )
    if name == "Dhow":
        # SE duplicates the short mast's existing holder onto the taller mast.
        holder = scene.find(boat.model, "rope_holder_001")
        short = scene.find(boat.model, "mast")
        tall = scene.find(boat.model, "mast_taller")
        original = boat.frame @ scene.matrix(holder)
        local = np.linalg.inv(boat.frame @ scene.matrix(short)) @ original
        local[:3, 3] += (0.0, 0.0, 0.3)
        moved = boat.frame @ scene.matrix(tall) @ local
        for node in scene.descendants(holder):
            mesh = scene.render_mesh(node, boat.frame)
            if mesh is not None:
                triangles = transform_points(moved @ np.linalg.inv(original), mesh[1])
                boat.extra_structure.append((holder, triangles))
    if name == "Junk":
        for mast_name in ("mast_mid_0", "mast_mid_1", "mast_mizzen_1", "mast_front_"):
            target = scene.component(
                scene.find(boat.model, "structure/" + mast_name), "MeshFilter"
            )
            source = scene.component(
                scene.find(boat.se, "mast_meshes/" + mast_name), "MeshFilter"
            )
            # Store a reference to the source object, not a cross-file numeric pointer.
            scene.mesh_overrides[identity(target)] = scene.ptr(
                source, scene.data(source)["m_Mesh"]
            )
        for node in scene.descendants(scene.find(boat.se, "mast_meshes")):
            boat.excluded.add(identity(node))
        # The native 1-1 stay no longer requires mizzen 2 specifically.
        option = scene.component(
            scene.find(boat.model, "midstay_1-1"), "BoatPartOption"
        )
        mizzen = scene.component(
            scene.find(boat.model, "structure/mast_mizzen_1"), "BoatPartOption"
        )
        boat.options.requires[identity(option)].discard(identity(mizzen))
        for path in ("structure/mast_mizzen_0", "structure/mast_mizzen_(empty)"):
            boat.options.disabled[identity(option)].add(
                identity(
                    scene.component(scene.find(boat.model, path), "BoatPartOption")
                )
            )
    if name == "Sanbuq":
        for path, left, right, angle_l, angle_r in (
            (
                "forestay_0_extension_long",
                (-0.2, -0.2, -0.17),
                (-0.2, -0.2, 0.13),
                186,
                356,
            ),
            (
                "forestay_1_extension_long",
                (-0.2, -0.2, -0.15),
                (-0.2, -0.2, 0.14),
                180,
                0,
            ),
        ):
            mast = scene.component(scene.find(boat.model, path), "Mast")
            for role, delta, angle in [
                ("reefWinch", (0, 1, 0), None),
                ("leftAngleWinch", left, angle_l),
                ("rightAngleWinch", right, angle_r),
            ]:
                for reference in scene.data(mast)[role]:
                    control = scene.ptr(mast, reference)
                    if control is None:
                        continue
                    original = boat.frame @ scene.matrix(control)
                    parent = boat.frame @ scene.matrix(scene.parent(control))
                    local = np.linalg.inv(parent) @ original
                    local[:3, 3] += delta
                    if angle is not None:
                        radians = math.radians(angle)
                        turn = np.array(
                            [
                                [math.cos(radians), 0, math.sin(radians)],
                                [0, 1, 0],
                                [-math.sin(radians), 0, math.cos(radians)],
                            ]
                        )
                        local[:3, :3] = turn @ np.diag(
                            np.linalg.norm(local[:3, :3], axis=0)
                        )
                    moved = parent @ local
                    points = transform_points(
                        moved @ np.linalg.inv(original),
                        control_points(boat=boat, control=control),
                    )
                    boat.extra_spheres.append(
                        sphere_from_points(points=points, origin=moved[:3, 3])
                    )
