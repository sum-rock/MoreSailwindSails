"""Register asset authoring and read-only fallback placement commands."""

import click

from tools.assets.cli import assets
from tools.halyards.cli import generate_fallback_halyards


@click.group()
def main() -> None:
    """MoreSailwindSails development tools."""


main.add_command(assets)
main.add_command(generate_fallback_halyards)
