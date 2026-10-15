#!/usr/bin/env python3
"""Manually refresh a local, searchable reference of Sailwind's managed code."""

import argparse
from datetime import datetime, timezone
import fcntl
import hashlib
import json
from pathlib import Path
import shlex
import shutil
import subprocess
import sys
import tempfile


GENERATOR = "MoreSailwindSails/decompile-sailwind.py"
ASSEMBLIES = ("Assembly-CSharp.dll", "Assembly-CSharp-firstpass.dll")
SNAPSHOT_ENTRIES = ("source", "manifest.json", "decompile.log")


def sha256(path):
    digest = hashlib.sha256()
    with path.open(mode="rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def run_command(arguments, log, commands):
    command = [str(argument) for argument in arguments]
    commands.append(command)
    log.write("$ " + shlex.join(command) + "\n")
    log.flush()
    result = subprocess.run(
        args=command,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        check=False,
    )
    log.write(result.stdout)
    log.flush()
    if result.returncode != 0:
        raise RuntimeError(
            f"Command exited with status {result.returncode}: {shlex.join(command)}"
        )
    return result.stdout


def validate_destination(output_dir):
    for name in (*SNAPSHOT_ENTRIES, ".decompile.lock"):
        if (output_dir / name).is_symlink():
            raise RuntimeError(
                f"Refusing to replace or follow symlink: {output_dir / name}"
            )
    existing = [name for name in SNAPSHOT_ENTRIES if (output_dir / name).exists()]
    if existing:
        manifest_path = output_dir / "manifest.json"
        if not manifest_path.is_file():
            raise RuntimeError("Existing snapshot paths have no ownership manifest.")
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        if not isinstance(manifest, dict) or manifest.get("generator") != GENERATOR:
            raise RuntimeError(
                "Existing manifest does not identify this script's output."
            )


def publish_snapshot(staging_dir, output_dir):
    backup_dir = staging_dir / "previous"
    backup_dir.mkdir()
    backed_up = []
    published = []
    try:
        for name in SNAPSHOT_ENTRIES:
            destination = output_dir / name
            if destination.exists():
                destination.rename(target=backup_dir / name)
                backed_up.append(name)
            (staging_dir / name).rename(target=destination)
            published.append(name)
    except BaseException:
        # Move new files back before restoring the previous complete snapshot.
        for name in reversed(published):
            (output_dir / name).rename(target=staging_dir / name)
        for name in reversed(backed_up):
            (backup_dir / name).rename(target=output_dir / name)
        raise


def refresh(game_dir, output_dir):
    managed_dir = game_dir / "Sailwind_Data" / "Managed"
    assemblies = [managed_dir / name for name in ASSEMBLIES]
    for assembly in assemblies:
        if not assembly.is_file():
            raise RuntimeError(f"Required game assembly is missing: {assembly}")
    decompiler = shutil.which(cmd="ilspycmd")
    if decompiler is None:
        raise RuntimeError(
            "ilspycmd is unavailable; run inside the repository's Nix shell."
        )

    validate_destination(output_dir=output_dir)
    staging_dir = Path(tempfile.mkdtemp(prefix=".decompile-", dir=output_dir))
    print(f"Staging extraction in {staging_dir}", flush=True)
    try:
        commands = []
        hashes = {path.name: sha256(path=path) for path in assemblies}
        with (staging_dir / "decompile.log").open(mode="w", encoding="utf-8") as log:
            tool_version = run_command(
                arguments=[decompiler, "--version"], log=log, commands=commands
            ).strip()

            file_counts = {}
            for assembly in assemblies:
                print(f"Decompiling {assembly.name}...", flush=True)
                destination = staging_dir / "source" / assembly.stem
                destination.mkdir(parents=True)
                run_command(
                    arguments=[
                        decompiler,
                        "--disable-updatecheck",
                        "--project",
                        "--nested-directories",
                        "--referencepath",
                        managed_dir,
                        "--outputdir",
                        destination,
                        assembly,
                    ],
                    log=log,
                    commands=commands,
                )
                file_counts[assembly.name] = sum(
                    1
                    for path in destination.rglob(pattern="*.cs")
                    if path.is_file() and path.stat().st_size
                )
                if file_counts[assembly.name] == 0:
                    raise RuntimeError(
                        f"No nonempty C# files generated for {assembly.name}."
                    )

            for assembly in assemblies:
                if sha256(path=assembly) != hashes[assembly.name]:
                    raise RuntimeError(f"Input changed during extraction: {assembly}")

        manifest = {
            "generator": GENERATOR,
            "extracted_at_utc": datetime.now(tz=timezone.utc).isoformat(),
            "tool": {"name": "ilspycmd", "version": tool_version},
            "reference_directory": str(managed_dir),
            "assemblies": [
                {
                    "path": str(assembly),
                    "sha256": hashes[assembly.name],
                    "output_directory": f"source/{assembly.stem}",
                    "source_file_count": file_counts[assembly.name],
                }
                for assembly in assemblies
            ],
            "commands": commands,
        }
        (staging_dir / "manifest.json").write_text(
            data=json.dumps(obj=manifest, indent=2) + "\n", encoding="utf-8"
        )
        publish_snapshot(staging_dir=staging_dir, output_dir=output_dir)
    except BaseException:
        print(
            f"Extraction failed; diagnostics and recovery files: {staging_dir}",
            file=sys.stderr,
        )
        raise
    else:
        # A cleanup failure after publishing must not report extraction failure.
        try:
            shutil.rmtree(path=staging_dir)
        except OSError as error:
            print(
                f"Snapshot published, but staging cleanup failed: {error}",
                file=sys.stderr,
            )
    print(f"Reference ready: {output_dir / 'source'}")
    print(f"Review diagnostics: {output_dir / 'decompile.log'}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--game-dir",
        type=Path,
        default=Path.home() / ".local/share/Steam/steamapps/common/Sailwind",
        help="Installed Sailwind directory (default: this user's Steam installation).",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=Path("/home/august/Documents/SailwindGame"),
        help=(
            "Local reference directory (default: /home/august/Documents/SailwindGame)."
        ),
    )
    arguments = parser.parse_args()
    game_dir = arguments.game_dir.expanduser().resolve()
    output_dir = arguments.output_dir.expanduser().resolve()
    repository_dir = Path(__file__).resolve().parent.parent
    try:
        for protected in (game_dir, repository_dir):
            if (
                output_dir == protected
                or protected in output_dir.parents
                or output_dir in protected.parents
            ):
                raise RuntimeError(
                    "The output directory must be separate "
                    "from the game and repository."
                )
        output_dir.mkdir(parents=True, exist_ok=True)
        validate_destination(output_dir=output_dir)
        with (output_dir / ".decompile.lock").open(mode="a") as lock:
            try:
                fcntl.flock(lock.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)
            except BlockingIOError:
                raise RuntimeError(
                    "Another extraction is already using this output directory."
                ) from None
            refresh(game_dir=game_dir, output_dir=output_dir)
    except (OSError, RuntimeError, ValueError) as error:
        print(f"Error: {error}", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        print("Extraction interrupted.", file=sys.stderr)
        return 130
    return 0


if __name__ == "__main__":
    sys.exit(main())
