"""Register developer tools for asset authoring."""

import click

from tools.assets.cli import assets


@click.group()
def main() -> None:
    """MoreSailwindSails development tools."""


main.add_command(assets)
