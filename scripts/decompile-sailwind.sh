#!/usr/bin/env bash
# Run the local game-reference refresh with the repository's Nix tools.
set -euo pipefail

repo_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
exec nix develop "$repo_dir" -c python3 "$repo_dir/scripts/decompile-sailwind.py" "$@"
