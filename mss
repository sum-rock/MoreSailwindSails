#!/usr/bin/env bash
# Run the Python CLI with the repository's pinned development tools.
set -euo pipefail

repo_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
exec nix develop "$repo_dir" -c uv run --project "$repo_dir" --locked mss "$@"
