"""Evaluate installed mast geometry and emit reviewable C# fallback definitions."""

import hashlib
import math

import numpy as np

from tools.halyards.boat import Boat
from tools.halyards.controls import control_sphere, template_geometry
from tools.halyards.geometry import transform_points, unit, vector
from tools.halyards.profiles import read_profile
from tools.halyards.scene import Scene, identity
from tools.halyards.search import search


def number(value):
    value = float(np.float32(value))
    if not math.isfinite(value):
        raise ValueError("Cannot emit non-finite C# coordinates")
    return format(0 if abs(value) < 1e-10 else value, ".9g") + "f"


def vector_code(value):
    return "new Vector3(" + ", ".join(number(v) for v in value) + ")"


def definition(*, mast_id, seat, source):
    return f"""// {source}
new HalyardFallbackDefinition(
    mast: {mast_id},
    fallback: new HalyardFallbackSeat(
        contact: {vector_code(seat.contact)},
        normal: {vector_code(seat.normal)},
        sourceNormal: {vector_code(seat.source_normal)},
        offset: {number(seat.offset)},
        templateIndex: {seat.template_index}
    )
),
"""


def mast_surface(*, boat, mast):
    scene = boat.scene
    collider = scene.component(mast, "CapsuleCollider")
    if collider is None:
        raise ValueError("No physical mast capsule axis")
    data = scene.data(collider)
    matrix = boat.frame @ scene.matrix(collider)
    axis = unit(matrix[:3, int(data["m_Direction"])])
    origin = transform_points(matrix, vector(data["m_Center"]))
    mesh = scene.render_mesh(mast, boat.frame)
    if mesh is None:
        raise ValueError("No rendered mesh on physical mast")
    return origin, axis, mesh[1]


def structure_meshes(*, boat):
    scene = boat.scene
    meshes = []
    for node in boat.nodes:
        if identity(node) in boat.excluded:
            continue
        renderer = scene.component(node, "MeshRenderer")
        if renderer is None or not scene.data(renderer)["m_Enabled"]:
            continue
        current = node
        skip = False
        while current is not None and identity(current) != identity(boat.root):
            if (
                scene.component(current, "GPButtonRopeWinch") is not None
                or scene.component(current, "Sail") is not None
            ):
                skip = True
                break
            current = scene.parent(current)
        if skip:
            continue
        mesh = scene.render_mesh(node, boat.frame)
        if mesh is not None:
            meshes.append((node, mesh[1]))
    return meshes + boat.extra_structure


def evaluate(*, boat, mast_id, spheres, structures, clearance, height_range):
    scene = boat.scene
    mast = boat.masts[mast_id]
    origin, axis, surface = mast_surface(boat=boat, mast=mast)
    candidates = [
        (index, scene.ptr(mast, reference))
        for index, reference in enumerate(scene.data(mast)["reefWinch"])
        if reference["m_PathID"]
    ]
    if not candidates:
        raise ValueError("No native reef template")
    obstacles = [
        sphere
        for key, sphere in spheres.items()
        if boat.control_compatible(boat.controls[key], mast)
    ] + boat.extra_spheres
    structure = [
        triangles
        for node, triangles in structures
        if identity(scene.transform(node)) != identity(scene.transform(mast))
        and boat.compatible(node, mast)
    ]
    structure = np.concatenate(structure) if structure else np.empty((0, 3, 3))
    relative = surface.reshape(-1, 3) - origin
    mast_radius = np.linalg.norm(
        relative - np.outer(relative @ axis, axis), axis=1
    ).max()
    usable_template = False
    for template_index, template in candidates:
        try:
            points, rotation, offset = template_geometry(boat=boat, control=template)
        except ValueError:
            continue
        usable_template = True
        template_origin = (boat.frame @ scene.matrix(template))[:3, 3]
        # Each template searches its own height against the full compatible geometry.
        search_center = origin + axis * ((template_origin - origin) @ axis)
        reach = (
            height_range
            + np.linalg.norm(points, axis=1).max()
            + mast_radius
            + abs(offset)
            + 0.005  # Include the maximum outward seating correction.
            + clearance
        )
        nearby_obstacles = [
            sphere
            for sphere in obstacles
            if np.all(sphere.center + sphere.radius >= search_center - reach)
            and np.all(sphere.center - sphere.radius <= search_center + reach)
        ]
        nearby_structure = structure
        if len(structure):
            keep = np.all(
                structure.max(axis=1) >= search_center - reach, axis=1
            ) & np.all(structure.min(axis=1) <= search_center + reach, axis=1)
            nearby_structure = structure[keep]
        seat = search(
            axis_origin=origin,
            axis=axis,
            surface=surface,
            template_points=points,
            template_rotation=rotation,
            template_origin=template_origin,
            offset=offset,
            template_index=template_index,
            obstacles=nearby_obstacles,
            structure=nearby_structure,
            clearance=clearance,
            height_range=height_range,
        )
        if seat is not None:
            source = f"{boat.profile.name} mast {mast_id}; template {identity(template)[0]}:{identity(template)[1]}; clearance {clearance:g} m"
            return seat, source
    if not usable_template:
        raise ValueError("No reef template has usable rendered geometry")
    raise ValueError(
        "No clear mast-surface position within the requested height range of any usable reef template"
    )


def generate(*, boat, game_dir, native_only, mast_id, clearance, height_range, report):
    if not all(
        math.isfinite(value) and value >= 0 for value in (clearance, height_range)
    ):
        raise ValueError("Clearance and height range must be finite and nonnegative")
    profile = read_profile(boat)
    report(
        f"Reading {profile.name}: installed Sailwind assets"
        + (" (native only)" if native_only else " and installed SE assets")
    )
    scene = Scene(game_dir=game_dir, include_se=not native_only)
    boat = Boat(scene=scene, profile=profile)
    targets = sorted(index for index, parent in profile.parents.items() if parent == -1)
    if mast_id is not None:
        if mast_id not in targets:
            raise ValueError(
                f"Mast {mast_id} is not a configured physical base mast; choose from {targets}"
            )
        targets = [mast_id]
    if not scene.has_se:
        targets = (
            [index for index in targets if index in boat.masts]
            if mast_id is None
            else targets
        )
    report(
        f"Coverage: {'native + SE' if scene.has_se else 'native'}; physical masts {targets}"
    )
    report(f"Measuring {len(boat.controls)} native control objects")
    spheres = {
        key: control_sphere(boat=boat, control=control)
        for key, control in boat.controls.items()
    }
    structures = structure_meshes(boat=boat)
    command_name = "baghala" if profile.name == "LargeDhow" else profile.name.lower()
    lines = [f"// Generated by mss generate-fallback-halyards {command_name}.\n"]
    for relative in (
        [
            "Sailwind_Data/level24",
            "BepInEx/plugins/ShipyardExpansion/shipyard_expansion.assets",
        ]
        if scene.has_se
        else ["Sailwind_Data/level24"]
    ):
        with (game_dir / relative).open("rb") as stream:
            digest = hashlib.file_digest(stream, "sha256").hexdigest()
        lines.append(f"// {relative} sha256={digest}\n")
    failures = 0
    for index in targets:
        try:
            if index not in boat.masts:
                raise ValueError(
                    "Configured mast is absent from the installed registered options"
                )
            seat, source = evaluate(
                boat=boat,
                mast_id=index,
                spheres=spheres,
                structures=structures,
                clearance=clearance,
                height_range=height_range,
            )
            lines.append(definition(mast_id=index, seat=seat, source=source))
            report(
                f"Mast {index}: clear seat {tuple(round(float(v), 6) for v in seat.contact)}"
            )
        except ValueError as error:
            failures += 1
            report(f"Mast {index}: UNRESOLVED: {error}")
            lines.append(f"// Mast {index}: UNRESOLVED: {error}\n")
    report(f"{len(targets) - failures}/{len(targets)} mast placements resolved")
    return "".join(lines), failures
