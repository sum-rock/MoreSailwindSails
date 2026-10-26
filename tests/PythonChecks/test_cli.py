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


@pytest.mark.parametrize(
    "prefix, blender",
    [
        (["assets", "--blender", "custom blender", "bake"], "custom blender"),
        (["bake"], "blender"),
    ],
)
def test_asset_command_passes_cut_and_read_only_flag(
    monkeypatch, tmp_path, prefix, blender
):
    calls = []
    monkeypatch.setattr(runner, "run", lambda **kw: calls.append(kw))
    result = CliRunner().invoke(
        main,
        [
            *prefix,
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
            blender=blender,
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


@pytest.mark.parametrize(
    "options, clearance, height_range",
    [([], 0.01, 2.0), (["--clearance", "0.03", "--height-range", "0.6"], 0.03, 0.6)],
)
def test_halyard_search_defaults_and_overrides(
    monkeypatch, tmp_path, options, clearance, height_range
):
    from tools.halyards import generate

    calls = []

    def fake(**kwargs):
        calls.append(kwargs)
        return "", 0

    monkeypatch.setattr(generate, "generate", fake)
    result = CliRunner().invoke(
        main,
        ["generate-fallback-halyards", "brig", "--game-dir", str(tmp_path), *options],
    )
    assert result.exit_code == 0, result.output
    assert len(calls) == 1
    assert calls[0]["clearance"] == clearance
    assert calls[0]["height_range"] == height_range


@pytest.mark.parametrize("options", [[], ["--cut", "a"]])
def test_spritsail_default_and_explicit_cut(monkeypatch, tmp_path, options):
    calls = []
    monkeypatch.setattr(runner, "run", lambda **kw: calls.append(kw))
    output = tmp_path / "spritsail.cs"
    result = CliRunner().invoke(
        main,
        [
            "assets",
            "--blender",
            "custom blender",
            "bake",
            "loose_foot_spritsail",
            *options,
            "--check",
            "--output",
            str(output),
        ],
    )
    assert result.exit_code == 0, result.output
    assert calls == [
        dict(
            blender="custom blender",
            action="bake",
            asset="loose_foot_spritsail",
            cut="A",
            output=output,
            check=True,
        )
    ]


def test_spritsail_worker_uses_catalog(monkeypatch, tmp_path):
    from tools.assets.loose_foot_spritsail_authoring import CUTS, SpritsailCut

    definition = SpritsailCut(
        asset_path=tmp_path / "mkB.blend",
        source_path=tmp_path / "mkB.cs",
        class_name="MkB",
    )
    monkeypatch.setitem(CUTS, "B", definition)
    calls = []
    monkeypatch.setattr(runner.shutil, "which", lambda _: "/blender")
    monkeypatch.setattr(runner.subprocess, "run", lambda args, **kw: calls.append(args))
    runner.run(
        blender="blender",
        action="bake",
        asset="loose_foot_spritsail",
        cut="B",
        output=None,
        check=True,
    )
    args = calls[0]
    assert str(definition.asset_path) in args
    assert args[-3:] == ["--cut", "B", "--check"]
    assert any(a.endswith("bake_loose_foot_spritsail.py") for a in args)


def test_spritsail_rejects_unauthored_cuts():
    from tools.assets.loose_foot_spritsail_authoring import get_cut

    result = CliRunner().invoke(
        main, ["assets", "bake", "loose_foot_spritsail", "--cut", "B"]
    )
    assert result.exit_code == 2
    with pytest.raises(ValueError, match="Unsupported loose footed spritsail cut"):
        get_cut("B")
