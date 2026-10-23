"""Run external developer tools with explicit arguments and readable errors."""

from pathlib import Path
import shlex
import subprocess

import click


def run(
    *,
    arguments: list[str],
    cwd: Path,
    capture: bool = False,
    allowed_codes: tuple[int, ...] = (0,),
) -> subprocess.CompletedProcess:
    try:
        result = subprocess.run(
            args=arguments, cwd=cwd, text=True, capture_output=capture, check=False
        )
    except FileNotFoundError as error:
        raise click.ClickException(
            f"Executable not found: {arguments[0]}; run inside nix develop."
        ) from error
    except OSError as error:
        raise click.ClickException(str(error)) from error
    if result.returncode not in allowed_codes:
        details = (result.stderr or result.stdout or "").strip() if capture else ""
        raise click.ClickException(
            f"Command exited with status {result.returncode}: {shlex.join(arguments)}"
            + (f"\n{details}" if details else "")
        )
    return result
