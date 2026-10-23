"""Validate transitive shipyard prerequisites and the existing profile contract."""

from tools.halyards.options import Options
from tools.halyards.profiles import PROFILES, read_profile


def test_transitive_groups_and_disabled_options():
    options = Options()
    options.groups = {"short": 0, "tall": 0, "top": 1, "stay": 2}
    options.requires = {"stay": {"top"}, "top": {"tall"}}
    assert options.compatible("stay", "tall")
    assert not options.compatible("stay", "short")
    options.disabled = {"stay": {"tall"}}
    assert not options.compatible("stay")


def test_dependency_cycle_terminates_and_checks_conflicts():
    options = Options()
    options.requires = {"a": {"b"}, "b": {"a"}}
    assert options.compatible("a")
    options.disabled = {"b": {"a"}}
    assert not options.compatible("a")


def test_all_boat_profiles_parse_and_topmasts_share_base():
    for boat in PROFILES:
        profile = read_profile(boat)
        assert profile.boat_name.startswith("BOAT ")
        assert any(parent == -1 for parent in profile.parents.values())
    assert read_profile("brig").parents[56] == 5
    assert read_profile("baghala") == read_profile("large_dhow")
