"""Remove the base mod's installation directory without touching other plugins."""

from pathlib import Path
import shutil

import click

from tools import settings


@click.command()
@click.option(
    "--game-dir",
    type=click.Path(path_type=Path, file_okay=False),
    default=settings.DEFAULT_GAME,
    show_default=True,
)
def uninstall(game_dir: Path) -> None:
    """Delete BepInEx/plugins/MoreSailwindSails. Close Sailwind first.

    Leaves saves, configuration, other plugins and the benchmark recorder intact.
    """
    game_dir = game_dir.expanduser().resolve()
    plugins = game_dir / settings.PLUGINS_DIRECTORY
    if not plugins.is_dir():
        raise click.ClickException(f"BepInEx plugins directory not found: {plugins}")
    destination = plugins / settings.PLUGIN_DIRECTORY
    try:
        if destination.is_symlink():
            destination.unlink()
        elif destination.exists():
            shutil.rmtree(path=destination)
        else:
            click.echo(f"Not installed: {destination}")
            return
    except OSError as error:
        raise click.ClickException(str(error)) from error
    click.echo(f"Uninstalled {destination}")
