"""Sequence checkout preparation, building, confirmation and release publication."""

from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

import click

from tools import settings
from tools.maintenance.build import rebuild
from tools.process import run


@click.command()
def release() -> None:
    """Update master, build Release, then confirm tagging and GitHub publication."""
    run_release()


def run_release() -> None:
    """Run release steps in order, stopping immediately on failure or cancellation."""
    commit = prepare_checkout()
    tag = f"v{version()}"
    validate_tag(tag=tag)
    notes = prepare_publication()
    dll = rebuild()
    confirm_publication(tag=tag, commit=commit, dll=dll)
    verify_checkout(expected_commit=commit)
    push_tag(tag=tag)
    publish_release(tag=tag, dll=dll, notes=notes)


def git(
    *arguments: str, allowed_codes: tuple[int, ...] = (0,)
) -> subprocess.CompletedProcess:
    """Run Git in the configured repository and capture its output."""
    return run(
        arguments=["git", *arguments],
        cwd=settings.ROOT,
        capture=True,
        allowed_codes=allowed_codes,
    )


def prepare_checkout() -> str:
    """Require a clean checkout and fast-forward the release branch to its remote."""
    if git("status", "--porcelain").stdout.strip():
        raise click.ClickException(
            "Working tree is not clean. Commit or remove changes before releasing."
        )
    git("switch", settings.RELEASE_BRANCH)
    git("fetch", settings.RELEASE_REMOTE, settings.RELEASE_BRANCH)
    git("merge", "--ff-only", "FETCH_HEAD")
    remote_head = git("rev-parse", "FETCH_HEAD").stdout.strip()
    if git("rev-parse", "HEAD").stdout.strip() != remote_head:
        raise click.ClickException(
            f"Local {settings.RELEASE_BRANCH} differs from fetched {settings.RELEASE_REMOTE}/{settings.RELEASE_BRANCH}. Reconcile it before releasing."
        )
    return remote_head


def version() -> str:
    """Read and validate the matching plugin and project versions."""
    try:
        plugin_versions = re.findall(
            r'^\s*public\s+const\s+string\s+PluginVersion\s*=\s*"([^"]+)"\s*;',
            settings.PLUGIN_SOURCE.read_text(),
            re.MULTILINE,
        )
        project_versions = [
            element.text
            for element in ET.parse(source=settings.PROJECT_FILE).findall(
                "./PropertyGroup/Version"
            )
        ]
    except (OSError, ET.ParseError) as error:
        raise click.ClickException(str(error)) from error
    if len(plugin_versions) != 1 or project_versions != plugin_versions:
        raise click.ClickException(
            "Expected exactly one matching PluginVersion and project Version."
        )
    return plugin_versions[0]


def validate_tag(*, tag: str) -> None:
    """Require a valid tag name that is unused locally and on the remote."""
    reference = f"refs/tags/{tag}"
    git("check-ref-format", reference)
    local_tag = git("show-ref", "--verify", "--quiet", reference, allowed_codes=(0, 1))
    if local_tag.returncode == 0:
        raise click.ClickException(f"Tag already exists locally: {tag}")
    remote_tag = git(
        "ls-remote", "--tags", "--refs", settings.RELEASE_REMOTE, reference
    )
    if remote_tag.stdout.strip():
        raise click.ClickException(
            f"Tag already exists on {settings.RELEASE_REMOTE}: {tag}"
        )


def prepare_publication() -> Path:
    """Check release notes and GitHub authentication before building."""
    notes = settings.RELEASE_NOTES
    if not notes.is_file():
        raise click.ClickException(f"Release notes not found: {notes}")
    run(arguments=["gh", "auth", "status"], cwd=settings.ROOT, capture=True)
    return notes


def confirm_publication(*, tag: str, commit: str, dll: Path) -> None:
    """Present the built release and request confirmation before publishing."""
    click.echo(
        f"Release {tag} at {commit}: tag and push to {settings.RELEASE_REMOTE}, then publish {dll} on GitHub."
    )
    click.confirm("Publish this release?", default=False, abort=True)


def verify_checkout(*, expected_commit: str) -> None:
    """Reject changes to the checkout during the build or confirmation prompt."""
    if (
        git("status", "--porcelain").stdout.strip()
        or git("rev-parse", "HEAD").stdout.strip() != expected_commit
    ):
        raise click.ClickException(
            "Checkout changed during release preparation; nothing was published."
        )


def push_tag(*, tag: str) -> None:
    """Create and push the annotated tag, retaining it if the push fails."""
    reference = f"refs/tags/{tag}"
    git("tag", "-a", tag, "-m", f"Release {tag}")
    try:
        git("push", settings.RELEASE_REMOTE, f"{reference}:{reference}")
    except click.ClickException as error:
        raise click.ClickException(
            f"Tag push failed: {error}. Local tag {tag} remains; check {settings.RELEASE_REMOTE} before retrying."
        ) from error
    click.echo(f"Pushed release tag {tag} to {settings.RELEASE_REMOTE}.")


def publish_release(*, tag: str, dll: Path, notes: Path) -> None:
    """Publish the built DLL and release notes against the pushed tag."""
    try:
        run(
            arguments=[
                "gh",
                "release",
                "create",
                tag,
                str(dll),
                "--verify-tag",
                "--notes-file",
                str(notes),
            ],
            cwd=settings.ROOT,
        )
    except click.ClickException as error:
        raise click.ClickException(
            f"GitHub publication failed: {error}. Tag {tag} remains on {settings.RELEASE_REMOTE}; check GitHub before retrying publication."
        ) from error
