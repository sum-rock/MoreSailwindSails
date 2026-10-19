"""Expose asset generation and baking commands without importing Blender."""

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
    """Generate Blender assets or bake authored meshes into C#."""
    context.ensure_object(dict)
    context.obj["blender"] = blender


def asset_command(*, action: str, asset: str):
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
                action=action,
                asset=asset,
                cut=cut,
                output=output,
                check=check,
            )
        except (ValueError, OSError, subprocess.CalledProcessError) as error:
            raise click.ClickException(str(error)) from error

    if asset == "staysail":
        command = click.option(
            "--cut",
            type=click.Choice(["A", "B", "C"], case_sensitive=False),
            default="A",
            show_default=True,
        )(command)
    if action == "bake":
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


@assets.group()
def generate() -> None:
    """Create an editable Blender asset (replaces its output file)."""


for asset in ("flying_sail", "staysail", "stay"):
    bake.add_command(asset_command(action="bake", asset=asset))
for asset in ("flying_sail", "staysail"):
    generate.add_command(asset_command(action="generate", asset=asset))
