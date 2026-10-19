"""Register the repository's developer and manual maintainer commands."""

import click

from tools.assets.cli import assets, bake
from tools.halyards.cli import generate_fallback_halyards
from tools.maintenance.decompile import decompile
from tools.maintenance.install import install_local
from tools.maintenance.logs import logs
from tools.maintenance.release import release


@click.group()
def main() -> None:
    """MoreSailwindSails development tools."""


for command in (
    assets,
    bake,
    generate_fallback_halyards,
    install_local,
    release,
    decompile,
    logs,
):
    main.add_command(command)
