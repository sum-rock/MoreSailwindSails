"""Cut choices and output paths for staysail authoring."""

from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
CUTS = ("A", "B", "C")


def asset_path(cut: str) -> Path:
    return ROOT / f"assets/staysails/fishermans_staysail_mk{cut.lower()}.blend"


def source_path(cut: str) -> Path:
    return (
        ROOT
        / f"src/Sails/FishermansStaysail/Mk{cut}/FishermansStaysailMk{cut}BakedMesh.g.cs"
    )
