"""Rebuild the plugin for manual installation or release publication."""

from pathlib import Path

import click

from tools import settings
from tools.process import run


def rebuild(*, game_dir: Path | None = None) -> Path:
    arguments = [
        "dotnet",
        "build",
        str(settings.PROJECT_FILE),
        "-c",
        "Release",
        "-t:Rebuild",
    ]
    if game_dir is not None:
        arguments.append(f"-p:SailwindDir={game_dir}")
    run(arguments=arguments, cwd=settings.ROOT)
    dll = settings.RELEASE_DLL
    if not dll.is_file():
        raise click.ClickException(f"Build did not produce the Release DLL: {dll}")
    return dll
