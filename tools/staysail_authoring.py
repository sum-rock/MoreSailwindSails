"""Nominal cut dimensions and output paths for staysail authoring."""

import math
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
WIDTH = 13.8
HEAD_SLOPE_DEGREES = 20.0
CUTS = ("A", "B", "C")


def dimensions(cut: str):
    """Return head rise and the two foot heights in Blender's vertical axis."""
    rise = WIDTH * math.tan(math.radians(HEAD_SLOPE_DEGREES))
    luff = WIDTH * (1.5 if cut == "C" else 1.0)
    foot_slope = {"A": -20.0, "B": 0.0, "C": 40.0}[cut]
    return rise, -luff, -luff + WIDTH * math.tan(math.radians(foot_slope))


def asset_path(cut: str) -> Path:
    return ROOT / f"assets/staysails/fishermans_staysail_mk{cut.lower()}.blend"


def source_path(cut: str) -> Path:
    return ROOT / f"src/Sails/FishermansStaysail/Mk{cut}/FishermansStaysailMk{cut}BakedMesh.g.cs"
