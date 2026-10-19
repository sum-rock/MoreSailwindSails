"""Exercise maintainer workflows with temporary installations and fake processes."""

import subprocess

import click
from click.testing import CliRunner
import pytest

from tools import process, settings
from tools.cli import main
from tools.maintenance import build, install, logs, release


@pytest.fixture
def checkout(tmp_path, monkeypatch):
    root = tmp_path / "checkout"
    (root / "src").mkdir(parents=True)
    (root / "docs").mkdir()
    (root / "src/Plugin.cs").write_text(
        'public const string PluginVersion = "0.3.0";\n'
    )
    (root / "src/MoreSailwindSails.csproj").write_text(
        "<Project><PropertyGroup><Version>0.3.0</Version></PropertyGroup></Project>"
    )
    (root / "docs/RELEASE_NOTES.md").write_text("Release notes\n")
    monkeypatch.setattr(settings, "ROOT", root)
    monkeypatch.setattr(settings, "PLUGIN_SOURCE", root / "src/Plugin.cs")
    monkeypatch.setattr(settings, "PROJECT_FILE", root / "src/MoreSailwindSails.csproj")
    monkeypatch.setattr(settings, "RELEASE_NOTES", root / "docs/RELEASE_NOTES.md")
    monkeypatch.setattr(
        settings,
        "RELEASE_DLL",
        root / "src/bin/Release/netstandard2.0/MoreSailwindSails.dll",
    )
    return root


def test_install_builds_first_against_custom_game(checkout, tmp_path, monkeypatch):
    game = tmp_path / "custom game"
    (game / "BepInEx/plugins").mkdir(parents=True)
    calls = []

    def fake_run(**kwargs):
        calls.append(kwargs)
        dll = checkout / "src/bin/Release/netstandard2.0/MoreSailwindSails.dll"
        dll.parent.mkdir(parents=True)
        dll.write_bytes(b"new build")

    monkeypatch.setattr(build, "run", fake_run)
    result = CliRunner().invoke(main, ["install-local", "--game-dir", str(game)])
    assert result.exit_code == 0, result.output
    destination = game / "BepInEx/plugins/MoreSailwindSails/MoreSailwindSails.dll"
    assert destination.read_bytes() == b"new build"
    assert destination.stat().st_mode & 0o777 == 0o644
    assert calls[0]["cwd"] == checkout
    assert calls[0]["arguments"][-1] == f"-p:SailwindDir={game}"


def test_install_build_failure_preserves_installed_dll(checkout, tmp_path, monkeypatch):
    game = tmp_path / "game"
    destination = game / "BepInEx/plugins/MoreSailwindSails/MoreSailwindSails.dll"
    destination.parent.mkdir(parents=True)
    destination.write_bytes(b"installed")

    def fail(**kwargs):
        raise click.ClickException("build failed")

    monkeypatch.setattr(build, "run", fail)
    result = CliRunner().invoke(main, ["install-local", "--game-dir", str(game)])
    assert result.exit_code == 1 and "build failed" in result.output
    assert destination.read_bytes() == b"installed"


def test_install_rejects_missing_plugins_before_build(checkout, monkeypatch):
    def unexpected(**kwargs):
        pytest.fail("must not build")

    monkeypatch.setattr(install, "rebuild", unexpected)
    result = CliRunner().invoke(main, ["install-local", "--game-dir", str(checkout)])
    assert result.exit_code == 1 and "plugins directory not found" in result.output


@pytest.fixture
def publisher(checkout, monkeypatch):
    calls = []
    state = {"dirty": False, "local_tag": False, "remote_tag": False, "fail": None}

    def fake_run(*, arguments, cwd, **kwargs):
        assert cwd == checkout
        calls.append(tuple(arguments))
        verb = arguments[1] if arguments[0] == "git" else " ".join(arguments[:2])
        if state["fail"] == verb:
            raise click.ClickException(f"failed {verb}")
        output, code = "", 0
        if verb == "status":
            output = " M changed" if state["dirty"] else ""
        elif verb == "rev-parse":
            output = "abcdef\n"
        elif verb == "show-ref":
            code = 0 if state["local_tag"] else 1
        elif verb == "ls-remote":
            output = "existing tag" if state["remote_tag"] else ""
        return subprocess.CompletedProcess(
            args=arguments, returncode=code, stdout=output, stderr=""
        )

    def fake_build(**kwargs):
        calls.append(("build",))
        if state["fail"] == "build":
            raise click.ClickException("failed build")
        return checkout / "built.dll"

    monkeypatch.setattr(release, "run", fake_run)
    monkeypatch.setattr(release, "rebuild", fake_build)
    return calls, state


def test_release_builds_then_confirms_then_publishes(publisher, monkeypatch):
    calls, state = publisher

    def confirm(*args, **kwargs):
        calls.append(("confirm",))

    monkeypatch.setattr(release.click, "confirm", confirm)
    result = CliRunner().invoke(main, ["release"])
    assert result.exit_code == 0, result.output
    assert calls.index(("build",)) < calls.index(("confirm",))
    assert calls.index(("confirm",)) < next(
        i for i, c in enumerate(calls) if c[:2] == ("git", "tag")
    )
    assert next(i for i, c in enumerate(calls) if c[:2] == ("git", "push")) < next(
        i for i, c in enumerate(calls) if c[:2] == ("gh", "release")
    )


@pytest.mark.parametrize("answer", ["n\n", ""])
def test_release_cancel_does_not_tag(publisher, answer):
    calls, state = publisher
    result = CliRunner().invoke(main, ["release"], input=answer)
    assert result.exit_code != 0
    assert ("build",) in calls
    assert not any(
        c[:2] in (("git", "tag"), ("git", "push"), ("gh", "release")) for c in calls
    )


@pytest.mark.parametrize("condition", ["dirty", "local_tag", "remote_tag"])
def test_release_preconditions_prevent_build_and_publication(publisher, condition):
    calls, state = publisher
    state[condition] = True
    result = CliRunner().invoke(main, ["release"], input="y\n")
    assert result.exit_code == 1
    assert ("build",) not in calls
    assert not any(c[:2] == ("git", "tag") for c in calls)


@pytest.mark.parametrize("failure", ["fetch", "gh auth", "build", "push", "gh release"])
def test_release_failure_reports_partial_state(publisher, failure):
    calls, state = publisher
    state["fail"] = failure
    result = CliRunner().invoke(main, ["release"], input="y\n")
    assert result.exit_code == 1
    if failure == "push":
        assert "Local tag v0.3.0 remains" in result.output
        assert not any(c[:2] == ("gh", "release") for c in calls)
    elif failure == "gh release":
        assert "Tag v0.3.0 remains on origin" in result.output
    else:
        assert not any(c[:2] == ("git", "tag") for c in calls)
    assert not any("-d" in c or "--delete" in c for c in calls)


def test_release_rejects_mismatched_versions(checkout, publisher):
    (checkout / "src/Plugin.cs").write_text(
        'public const string PluginVersion = "9.0.0";'
    )
    result = CliRunner().invoke(main, ["release"])
    assert result.exit_code == 1 and "matching" in result.output
    assert ("build",) not in publisher[0]


@pytest.mark.parametrize("name", ["player", "bepinex"])
def test_log_options_and_defaults(name, monkeypatch, tmp_path):
    calls = []
    monkeypatch.setattr(logs, "run", lambda **kwargs: calls.append(kwargs))
    result = CliRunner().invoke(main, ["logs", name])
    assert result.exit_code == 0
    assert calls[0]["arguments"][:5] == ["tail", "-n", "5000", "-F", "--"]
    path = tmp_path / "missing log"
    result = CliRunner().invoke(
        main, ["logs", name, "--path", str(path), "--lines", "0"]
    )
    assert result.exit_code == 0
    assert calls[1]["arguments"] == ["tail", "-n", "0", "-F", "--", str(path)]
    assert CliRunner().invoke(main, ["logs", name, "--lines", "-1"]).exit_code == 2


@pytest.mark.parametrize("missing", [True, False])
def test_process_errors_are_concise(monkeypatch, tmp_path, missing):
    def fake_run(**kwargs):
        if missing:
            raise FileNotFoundError("absent")
        return subprocess.CompletedProcess(
            args=kwargs["args"], returncode=2, stdout="", stderr="failed"
        )

    monkeypatch.setattr(process.subprocess, "run", fake_run)
    with pytest.raises(
        click.ClickException, match="Executable not found" if missing else "status 2"
    ):
        process.run(arguments=["tool"], cwd=tmp_path, capture=True)


@pytest.mark.parametrize(
    "arguments",
    [["install-local"], ["release"], ["decompile"], ["logs"], ["logs", "player"]],
)
def test_maintainer_help(arguments):
    result = CliRunner().invoke(main, [*arguments, "--help"])
    assert result.exit_code == 0, result.output


def test_release_rejects_checkout_changes_during_confirmation(publisher, monkeypatch):
    calls, state = publisher

    def confirm(*args, **kwargs):
        state["dirty"] = True

    monkeypatch.setattr(release.click, "confirm", confirm)
    result = CliRunner().invoke(main, ["release"])
    assert result.exit_code == 1 and "Checkout changed" in result.output
    assert not any(c[:2] == ("git", "tag") for c in calls)


@pytest.mark.parametrize("interrupted", [False, True])
def test_log_failure_and_interrupt(monkeypatch, interrupted):
    def fail(**kwargs):
        if interrupted:
            raise KeyboardInterrupt()
        raise click.ClickException("tail failed")

    monkeypatch.setattr(logs, "run", fail)
    result = CliRunner().invoke(main, ["logs", "player"])
    assert result.exit_code == (130 if interrupted else 1)
    if not interrupted:
        assert "tail failed" in result.output
