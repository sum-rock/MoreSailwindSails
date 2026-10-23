"""Define repository paths, installed-game defaults and maintainer settings."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PLUGIN_SOURCE = ROOT / "src/Plugin.cs"
PROJECT_FILE = ROOT / "src/MoreSailwindSails.csproj"
RELEASE_DLL = ROOT / "src/bin/Release/netstandard2.0/MoreSailwindSails.dll"
RELEASE_NOTES = ROOT / "RELEASE_NOTES.md"
BOAT_PROFILES = ROOT / "src/BoatConfiguration/Boats"
RELEASE_BRANCH = "master"
RELEASE_REMOTE = "origin"

DEFAULT_GAME = Path.home() / ".local/share/Steam/steamapps/common/Sailwind"
# Relative to the selected game directory, including --game-dir overrides.
MANAGED_DIRECTORY = Path("Sailwind_Data/Managed")
PLUGINS_DIRECTORY = Path("BepInEx/plugins")
PLUGIN_DIRECTORY = "MoreSailwindSails"
BEPINEX_LOG = DEFAULT_GAME / "BepInEx/LogOutput.log"
PLAYER_LOG = Path.home() / (
    ".local/share/Steam/steamapps/compatdata/1764530/pfx/drive_c/users/steamuser/"
    "AppData/LocalLow/Raw Lion Workshop/Sailwind/Player.log"
)
LOG_LINES = 5000

DEFAULT_REFERENCE = Path.home() / "Documents/SailwindGame"
# Preserve the ownership marker used by existing snapshots.
DECOMPILE_GENERATOR = "MoreSailwindSails/decompile-sailwind.py"
DECOMPILE_ASSEMBLIES = ("Assembly-CSharp.dll", "Assembly-CSharp-firstpass.dll")
DECOMPILE_SOURCE = "source"
DECOMPILE_MANIFEST = "manifest.json"
DECOMPILE_LOG = "decompile.log"
DECOMPILE_LOCK = ".decompile.lock"
DECOMPILE_STAGING_PREFIX = ".decompile-"
DECOMPILE_BACKUP = "previous"
DECOMPILE_ENTRIES = (DECOMPILE_SOURCE, DECOMPILE_MANIFEST, DECOMPILE_LOG)
