"""Follow game logs with GNU tail's rotation and restart handling."""

from pathlib import Path

import click

from tools import settings
from tools.process import run


@click.group()
def logs() -> None:
    """Follow BepInEx or Unity logs across game restarts."""


def log_command(*, name: str, default: Path) -> click.Command:
    @click.command(name=name)
    @click.option(
        "--path",
        type=click.Path(path_type=Path, dir_okay=False),
        default=default,
        show_default=True,
    )
    @click.option(
        "--lines",
        type=click.IntRange(min=0),
        default=settings.LOG_LINES,
        show_default=True,
    )
    def follow(path: Path, lines: int) -> None:
        """Display recent lines and follow the log until Ctrl+C."""
        try:
            run(
                arguments=[
                    "tail",
                    "-n",
                    str(lines),
                    "-F",
                    "--",
                    str(path.expanduser().resolve()),
                ],
                cwd=settings.ROOT,
            )
        except KeyboardInterrupt:
            raise click.exceptions.Exit(code=130) from None

    return follow


logs.add_command(log_command(name="bepinex", default=settings.BEPINEX_LOG))
logs.add_command(log_command(name="player", default=settings.PLAYER_LOG))
