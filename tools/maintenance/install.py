"""Build against a selected game installation and install its plugin DLL."""

from pathlib import Path
import shutil

import click

from tools.maintenance.build import rebuild
from tools import settings


@click.command()
@click.option(
    "--game-dir",
    type=click.Path(path_type=Path, file_okay=False),
    default=settings.DEFAULT_GAME,
    show_default=True,
)
def install_local(game_dir: Path) -> None:
    """Rebuild and install the plugin locally. Close Sailwind first."""
    game_dir = game_dir.expanduser().resolve()
    plugins = game_dir / settings.PLUGINS_DIRECTORY
    if not plugins.is_dir():
        raise click.ClickException(f"BepInEx plugins directory not found: {plugins}")
    dll = rebuild(game_dir=game_dir)
    destination = plugins / settings.PLUGIN_DIRECTORY / dll.name
    try:
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(src=dll, dst=destination)
        destination.chmod(mode=0o644)
    except OSError as error:
        raise click.ClickException(str(error)) from error
    click.echo(f"Installed {destination}")
