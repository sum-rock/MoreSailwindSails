"""Check reference extraction and recovery with synthetic inputs and a fake ILSpy."""

import fcntl
import json
from pathlib import Path
import subprocess

from click.testing import CliRunner
import pytest

from tools import settings
from tools.cli import main
from tools.maintenance import decompile


@pytest.fixture
def extraction(tmp_path, monkeypatch):
    game = tmp_path / "game"
    managed = game / "Sailwind_Data/Managed"
    managed.mkdir(parents=True)
    for name in settings.DECOMPILE_ASSEMBLIES:
        (managed / name).write_bytes(b"synthetic input")
    output = tmp_path / "reference"
    monkeypatch.setattr(settings, "ROOT", tmp_path / "repository")
    monkeypatch.setattr(decompile.shutil, "which", lambda **kwargs: "/fake/ilspycmd")
    state = {"fail": False, "change_input": False}

    def fake_run(*, args, **kwargs):
        if args[-1] == "--version":
            return subprocess.CompletedProcess(
                args=args, returncode=0, stdout="fake ILSpy 1.0\n"
            )
        if state["fail"]:
            return subprocess.CompletedProcess(
                args=args, returncode=1, stdout="fake extraction failed"
            )
        target = Path(args[args.index("--outputdir") + 1])
        (target / "Example.cs").write_text("class Example {}\n")
        if state["change_input"]:
            Path(args[-1]).write_bytes(b"changed input")
        return subprocess.CompletedProcess(
            args=args, returncode=0, stdout="extracted\n"
        )

    monkeypatch.setattr(decompile.subprocess, "run", fake_run)
    return game, output, state


def invoke(game, output):
    return CliRunner().invoke(
        main, ["decompile", "--game-dir", str(game), "--output-dir", str(output)]
    )


def test_refresh_accepts_existing_manifest_and_preserves_notes(extraction):
    game, output, state = extraction
    result = invoke(game, output)
    assert result.exit_code == 0, result.output
    manifest = json.loads((output / "manifest.json").read_text())
    assert manifest["generator"] == "MoreSailwindSails/decompile-sailwind.py"
    assert [a["source_file_count"] for a in manifest["assemblies"]] == [1, 1]
    assert len(manifest["commands"]) == 3
    (output / "notes.txt").write_text("keep")
    (output / "source/obsolete.cs").write_text("old")
    result = invoke(game, output)
    assert result.exit_code == 0, result.output
    assert (output / "notes.txt").read_text() == "keep"
    assert not (output / "source/obsolete.cs").exists()
    assert not list(output.glob(".decompile-*"))


@pytest.mark.parametrize("failure", ["fail", "change_input"])
def test_failed_refresh_preserves_snapshot_and_diagnostics(extraction, failure):
    game, output, state = extraction
    assert invoke(game, output).exit_code == 0
    previous = (output / "manifest.json").read_bytes()
    state[failure] = True
    result = invoke(game, output)
    assert result.exit_code == 1
    assert (output / "manifest.json").read_bytes() == previous
    assert list(output.glob(".decompile-*/decompile.log"))
    assert "diagnostics and recovery files" in result.output


def test_publication_rollback_restores_previous_snapshot(extraction, monkeypatch):
    game, output, state = extraction
    assert invoke(game, output).exit_code == 0
    before = {
        str(p.relative_to(output)): p.read_bytes()
        for p in output.rglob("*")
        if p.is_file()
    }
    original_rename = Path.rename

    def fail_manifest(self, target):
        if self.name == "manifest.json" and self.parent.name.startswith(".decompile-"):
            raise OSError("simulated publication failure")
        return original_rename(self, target=target)

    monkeypatch.setattr(Path, "rename", fail_manifest)
    result = invoke(game, output)
    assert result.exit_code == 1 and "simulated publication failure" in result.output
    assert all(
        (output / name).read_bytes() == content for name, content in before.items()
    )


@pytest.mark.parametrize("kind", ["foreign", "missing", "symlink"])
def test_rejects_unowned_or_symlinked_snapshots(extraction, kind):
    game, output, state = extraction
    output.mkdir()
    if kind == "symlink":
        (output / "source").symlink_to(target=game, target_is_directory=True)
    else:
        (output / "source").mkdir()
        if kind == "foreign":
            (output / "manifest.json").write_text('{"generator": "someone else"}')
    result = invoke(game, output)
    assert result.exit_code == 1
    assert not list(output.glob(".decompile-*"))


def test_rejects_overlapping_output_and_concurrent_refresh(extraction):
    game, output, state = extraction
    assert invoke(game, game / "reference").exit_code == 1
    assert invoke(game, game.parent).exit_code == 1
    output.mkdir()
    with (output / ".decompile.lock").open(mode="a") as lock:
        fcntl.flock(lock.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)
        result = invoke(game, output)
    assert result.exit_code == 1 and "Another extraction" in result.output


def test_missing_game_inputs_are_reported(extraction):
    game, output, state = extraction
    (game / "Sailwind_Data/Managed/Assembly-CSharp.dll").unlink()
    result = invoke(game, output)
    assert (
        result.exit_code == 1 and "Required game assembly is missing" in result.output
    )
