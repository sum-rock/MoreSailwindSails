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


# Authored Blender frame numbers for all staysail animations.
SHEET_FRAMES = (60, 90, 120)
REEF_FRAMES = (200, 240, 280, 320, 360, 400, 420)
EASED_FRAMES = tuple(frame + 300 for frame in REEF_FRAMES)
SAMPLE_FRAMES = tuple(sorted(set(SHEET_FRAMES + REEF_FRAMES + EASED_FRAMES)))
