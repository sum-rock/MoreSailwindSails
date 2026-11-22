"""Extract assets with Blender, then render C# in the project's Python environment."""

import importlib
from pathlib import Path
import shutil
import subprocess
import tempfile
import json

from tools.settings import ROOT
from tools.assets import rendering

ASSETS = ("flying_sail", "staysail", "stay", "loose_foot_spritsail")


def paths(*, asset, cut):
    if asset not in ASSETS:
        raise ValueError(f"Unknown asset: {asset}")
    module = importlib.import_module(f"tools.assets.{asset}.authoring")
    if asset == "staysail":
        if cut not in module.CUTS:
            raise ValueError(f"Unsupported staysail cut: {cut}")
        return module.asset_path(cut=cut), module.source_path(cut=cut)
    if asset == "loose_foot_spritsail":
        definition = module.get_cut(cut)
        return definition.asset_path, definition.source_path
    if cut != "A":
        raise ValueError(f"{asset} has no cut {cut}")
    return module.ASSET_PATH, module.SOURCE_PATH


def run(
    *, blender: str, asset: str, cut: str, output: Path | None, check: bool
) -> None:
    source, default_output = paths(asset=asset, cut=cut)
    executable = shutil.which(blender)
    if executable is None:
        raise ValueError(
            f"Blender executable not found: {blender}; use assets --blender PATH."
        )
    with tempfile.TemporaryDirectory(prefix="mss-extracted-") as directory:
        intermediate = Path(directory) / "asset.json"
        subprocess.run(
            [
                executable,
                "--background",
                "--factory-startup",
                "--disable-autoexec",
                str(source),
                "--python-exit-code",
                "1",
                "--python",
                str(Path(__file__).with_name("extract.py")),
                "--",
                "--asset",
                asset,
                "--cut",
                cut,
                "--output",
                str(intermediate),
            ],
            check=True,
        )

        def reject_constant(value):
            raise ValueError(f"Non-finite extracted value: {value}")

        data = json.loads(
            intermediate.read_text(encoding="utf-8"), parse_constant=reject_constant
        )
        result = rendering.bake(asset=asset, cut=cut, data=data)
        rendering.write(
            source=result,
            output=output if output is not None else default_output,
            check=check,
        )
