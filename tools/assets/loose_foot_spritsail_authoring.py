"""Cut catalog shared by the CLI and Blender workers; importing it needs no Blender."""

from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


@dataclass(frozen=True)
class SpritsailCut:
    asset_path: Path
    source_path: Path
    class_name: str


CUTS = {
    "A": SpritsailCut(
        asset_path=ROOT / "assets/loose_foot_spritsail/loose_foot_spritsail-mkA.blend",
        source_path=ROOT
        / "src/Sails/LooseFootedSpritsail/MkA/LooseFootedSpritsailMkABakedAsset.g.cs",
        class_name="LooseFootedSpritsailMkABakedAsset",
    ),
}


def get_cut(cut: str) -> SpritsailCut:
    try:
        return CUTS[cut.upper()]
    except KeyError:
        raise ValueError(
            f"Unsupported loose footed spritsail cut {cut!r}; supported: {', '.join(CUTS)}"
        ) from None
