#!/usr/bin/env bash
set -euo pipefail

# Follow Unity's Player log across Sailwind restarts.
log_path="$HOME/.local/share/Steam/steamapps/compatdata/1764530/pfx/drive_c/users/steamuser/AppData/LocalLow/Raw Lion Workshop/Sailwind/Player.log"
exec tail -n 5000 -F "$log_path"
