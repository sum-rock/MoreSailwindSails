"""Locate the editable checkout and installed game without depending on cwd."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BOAT_PROFILES = ROOT / "src/BoatConfiguration/Boats"
DEFAULT_GAME = Path.home() / ".local/share/Steam/steamapps/common/Sailwind"
