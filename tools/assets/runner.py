"""Launch asset workers in Blender's own Python environment."""

from pathlib import Path
import shutil
import subprocess

from tools.settings import ROOT


def run(
    *, blender: str, action: str, asset: str, cut: str, output: Path | None, check: bool
) -> None:
    executable = shutil.which(blender)
    if executable is None:
        raise ValueError(
            f"Blender executable not found: {blender}; use assets --blender PATH."
        )
    command = [executable, "--background", "--factory-startup", "--disable-autoexec"]
    if action == "bake":
        from tools.assets.loose_foot_spritsail_authoring import get_cut

        source = (
            get_cut(cut).asset_path
            if asset == "loose_foot_spritsail"
            else {
                "flying_sail": ROOT / "assets/flying_sail/fishermans_flying_sail.blend",
                "staysail": ROOT
                / f"assets/staysails/fishermans_staysail_mk{cut.lower()}.blend",
                "stay": ROOT / "assets/stays/stay.blend",
            }[asset]
        )
        command.append(str(source))
    command.extend(
        [
            "--python-exit-code",
            "1",
            "--python",
            str(Path(__file__).with_name(f"{action}_{asset}.py")),
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
