#!/usr/bin/env bash
set -euo pipefail

repo_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd -- "$repo_dir"

if [[ -n "$(git status --porcelain)" ]]; then
	printf 'Working tree is not clean. Commit or remove changes before tagging.\n' >&2
	exit 1
fi

git switch master
git fetch origin master
git merge --ff-only FETCH_HEAD

remote_head="$(git rev-parse FETCH_HEAD)"
if [[ "$(git rev-parse HEAD)" != "$remote_head" ]]; then
	printf 'Local master has commits that are not on origin/master. Push or reconcile them before tagging.\n' >&2
	exit 1
fi

plugin_version="$(sed -nE 's/^[[:space:]]*public[[:space:]]+const[[:space:]]+string[[:space:]]+PluginVersion[[:space:]]*=[[:space:]]*"([^"]+)"[[:space:]]*;.*/\1/p' src/Plugin.cs)"
if [[ -z "$plugin_version" || "$plugin_version" == *$'\n'* ]]; then
	printf 'Expected exactly one PluginVersion declaration in src/Plugin.cs.\n' >&2
	exit 1
fi

project_version="$(sed -nE 's/^[[:space:]]*<Version>([^<]+)<\/Version>[[:space:]]*$/\1/p' src/MoreSailwindSails.csproj)"
if [[ "$project_version" != "$plugin_version" ]]; then
	printf 'Project version (%s) does not match PluginVersion (%s).\n' "$project_version" "$plugin_version" >&2
	exit 1
fi

tag="v$plugin_version"
if ! git check-ref-format "refs/tags/$tag"; then
	printf 'Invalid release tag: %s\n' "$tag" >&2
	exit 1
fi

if git show-ref --verify --quiet "refs/tags/$tag"; then
	printf 'Tag already exists locally: %s\n' "$tag" >&2
	exit 1
fi

remote_tag="$(git ls-remote --tags --refs origin "refs/tags/$tag")" || {
	printf 'Could not check whether %s exists on origin.\n' "$tag" >&2
	exit 1
}
if [[ -n "$remote_tag" ]]; then
	printf 'Tag already exists on origin: %s\n' "$tag" >&2
	exit 1
fi

printf 'Release %s at commit %s will be tagged and pushed to origin, then the DLL will be rebuilt and a GitHub release published.\n' "$tag" "$remote_head"
printf 'Are you sure? [y/N] ' >&2
if ! IFS= read -r confirmation; then
	printf '\nRelease cancelled.\n' >&2
	exit 1
fi
case "$confirmation" in
[yY] | [yY][eE][sS]) ;;
*)
	printf 'Release cancelled.\n' >&2
	exit 1
	;;
esac

git tag -a "$tag" -m "Release $tag"
if ! git push origin "refs/tags/$tag:refs/tags/$tag"; then
	git tag -d "$tag"
	printf 'Could not push %s; removed the local tag created by this run.\n' "$tag" >&2
	exit 1
fi

printf 'Pushed release tag %s to origin.\n' "$tag"

nix develop -c dotnet build src/MoreSailwindSails.csproj -c Release -t:Rebuild
gh release create "$tag" "$repo_dir/src/bin/Release/netstandard2.0/MoreSailwindSails.dll" --verify-tag --notes-file "$repo_dir/docs/RELEASE_NOTES.md"
