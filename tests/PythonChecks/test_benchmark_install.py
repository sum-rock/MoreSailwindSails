"""Exercise benchmark installation only with temporary game trees and mocked builds."""

import click
from click.testing import CliRunner
import pytest

from tools import settings
from tools.cli import main
from tools.performance import install


@pytest.fixture
def fixture(tmp_path, monkeypatch):
    root = tmp_path / "checkout with spaces"
    project = root / "benchmark/SailwindBenchmark.csproj"
    dll = root / "benchmark/bin/Release/netstandard2.0/SailwindBenchmark.dll"
    game = tmp_path / "custom game"
    destination = game / "BepInEx/plugins/SailwindBenchmark/SailwindBenchmark.dll"
    destination.parent.mkdir(parents=True)
    destination.write_bytes(b"installed recorder")
    project.parent.mkdir(parents=True)
    project.write_text("<Project />")
    monkeypatch.setattr(settings, "ROOT", root)
    monkeypatch.setattr(settings, "BENCHMARK_PROJECT_FILE", project)
    monkeypatch.setattr(settings, "BENCHMARK_RELEASE_DLL", dll)
    return root, project, dll, game, destination


def invoke(game):
    return CliRunner().invoke(main, ["performance", "install", "--game-dir", str(game)])


def test_builds_selected_project_then_installs_only_recorder(fixture, monkeypatch):
    root, project, dll, game, destination = fixture
    preserved = [
        game / "BepInEx/plugins/MoreSailwindSails/MoreSailwindSails.dll",
        game / "BepInEx/config/com.august.sailwindbenchmark.cfg",
        game / "BepInEx/benchmark-results/capture.csv",
    ]
    for path in preserved:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(b"untouched")
    calls = []

    def build(**kwargs):
        assert destination.read_bytes() == b"installed recorder"
        calls.append(kwargs)
        dll.parent.mkdir(parents=True)
        dll.write_bytes(b"new recorder")

    monkeypatch.setattr(install, "run", build)
    result = invoke(game)
    assert result.exit_code == 0, result.output
    assert calls == [
        dict(
            arguments=[
                "dotnet",
                "build",
                str(project),
                "-c",
                "Release",
                "-t:Rebuild",
                f"-p:SailwindDir={game}",
            ],
            cwd=root,
        )
    ]
    assert destination.read_bytes() == b"new recorder"
    assert destination.stat().st_mode & 0o777 == 0o644
    assert all(path.read_bytes() == b"untouched" for path in preserved)
    assert f"Installed {destination}" in result.output


@pytest.mark.parametrize("build_fails", [True, False])
def test_build_errors_preserve_installed_recorder(fixture, monkeypatch, build_fails):
    _, _, dll, game, destination = fixture
    if build_fails:
        # A stale output must not be installed after a failed build.
        dll.parent.mkdir(parents=True)
        dll.write_bytes(b"stale")

    def build(**kwargs):
        if build_fails:
            raise click.ClickException("build failed")

    monkeypatch.setattr(install, "run", build)
    result = invoke(game)
    assert result.exit_code == 1
    assert ("build failed" if build_fails else "did not produce") in result.output
    assert destination.read_bytes() == b"installed recorder"


def test_missing_plugins_prevents_build(tmp_path, monkeypatch):
    def unexpected(**kwargs):
        pytest.fail("must not build without BepInEx plugins directory")

    monkeypatch.setattr(install, "run", unexpected)
    result = invoke(tmp_path)
    assert result.exit_code == 1 and "plugins directory not found" in result.output


def test_copy_failure_is_reported(fixture, monkeypatch):
    _, _, dll, game, _ = fixture
    dll.parent.mkdir(parents=True)
    dll.write_bytes(b"new recorder")
    monkeypatch.setattr(install, "run", lambda **kwargs: None)

    def fail(**kwargs):
        raise PermissionError("destination locked")

    monkeypatch.setattr(install.shutil, "copyfile", fail)
    result = invoke(game)
    assert result.exit_code == 1 and "destination locked" in result.output


def test_help_exposes_default_and_close_game_instruction():
    result = CliRunner().invoke(main, ["performance", "install", "--help"])
    assert result.exit_code == 0
    assert "Close Sailwind first" in result.output
    assert "--game-dir" in result.output
    assert install.install.params[0].default == settings.DEFAULT_GAME
    group = CliRunner().invoke(main, ["performance", "--help"])
    assert "install" in group.output and "compare" in group.output
