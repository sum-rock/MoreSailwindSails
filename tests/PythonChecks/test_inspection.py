"""Verify boat inventory output and CLI behavior without installed game assets."""

import subprocess
import sys
from types import SimpleNamespace

from click.testing import CliRunner
import pytest

from tools.cli import main


@pytest.mark.parametrize(
    "name,native_only,profile_name",
    [("BRIG", False, "Brig"), ("large_DHOW", True, "LargeDhow")],
)
def test_inventory_sorting_labels_and_options(
    monkeypatch, tmp_path, name, native_only, profile_name
):
    from tools.halyards import boat, scene

    mast = SimpleNamespace(assets_file=SimpleNamespace(name="level24"), path_id=101)
    stay = SimpleNamespace(assets_file=SimpleNamespace(name="se-assets"), path_id=202)
    option = {"optionName": "Middle top stay"}
    reader = SimpleNamespace(
        component=lambda *, obj, kind: option if obj is stay else None,
        data=lambda *, obj: obj,
        name=lambda *, obj: "Foremast",
    )

    def load_scene(*, game_dir, include_se):
        assert game_dir == tmp_path
        assert include_se is not native_only
        return reader

    def load_boat(*, scene, profile):
        assert scene is reader
        assert profile.name == profile_name
        return SimpleNamespace(masts={15: stay, 3: mast})

    monkeypatch.setattr(scene, "Scene", load_scene)
    monkeypatch.setattr(boat, "Boat", load_boat)
    arguments = ["inspect-boat", name, "--game-dir", str(tmp_path)]
    if native_only:
        arguments.append("--native-only")
    result = CliRunner().invoke(main, arguments)

    assert result.exit_code == 0, result.output
    assert not result.stderr
    lines = result.stdout.splitlines()
    assert len(lines) == 3
    assert "Index" in lines[0] and "Object ID" in lines[0]
    assert lines[1].split() == ["3", "Foremast", "level24", "101"]
    assert lines[2].split() == ["15", "Middle", "top", "stay", "se-assets", "202"]
    assert not list(tmp_path.iterdir())


@pytest.mark.parametrize(
    "error",
    [
        ValueError("Ambiguous registered mast 5: Main mast"),
        OSError("Cannot read installed asset"),
    ],
)
def test_reader_errors_are_concise(monkeypatch, tmp_path, error):
    from tools.halyards import boat, scene

    monkeypatch.setattr(scene, "Scene", lambda **kwargs: object())

    def fail(**kwargs):
        raise error

    monkeypatch.setattr(boat, "Boat", fail)
    result = CliRunner().invoke(
        main, ["inspect-boat", "brig", "--game-dir", str(tmp_path)]
    )
    assert result.exit_code == 1
    assert not result.stdout
    assert result.stderr == f"Error: {error}\n"


def test_missing_assets_are_reported(tmp_path):
    result = CliRunner().invoke(
        main, ["inspect-boat", "brig", "--game-dir", str(tmp_path)]
    )
    assert result.exit_code == 1
    assert not result.stdout
    assert "Missing installed Sailwind input:" in result.stderr


def test_unsupported_boat_is_rejected():
    result = CliRunner().invoke(main, ["inspect-boat", "shroud"])
    assert result.exit_code == 2
    assert "Invalid value for" in result.stderr


def test_help_does_not_import_asset_readers():
    result = subprocess.run(
        args=[
            sys.executable,
            "-c",
            "from tools.cli import main; "
            "from click.testing import CliRunner; "
            "import sys; "
            "result = CliRunner().invoke(main, ['inspect-boat', '--help']); "
            "assert result.exit_code == 0, result.output; "
            "assert '--native-only' in result.output; "
            "assert '--game-dir' in result.output; "
            "assert 'tools.halyards.scene' not in sys.modules; "
            "assert 'tools.halyards.boat' not in sys.modules; "
            "assert 'UnityPy' not in sys.modules",
        ],
        capture_output=True,
        text=True,
        check=False,
    )
    assert result.returncode == 0, result.stderr
