#!/usr/bin/env bash
set -euo pipefail

repo_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
game_dir="${1:-$HOME/.local/share/Steam/steamapps/common/Sailwind}"
dll="$repo_dir/src/bin/Release/netstandard2.0/MoreSailwindSails.dll"
destination="$game_dir/BepInEx/plugins/MoreSailwindSails/MoreSailwindSails.dll"

if [[ ! -f "$dll" ]]; then
    printf 'Release DLL not found. Build first: nix develop -c dotnet build -c Release\n' >&2
    exit 1
fi

if [[ ! -d "$game_dir/BepInEx/plugins" ]]; then
    printf 'BepInEx plugins directory not found: %s\n' "$game_dir/BepInEx/plugins" >&2
    exit 1
fi

nix develop -c dotnet build src/MoreSailwindSails.csproj -c Release -t:Rebuild
install -Dm644 -- "$dll" "$destination"
printf 'Installed %s\n' "$destination"
