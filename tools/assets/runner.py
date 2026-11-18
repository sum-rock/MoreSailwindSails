"""Launch asset workers in Blender's own Python environment."""

from pathlib import Path
import shutil
import subprocess

from tools.settings import ROOT


def run(
    *, blender: str, asset: str, cut: str, output: Path | None, check: bool
) -> None:
    executable = shutil.which(blender)
    if executable is None:
        raise ValueError(
            f"Blender executable not found: {blender}; use assets --blender PATH."
        )
    command = [executable, "--background", "--factory-startup", "--disable-autoexec"]
    from tools.assets.flying_sail.authoring import ASSET_PATH
    from tools.assets.staysail.authoring import asset_path
    from tools.assets.loose_foot_spritsail.authoring import get_cut

    source = (
        get_cut(cut).asset_path
        if asset == "loose_foot_spritsail"
        else {
            "flying_sail": ASSET_PATH,
            "staysail": asset_path(cut=cut),
            "stay": ROOT / "assets/stays/stay.blend",
        }[asset]
    )
    command.append(str(source))
    command.extend(
        [
            "--python-exit-code",
            "1",
            "--python",
            str(Path(__file__).parent / asset / "bake.py"),
            "--",
        ]
    )
    if asset in ("staysail", "loose_foot_spritsail"):
        command.extend(["--cut", cut])
    if output is not None:
        command.extend(["--output", str(output.resolve())])
    if check:
        command.append("--check")
    subprocess.run(command, check=True)
