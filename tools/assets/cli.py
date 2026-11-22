"""Expose asset baking commands without importing Blender."""

from pathlib import Path
import subprocess

import click


@click.group()
@click.option(
    "--blender",
    default="blender",
    show_default=True,
    help="Blender executable or path.",
)
@click.pass_context
def assets(context: click.Context, blender: str) -> None:
    """Bake authored Blender assets into C#."""
    context.ensure_object(dict)
    context.obj["blender"] = blender


def asset_command(*, asset: str):
    @click.command(name=asset)
    @click.option("--output", type=click.Path(path_type=Path, dir_okay=False))
    @click.pass_context
    def command(
        context: click.Context, output: Path | None, cut: str = "A", check: bool = False
    ) -> None:
        from tools.assets.runner import run

        try:
            run(
                blender=context.obj["blender"],
                asset=asset,
                cut=cut,
                output=output,
                check=check,
            )
        except (ValueError, OSError, subprocess.CalledProcessError) as error:
            raise click.ClickException(str(error)) from error

    if asset in ("staysail", "loose_foot_spritsail"):
        from tools.assets.loose_foot_spritsail.authoring import CUTS as SPRITSAIL_CUTS
        from tools.assets.staysail.authoring import CUTS as STAYSAIL_CUTS

        command = click.option(
            "--cut",
            type=click.Choice(
                STAYSAIL_CUTS if asset == "staysail" else list(SPRITSAIL_CUTS),
                case_sensitive=False,
            ),
            default="A",
            show_default=True,
        )(command)
    command = click.option(
        "--check",
        is_flag=True,
        help="Compare against the current bake; write nothing.",
    )(command)
    return command


@assets.group()
@click.pass_context
def bake(context: click.Context) -> None:
    """Bake an authored asset into deterministic C# mesh data."""
    context.ensure_object(dict)
    context.obj.setdefault("blender", "blender")


for asset in ("flying_sail", "staysail", "stay", "loose_foot_spritsail"):
    bake.add_command(asset_command(asset=asset))
