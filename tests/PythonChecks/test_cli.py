"""Test public command routing, file output and Blender isolation."""

import subprocess
import sys

from click.testing import CliRunner

from tools.cli import main
from tools.assets import runner


def test_help_needs_no_blender_or_game():
    result = CliRunner().invoke(main, ["--help"])
    assert result.exit_code == 0
    assert "assets" in result.output
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
