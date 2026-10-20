"""Expose the installed boat's mast and stay identities as a read-only inventory."""

from pathlib import Path

import click

from tools.halyards.profiles import PROFILES, read_profile
from tools.settings import DEFAULT_GAME


@click.command()
@click.argument("boat", type=click.Choice(list(PROFILES), case_sensitive=False))
@click.option(
    "--game-dir",
    type=click.Path(path_type=Path, file_okay=False, exists=True),
    default=DEFAULT_GAME,
    show_default=True,
)
@click.option(
    "--native-only", is_flag=True, help="Exclude Shipyard Expansion parts and changes."
)
def inspect_boat(boat: str, game_dir: Path, native_only: bool) -> None:
    """List registered mast/stay indexes, labels, asset files, and object IDs."""
    from tools.halyards.boat import Boat
    from tools.halyards.scene import Scene, identity

    try:
        scene = Scene(game_dir=game_dir, include_se=not native_only)
        installed = Boat(scene=scene, profile=read_profile(boat=boat))
        rows = [("Index", "Label", "Asset file", "Object ID")]
        for index, mast in sorted(installed.masts.items()):
            option = scene.component(obj=mast, kind="BoatPartOption")
            label = (
                scene.data(obj=option)["optionName"] if option else scene.name(obj=mast)
            )
            asset_file, object_id = identity(mast)
            rows.append((str(index), label, asset_file, str(object_id)))
    except (ValueError, OSError) as error:
        raise click.ClickException(str(error)) from error

    widths = [max(len(row[column]) for row in rows) for column in range(4)]
    for row in rows:
        click.echo(
            "  ".join(value.ljust(width) for value, width in zip(row, widths)).rstrip()
        )
