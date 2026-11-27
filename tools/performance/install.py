"""Build and install the optional recorder independently of the MSS plugin."""

from pathlib import Path
import shutil

import click

from tools import settings
from tools.process import run


@click.command()
@click.option(
    "--game-dir",
    type=click.Path(path_type=Path, file_okay=False),
    default=settings.DEFAULT_GAME,
    show_default=True,
)
def install(game_dir: Path) -> None:
    """Rebuild and install SailwindBenchmark in Release. Close Sailwind first.

    Replaces BepInEx/plugins/SailwindBenchmark/SailwindBenchmark.dll in the
    selected installation. Does not install MSS or change benchmark settings.
    Run inside nix develop.
    """
    game_dir = game_dir.expanduser().resolve()
    plugins = game_dir / settings.PLUGINS_DIRECTORY
    if not plugins.is_dir():
        raise click.ClickException(f"BepInEx plugins directory not found: {plugins}")
    run(
        arguments=[
            "dotnet",
            "build",
            str(settings.BENCHMARK_PROJECT_FILE),
            "-c",
            "Release",
            "-t:Rebuild",
            f"-p:SailwindDir={game_dir}",
        ],
        cwd=settings.ROOT,
    )
    dll = settings.BENCHMARK_RELEASE_DLL
    if not dll.is_file():
        raise click.ClickException(f"Build did not produce the Release DLL: {dll}")
    destination = plugins / settings.BENCHMARK_DIRECTORY / dll.name
    try:
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(src=dll, dst=destination)
        destination.chmod(mode=0o644)
    except OSError as error:
        raise click.ClickException(str(error)) from error
    click.echo(f"Installed {destination}")
