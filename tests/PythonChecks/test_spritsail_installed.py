"""Verify the native donors/materials used by spritsail construction without exporting assets."""

import pytest

import numpy as np

from tools.halyards.boat import Boat
from tools.halyards.geometry import ray_surface, vector
from tools.halyards.profiles import read_profile
from tools.halyards.scene import identity


def test_spritsail_native_donors(installed_scene):
    scene = installed_scene
    directory = next(
        o for o in scene.native_objects if scene.script(o) == "PrefabsDirectory"
    )
    prefabs = scene.data(directory)["sails"]
    for index, expected in ((110, "brig jib"), (15, "full gaff")):
        go = scene.ptr(directory, prefabs[index])
        sail = scene.component(go, "Sail")
        data = scene.data(sail)
        assert data["prefabIndex"] == index
        assert data["sailName"] == expected
        assert scene.ptr(sail, data["cloth"]) is not None
        if index == 15:
            assert data["installHeight"] == pytest.approx(8.8)
            boom = next(
                t for t in scene.descendants(go) if scene.name(t) == "boom_gaff"
            )
            renderer = scene.component(boom, "MeshRenderer")
            assert (
                scene.ptr(renderer, scene.data(renderer)["m_Materials"][0]) is not None
            )
    assert len(prefabs) <= 404 or scene.ptr(directory, prefabs[404]) is None


def test_junk_mizzen_unreadable_surface(installed_scene):
    scene = installed_scene
    if not scene.has_se:
        pytest.skip("Requires Shipyard Expansion's tall Junk mizzen")
    boat = Boat(scene=scene, profile=read_profile(boat="junk"))
    mast = boat.masts[57]
    mesh_filter = scene.component(mast, "MeshFilter")
    mesh = scene.ptr(mesh_filter, scene.data(mesh_filter)["m_Mesh"])
    assert not scene.data(mesh)["m_IsReadable"]
    walk = scene.ptr(mast, scene.data(mast)["walkColMast"])
    surface = scene.component(walk, "MeshCollider")
    assert not scene.data(surface)["m_Convex"]
    assert identity(scene.ptr(surface, scene.data(surface)["m_Mesh"])) == identity(mesh)

    capsule = scene.data(scene.component(mast, "CapsuleCollider"))
    radius = capsule["m_Radius"]
    assert radius == pytest.approx(0.24)
    center = vector(capsule["m_Center"])
    center[2] = (0.1 - 18.8) * 8.8 / 20.8
    vertices, indices = scene.mesh(mesh)
    radii = []
    for angle in np.arange(32) * np.pi / 16:
        direction = np.array([np.cos(angle), np.sin(angle), 0.0])
        hit = ray_surface(
            center + direction * radius * 2, -direction, vertices[indices]
        )
        assert hit is not None
        radii.append(np.linalg.norm(hit[0] - center))
    # The collider fallback oversizes this shaft by about 44%; surface hits avoid it.
    # Finite angular samples stay within the fitting's 1 mm clearance of the mesh extrema.
    shaft_radius = 0.1667653
    assert shaft_radius - 0.001 < max(radii) <= shaft_radius
    assert radius / max(radii) > 1.4
