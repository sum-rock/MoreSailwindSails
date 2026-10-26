"""Verify the native donors/materials used by spritsail construction without exporting assets."""

import pytest


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
