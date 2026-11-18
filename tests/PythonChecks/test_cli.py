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
            asset="staysail",
            cut="B",
            check=True,
            output=tmp_path / "out.cs",
        )
    ]


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
            asset="loose_foot_spritsail",
            cut="A",
            output=output,
            check=True,
        )
    ]


def test_spritsail_rejects_unauthored_cuts():
    from tools.assets.loose_foot_spritsail.authoring import get_cut

    result = CliRunner().invoke(
        main, ["assets", "bake", "loose_foot_spritsail", "--cut", "E"]
    )
    assert result.exit_code == 2
    with pytest.raises(ValueError, match="Unsupported loose footed spritsail cut"):
        get_cut("E")


def test_asset_generation_is_removed():
    cli = CliRunner()
    assert "generate" not in cli.invoke(main, ["assets", "--help"]).output
    for asset in ("flying_sail", "staysail"):
        result = cli.invoke(main, ["assets", "generate", asset])
        assert result.exit_code == 2
        assert "No such command 'generate'" in result.output


CASES = (
    [("flying_sail", "A"), ("stay", "A")]
    + [("staysail", c) for c in "ABC"]
    + [("loose_foot_spritsail", c) for c in "ABCD"]
)


@pytest.mark.parametrize("asset,cut", CASES)
def test_private_json_handoff(monkeypatch, tmp_path, asset, cut):
    from pathlib import Path
    import json

    calls = []
    monkeypatch.setattr(runner.shutil, "which", lambda _: "/path with spaces/blender")
    data = {"precise": 0.12345678912345678, "poses": [[1, 2], [1, 2]]}

    def extract(args, **kw):
        calls.append((args, kw))
        Path(args[-1]).write_text(json.dumps(data))

    def render(**kwargs):
        assert kwargs == dict(asset=asset, cut=cut, data=data)
        return "generated source"

    monkeypatch.setattr(runner.subprocess, "run", extract)
    monkeypatch.setattr(runner.rendering, "bake", render)
    output = tmp_path / "out.cs"
    runner.run(blender="blender", asset=asset, cut=cut, output=output, check=False)
    args, kw = calls[0]
    assert kw == {"check": True} and args[0] == "/path with spaces/blender"
    assert str(runner.paths(asset=asset, cut=cut)[0]) in args
    assert args[args.index("--python") + 1].endswith("/extract.py")
    assert not Path(args[-1]).exists()
    assert output.read_text() == "generated source"
    runner.run(blender="blender", asset=asset, cut=cut, output=output, check=True)
    output.write_text("stale")
    with pytest.raises(ValueError, match="Stale"):
        runner.run(blender="blender", asset=asset, cut=cut, output=output, check=True)
    assert output.read_text() == "stale" and not Path(calls[-1][0][-1]).exists()
    assert "bpy" not in sys.modules


@pytest.mark.parametrize("failure", ["extraction", "rendering", "json", "nan"])
def test_failure_preserves_destination_and_cleans_handoff(
    monkeypatch, tmp_path, failure
):
    from pathlib import Path

    calls = []
    monkeypatch.setattr(runner.shutil, "which", lambda _: "/blender")

    def extract(args, **kw):
        calls.append(Path(args[-1]))
        if failure == "extraction":
            raise subprocess.CalledProcessError(1, args)
        calls[-1].write_text(
            "invalid"
            if failure == "json"
            else '{"x": NaN}'
            if failure == "nan"
            else "{}"
        )

    def render(**kwargs):
        raise ValueError("render failure")

    monkeypatch.setattr(runner.subprocess, "run", extract)
    monkeypatch.setattr(runner.rendering, "bake", render)
    output = tmp_path / "out.cs"
    output.write_text("original")
    with pytest.raises((ValueError, subprocess.CalledProcessError)):
        runner.run(blender="blender", asset="stay", cut="A", output=output, check=False)
    assert output.read_text() == "original" and not calls[0].parent.exists()


def test_intermediate_format_has_no_public_commands():
    cli = CliRunner()
    assert cli.invoke(main, ["assets", "export"]).exit_code == 2
    assert (
        cli.invoke(main, ["bake", "stay", "--usd-input", "example.usdc"]).exit_code == 2
    )
