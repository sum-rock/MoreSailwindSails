#!/usr/bin/env bash
set -euo pipefail

# Follow BepInEx's log across Sailwind restarts.
log_path="$HOME/.local/share/Steam/steamapps/common/Sailwind/BepInEx/LogOutput.log"
exec tail -n 5000 -F "$log_path"
