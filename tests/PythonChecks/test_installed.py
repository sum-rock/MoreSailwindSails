"""Check measured native/SE transforms and registered identities without exporting meshes."""

import numpy as np
import pytest

from tools.halyards.boat import Boat
from tools.halyards.controls import control_box, template_geometry
from tools.halyards.profiles import read_profile
from tools.halyards.scene import identity


def test_brig_native_and_se_reference_frames(installed_scene):
    scene = installed_scene
    boat = Boat(scene=scene, profile=read_profile("brig"))
    mast = boat.masts[5]
    header = mast.parse_monobehaviour_head()
    assert scene.data(mast)["m_Script"] == {
        "m_FileID": header.m_Script.m_FileID,
        "m_PathID": header.m_Script.m_PathID,
    }
    control = scene.ptr(mast, scene.data(mast)["reefWinch"][0])
    # Installed native source, independently measured in the boat frame.
    np.testing.assert_allclose(
        (boat.frame @ scene.matrix(control))[:3, 3], [-0.403, 5.539, -5.52], atol=1e-5
    )
    points, rotation, offset = template_geometry(boat=boat, control=control)
    np.testing.assert_allclose(rotation[:, 2], [-1, 0, 0], atol=1e-5)
    assert offset == pytest.approx(0.0958560035, abs=1e-6)
    assert np.isfinite(points).all()
    if scene.has_se:
        # Native model import rotation/translation must be included for SE coordinates.
        source = boat.masts[56]
        extra = scene.ptr(source, scene.data(source)["reefWinch"][0])
        box = control_box(boat=boat, control=extra)
        np.testing.assert_allclose(
            box.center, [0.36005939, 5.8431, -5.81233134], atol=1e-5
        )
        assert identity(extra) in boat.controls
        # Current SE registers both; the older 0.11.1 fixture marked 74 unregistered.
        assert scene.name(boat.masts[70]) == scene.name(boat.masts[74])
        assert identity(boat.masts[70]) != identity(boat.masts[74])
        assert boat.option(boat.masts[70]) != boat.option(boat.masts[74])


@pytest.mark.parametrize(
    "name", ["junk", "jong", "sanbuq", "baghala", "cog", "kakam", "dhow"]
)
def test_configured_physical_masts_resolve_uniquely(installed_scene, name):
    boat = Boat(scene=installed_scene, profile=read_profile(name))
    if installed_scene.has_se:
        for mast, parent in boat.profile.parents.items():
            if parent == -1:
                assert mast in boat.masts, (name, mast)
    assert len(boat.controls) > 0
