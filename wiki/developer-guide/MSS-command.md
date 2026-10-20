# mss command

`mss` is the repository's Python CLI for asset authoring, installed-boat
inspection, and maintainer workflows. Its entry point is declared in
`pyproject.toml` and commands are registered in `tools/cli.py`; boat inspection
is implemented in `tools/inspection.py`.

## Running mss

From the repository root:

```sh
./mss --help
```

The `./mss` wrapper enters the pinned Nix environment and runs the CLI with its
locked Python dependencies. It handles environment setup for these commands. The
environment supplies Python, uv, Blender, .NET, Git, GitHub CLI, ILSpy, and GNU
tail. Every command and subcommand accepts `--help`.

| Command                        | Purpose                                                   | Output or effect                                                    |
| ------------------------------ | --------------------------------------------------------- | ------------------------------------------------------------------- |
| `assets generate`              | Generate editable sail meshes                             | Creates or replaces a `.blend` file                                 |
| `assets bake`                  | Bake authored meshes                                      | Writes generated C#, or compares it with `--check`                  |
| `bake`                         | Alias for `assets bake`                                   | Same bake commands and options                                      |
| `inspect-boat`                 | List registered mast and stay identities                  | Prints a table without modifying the installation                   |
| `generate-fallback-halyards`   | Find candidate winch positions in installed boat geometry | Emits C# definitions to stdout or a file                            |
| `install-local`                | Rebuild and install the plugin                            | Replaces the DLL in the selected game installation                  |
| `release`                      | Prepare and publish a release                             | Updates the checkout, builds, then confirms tagging and publication |
| `decompile`                    | Refresh a local game-code reference                       | Writes a decompiled snapshot outside the repository                 |
| `logs bepinex` / `logs player` | Follow game logs                                          | Displays recent lines and continues until Ctrl+C                    |

Commands that accept `--game-dir` default to
`~/.local/share/Steam/steamapps/common/Sailwind`. Options belong to the command
that declares them; there is no global `--game-dir` option.

## Asset generation and baking

Blender runs in background mode with factory startup settings and automatic
script execution disabled. `assets --blender PATH` selects an alternative
Blender executable; it defaults to `blender` on `PATH`.

```sh
./mss assets generate flying_sail
./mss assets generate staysail --cut B
./mss assets bake flying_sail
./mss assets bake staysail --cut B --check
./mss assets bake stay
./mss assets --blender /path/to/blender bake staysail --cut C
```

### Editable assets

`assets generate` supports `flying_sail` and `staysail`. It recreates a sail
from the authoring definitions and replaces the output `.blend` file. For
staysails, `--cut A|B|C` selects the cut, defaults to `A`, and is
case-insensitive. `--output FILE` selects a different destination.

| Asset              | Default generated file and bake input             |
| ------------------ | ------------------------------------------------- |
| `flying_sail`      | `assets/flying_sail/fishermans_flying_sail.blend` |
| `staysail --cut A` | `assets/staysails/fishermans_staysail_mka.blend`  |
| `staysail --cut B` | `assets/staysails/fishermans_staysail_mkb.blend`  |
| `staysail --cut C` | `assets/staysails/fishermans_staysail_mkc.blend`  |
| `stay`             | `assets/stays/stay.blend` (bake only)             |

There is no `assets generate stay` command. Baking reads the repository asset
shown above; `--output` changes the destination, not the input asset.

### Generated C#

`assets bake` supports all three asset types. It validates the authored mesh and
writes deterministic C# mesh data. Staysails have the same `--cut` option as
generation. Every bake accepts `--output FILE` and `--check`.

| Asset         | Default bake output                                                                                                 |
| ------------- | ------------------------------------------------------------------------------------------------------------------- |
| `flying_sail` | `src/Sails/FishermansFlyingSail/FishermansFlyingSailBakedMesh.g.cs`                                                 |
| `staysail`    | `src/Sails/FishermansStaysail/MkA/FishermansStaysailMkABakedMesh.g.cs` (substitute `B` or `C` for the selected cut) |
| `stay`        | `src/Stays/FishermansStay/FishermansStayBakedMesh.g.cs`                                                             |

`--check` writes nothing and fails if the destination is missing or differs from
the generated data. It still requires Blender. `./mss bake staysail --cut A` is
a shorter form of `./mss assets bake staysail --cut A`; use the `assets` form
when selecting Blender with `--blender`.

The [Staysail](Fishermans-Staysails#assets-and-ownership) and
[Flying Sail](Fishermans-Flying-Sails#assets-and-ownership) pages describe how
these meshes are shaped and owned at runtime.

## Boat inspection

`inspect-boat BOAT` lists the mast and stay indexes registered for a boat in the
installed assets. It prints a table sorted by index, with the option label (or
object name when no option exists), asset file, and `Mast` object ID. This
inventory helps verify the [boat reference](Boats) and identify rigging parts.

```sh
./mss inspect-boat brig
./mss inspect-boat brig --native-only
./mss inspect-boat baghala --game-dir /path/to/Sailwind
```

| Option                 | Behavior                                                               |
| ---------------------- | ---------------------------------------------------------------------- |
| `--game-dir DIRECTORY` | Select the installed game; defaults to the game directory listed above |
| `--native-only`        | Exclude Shipyard Expansion parts and changes                           |

Supported names are `brig`, `junk`, `jong`, `sanbuq`, `baghala`, `cog`, `kakam`,
and `dhow`, with `large_dhow` as an alias for `baghala`. Names are
case-insensitive. Installed SE additions are included by default. Shattered Seas
and OldChronian boats are outside the supported profiles.

The command reads assets without modifying the installation or exporting meshes.
Its output describes serialized assets and the reader's SE adaptations, not live
game state. Missing assets, incomplete SE installations, and ambiguous
registered mast indexes produce an error on stderr and a nonzero exit status,
without printing a partial inventory.

The index is `Mast.orderIndex`, not a shipyard menu position. The asset file and
object ID together identify the serialized `Mast` component; names alone can be
shared by different objects. The inventory includes registered rigging, rather
than every object present in an asset bundle.

## Fallback halyard generation

`generate-fallback-halyards BOAT` reads installed boat geometry and searches the
physical base masts for clear winch positions near native halyard height. It
emits `HalyardFallbackDefinition` entries containing contact points, normals,
offsets, and template indexes. It reads the installation without modifying game
assets or automatically editing the plugin's configuration.

```sh
./mss generate-fallback-halyards brig --mast 5
./mss generate-fallback-halyards baghala --native-only --output /tmp/baghala-halyards.cs
```

Supported boat names are `brig`, `junk`, `jong`, `sanbuq`, `baghala`, `cog`,
`kakam`, and `dhow`; `large_dhow` is an alias for `baghala`. Names are
case-insensitive. Mod-added boats such as Shroud and the OldChronian boats are
outside this generator's supported profiles.

| Option                 | Behavior                                                                     |
| ---------------------- | ---------------------------------------------------------------------------- |
| `--game-dir DIRECTORY` | Select the installed game to inspect                                         |
| `--native-only`        | Exclude Shipyard Expansion parts and changes                                 |
| `--mast INTEGER`       | Restrict the search to one configured physical base mast ID                  |
| `--output FILE`        | Write definitions to this file instead of stdout                             |
| `--clearance FLOAT`    | Minimum winch clearance in metres; default `0.02`                            |
| `--height-range FLOAT` | Maximum distance above or below native winch height in metres; default `0.5` |

Clearance and height range must be finite and nonnegative. Without `--mast`, the
command evaluates configured base masts; native-only coverage omits absent SE
masts. Installed SE assets are included when present unless `--native-only` is
set. An incomplete SE installation can prevent inspection.

Definitions go to stdout and progress or failures go to stderr. Output includes
input asset hashes and comments identifying unresolved masts. If any requested
mast remains unresolved, the command returns failure **after retaining the valid
definitions** in its output. Review those definitions before adding them to boat
control configuration. [How winches are placed](How-winches-are-placed) explains
how the runtime allocator uses authored fallbacks.

## Local installation

```sh
./mss install-local --game-dir /path/to/Sailwind
```

With Sailwind closed, `install-local` checks for `BepInEx/plugins`, rebuilds the
plugin in Release against the selected installation, and copies the DLL to:

```text
<Sailwind>/BepInEx/plugins/MoreSailwindSails/MoreSailwindSails.dll
```

It creates the plugin subdirectory if needed and replaces the destination DLL
without a confirmation prompt. The build uses
`dotnet build -c Release -t:Rebuild` and the selected `SailwindDir`. This
command does not run the check suites;
[Building and contributing](Building-and-contributing) lists them.

## Release publication

```sh
./mss release
```

`release` has no command-specific options. Its workflow is:

1. Require a clean working tree, switch to `master`, fetch `origin/master`, and
   fast-forward. The local commit must equal the fetched remote commit.
2. Read matching versions from `PluginVersion` in `src/Plugin.cs` and `Version`
   in `src/MoreSailwindSails.csproj`. Require an unused `v<VERSION>` tag locally
   and on `origin`.
3. Require `docs/RELEASE_NOTES.md` and authenticated GitHub CLI access, then
   rebuild the Release DLL using the default game location.
4. Show the tag, commit, and DLL, and ask **“Publish this release?”**,
   defaulting to no. Checkout updates and the build have already happened at
   this point.
5. After confirmation, verify the checkout is still clean and at the same
   commit, create an annotated tag, push it to `origin`, and publish a GitHub
   release with the DLL and release notes.

The command does not change version numbers, write release notes, or run the
check suites. A failed push leaves the local tag; a failed GitHub publication
leaves the pushed tag. The error reports that partial state so publication can
be recovered without blindly repeating the entire workflow.

## Decompiled game reference

```sh
./mss decompile --game-dir /path/to/Sailwind --output-dir /path/to/game-reference
```

`decompile` uses `ilspycmd` to extract `Assembly-CSharp.dll` and
`Assembly-CSharp-firstpass.dll` from `Sailwind_Data/Managed`. The default output
is `~/Documents/SailwindGame`, containing:

- `source/`: searchable C# projects, one per assembly.
- `manifest.json`: input hashes, tool version, extraction time, file counts, and
  commands.
- `decompile.log`: decompiler commands and output.

The output directory must be separate from both the game and repository,
including their ancestors and descendants. A lock prevents concurrent refreshes.
Existing snapshot entries require the tool's ownership manifest; symlinked
snapshot paths are rejected. Extraction is staged, checked for nonempty source
and unchanged inputs, then replaces the owned snapshot entries. Failed
extractions retain diagnostic and recovery files and report their location. Keep
this proprietary reference local.

## Log following

```sh
./mss logs bepinex --lines 200
./mss logs player --path /path/to/Player.log --lines 100
```

Both commands accept `--path FILE` and `--lines INTEGER`. They display the last
**5,000 lines** by default, then use `tail -F` to follow the file across
rotation and game restarts. `--lines 0` starts with new output only. Ctrl+C
stops following.

The default BepInEx log is under the default game directory at
`BepInEx/LogOutput.log`. The default Player log is the Steam/Proton path:

```text
~/.local/share/Steam/steamapps/compatdata/1764530/pfx/drive_c/users/steamuser/AppData/LocalLow/Raw Lion Workshop/Sailwind/Player.log
```

Log commands use `--path`, not `--game-dir`, for other installations.

## Source

- [Paths and defaults](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/tools/settings.py)
- [Asset commands](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/tools/assets/cli.py)
  and
  [Blender runner](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/tools/assets/runner.py)
- [Fallback options](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/tools/halyards/cli.py)
  and
  [geometry evaluation](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/tools/halyards/generate.py)
- [Local installation](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/tools/maintenance/install.py)
  and
  [release workflow](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/tools/maintenance/release.py)
- [Decompiled snapshots](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/tools/maintenance/decompile.py)
  and
  [log following](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/tools/maintenance/logs.py)
