"""Round-trip Blender fixtures through JSON and the project's independent Python baker."""

from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]


def bake(*, asset, cut="A"):
    with tempfile.TemporaryDirectory(prefix="mss-json-check-") as directory:
        path = Path(directory) / "asset.json"
        from tools.assets.extract import collect, write

        write(data=collect(asset=asset, cut=cut), output=path)
        result = subprocess.run(
            [
                str(ROOT / ".venv/bin/python"),
                "-c",
                "import json, pathlib, sys; from tools.assets.rendering import bake; print(bake(asset=sys.argv[1], data=json.loads(pathlib.Path(sys.argv[2]).read_text()), cut=sys.argv[3]), end='')",
                asset,
                str(path),
                cut,
            ],
            cwd=ROOT,
            capture_output=True,
            text=True,
        )
        if result.returncode:
            raise ValueError(result.stderr)
        return result.stdout
