"""Expose read-only fallback placement generation through Click."""

from pathlib import Path

import click

from tools.settings import DEFAULT_GAME


@click.command()
@click.argument(
    "boat",
    type=click.Choice(
        [
            "brig",
            "junk",
            "jong",
            "sanbuq",
            "baghala",
            "large_dhow",
            "cog",
            "kakam",
            "dhow",
        ],
        case_sensitive=False,
    ),
)
@click.option(
    "--game-dir",
    type=click.Path(path_type=Path, file_okay=False, exists=True),
    default=DEFAULT_GAME,
    show_default=True,
)
@click.option(
    "--native-only", is_flag=True, help="Exclude Shipyard Expansion parts and changes."
)
@click.option(
    "--mast",
    type=click.IntRange(min=0),
    help="Evaluate only this physical base mast ID.",
)
@click.option(
    "--output",
    type=click.Path(path_type=Path, dir_okay=False),
    help="Write C# definitions here instead of stdout.",
)
@click.option(
    "--clearance",
    type=click.FloatRange(min=0),
    default=0.02,
    show_default=True,
    help="Minimum winch clearance in metres.",
)
@click.option(
    "--height-range",
    type=click.FloatRange(min=0),
    default=0.5,
    show_default=True,
    help="Maximum distance above/below native winch height in metres.",
)
def generate_fallback_halyards(
    boat: str,
    game_dir: Path,
    native_only: bool,
    mast: int | None,
    output: Path | None,
    clearance: float,
    height_range: float,
) -> None:
    """Print HalyardFallbackDefinition entries from installed boat geometry."""
    from tools.halyards.generate import generate

    try:
        source, failures = generate(
            boat=boat,
            game_dir=game_dir,
            native_only=native_only,
            mast_id=mast,
            clearance=clearance,
            height_range=height_range,
            report=lambda message: click.echo(message, err=True),
        )
        if output is None:
            click.echo(source, nl=False)
        else:
            output.write_text(source, encoding="utf-8")
        if failures:
            raise click.ClickException(
                f"{failures} requested mast(s) unresolved; valid definitions were retained."
            )
    except (ValueError, OSError) as error:
        raise click.ClickException(str(error)) from error
