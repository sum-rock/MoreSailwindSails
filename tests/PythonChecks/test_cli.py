"""Test public command routing, file output and Blender isolation."""

import subprocess
import sys

from click.testing import CliRunner
import pytest

from tools.cli import main
from tools.assets import runner


def test_help_needs_no_blender_or_game():
    result = CliRunner().invoke(main, ["--help"])
    assert result.exit_code == 0
    assert "generate-fallback-halyards" in result.output
    assert "bpy" not in sys.modules


def test_asset_command_passes_cut_and_read_only_flag(monkeypatch, tmp_path):
    calls = []
    monkeypatch.setattr(runner, "run", lambda **kw: calls.append(kw))
    result = CliRunner().invoke(
        main,
        [
            "assets",
            "--blender",
            "custom blender",
            "bake",
            "staysail",
            "--cut",
            "b",
            "--check",
            "--output",
            str(tmp_path / "out.cs"),
        ],
    )
    assert result.exit_code == 0, result.output
    assert calls == [
        dict(
            blender="custom blender",
            action="bake",
            asset="staysail",
            cut="B",
            check=True,
            output=tmp_path / "out.cs",
        )
    ]


def test_blender_arguments_do_not_use_a_shell(monkeypatch, tmp_path):
    calls = []
    monkeypatch.setattr(
        runner.shutil, "which", lambda value: "/path with spaces/blender"
    )
    monkeypatch.setattr(
        runner.subprocess, "run", lambda args, **kw: calls.append((args, kw))
    )
    runner.run(
        blender="blender",
        action="bake",
        asset="flying_sail",
        cut="A",
        output=tmp_path / "with spaces.cs",
        check=True,
    )
    args, kw = calls[0]
    assert args[0] == "/path with spaces/blender" and kw == {"check": True}
    assert "--disable-autoexec" in args and "--check" in args
    assert str(tmp_path / "with spaces.cs") in args


def test_missing_blender_and_failed_worker(monkeypatch):
    monkeypatch.setattr(runner.shutil, "which", lambda value: None)
    result = CliRunner().invoke(main, ["assets", "bake", "stay"])
    assert result.exit_code == 1 and "Blender executable not found" in result.output

    def fail(**kw):
        raise subprocess.CalledProcessError(1, ["blender"])

    monkeypatch.setattr(runner, "run", fail)
    assert CliRunner().invoke(main, ["assets", "bake", "stay"]).exit_code == 1


def test_generator_separates_diagnostics_and_retains_partial_output(
    monkeypatch, tmp_path
):
    from tools.halyards import generate

    def fake(**kwargs):
        kwargs["report"]("Mast 4 unresolved")
        return "new HalyardFallbackDefinition();\n", 1

    monkeypatch.setattr(generate, "generate", fake)
    result = CliRunner().invoke(
        main, ["generate-fallback-halyards", "brig", "--game-dir", str(tmp_path)]
    )
    assert result.exit_code == 1
    assert result.stdout == "new HalyardFallbackDefinition();\n"
    assert "Mast 4 unresolved" in result.stderr
    output = tmp_path / "seats.cs"
    result = CliRunner().invoke(
        main,
        [
            "generate-fallback-halyards",
            "brig",
            "--game-dir",
            str(tmp_path),
            "--output",
            str(output),
        ],
    )
    assert (
        output.read_text() == "new HalyardFallbackDefinition();\n" and not result.stdout
    )


@pytest.mark.parametrize(
    "args",
    [
        ["assets", "bake", "staysail", "--cut", "D"],
        ["assets", "generate", "stay"],
        ["generate-fallback-halyards", "shroud"],
        ["generate-fallback-halyards", "brig", "--clearance", "-1"],
    ],
)
def test_invalid_commands(args):
    assert CliRunner().invoke(main, args).exit_code == 2


def test_missing_installed_assets_are_a_concise_error(tmp_path):
    result = CliRunner().invoke(
        main, ["generate-fallback-halyards", "brig", "--game-dir", str(tmp_path)]
    )
    assert result.exit_code == 1
    assert "Missing installed Sailwind input:" in result.stderr
    assert not result.stdout
