"""Locate the editable checkout and installed game without depending on cwd."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_GAME = Path.home() / ".local/share/Steam/steamapps/common/Sailwind"
