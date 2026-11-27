"""Exercise base-mod removal against temporary installations only."""

from click.testing import CliRunner
import pytest

from tools import settings
from tools.cli import main
from tools.maintenance import uninstall


def invoke(game):
    return CliRunner().invoke(main, ["uninstall", "--game-dir", str(game)])


def test_uninstall_removes_only_base_mod(tmp_path):
    game = tmp_path / "custom game"
    target = game / "BepInEx/plugins/MoreSailwindSails"
    installed = target / "MoreSailwindSails.dll"
    nested = target / "nested/old.dll"
    preserved = [
        game / "BepInEx/plugins/SailwindBenchmark/SailwindBenchmark.dll",
        game / "BepInEx/plugins/OtherMod.dll",
        game / "BepInEx/config/com.august.moresailwindsails.cfg",
        game / "save.dat",
    ]
    for path in [installed, nested, *preserved]:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(b"original")
    result = invoke(game)
    assert result.exit_code == 0, result.output
    assert f"Uninstalled {target}" in result.output
    assert not target.exists()
    assert all(path.read_bytes() == b"original" for path in preserved)
    again = invoke(game)
    assert again.exit_code == 0 and "Not installed" in again.output


def test_missing_plugins_directory_is_reported(tmp_path):
    result = invoke(tmp_path)
    assert result.exit_code == 1 and "plugins directory not found" in result.output
    assert not list(tmp_path.iterdir())


@pytest.mark.parametrize("broken", [False, True])
def test_uninstall_unlinks_without_following_symlinks(tmp_path, broken):
    plugins = tmp_path / "game/BepInEx/plugins"
    plugins.mkdir(parents=True)
    external = tmp_path / "external"
    if not broken:
        external.mkdir()
        (external / "keep.dll").write_bytes(b"original")
    link = plugins / "MoreSailwindSails"
    link.symlink_to(external, target_is_directory=True)
    result = invoke(tmp_path / "game")
    assert result.exit_code == 0, result.output
    assert not link.is_symlink()
    if not broken:
        assert (external / "keep.dll").read_bytes() == b"original"


def test_delete_error_is_concise(tmp_path, monkeypatch):
    target = tmp_path / "BepInEx/plugins/MoreSailwindSails"
    target.mkdir(parents=True)

    def fail(**kwargs):
        raise PermissionError("plugin directory locked")

    monkeypatch.setattr(uninstall.shutil, "rmtree", fail)
    result = invoke(tmp_path)
    assert result.exit_code == 1 and "plugin directory locked" in result.output
    assert target.exists()


def test_install_api_and_uninstall_default():
    cli = CliRunner()
    help_result = cli.invoke(main, ["--help"])
    assert "install-local" not in help_result.output
    assert cli.invoke(main, ["install-local"]).exit_code == 2
    assert cli.invoke(main, ["install", "--help"]).exit_code == 0
    assert uninstall.uninstall.params[0].default == settings.DEFAULT_GAME
    assert "Close Sailwind first" in cli.invoke(main, ["uninstall", "--help"]).output
