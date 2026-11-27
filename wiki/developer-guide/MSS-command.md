# mss command

`mss` is the repository's Python CLI for asset authoring, installed-boat
inspection, performance comparisons, and maintainer workflows. Its entry point
is declared in `pyproject.toml` and commands are registered in `tools/cli.py`;
boat inspection is implemented in `tools/inspection.py`.

This reference follows revision `5d0dd01` (plugin metadata **0.4.1**, benchmark
**0.1.2**, Python package **0.1.0**). The linked code is the source of truth.

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
| `assets bake`                  | Bake authored meshes                                      | Writes generated C#, or compares it with `--check`                  |
| `bake`                         | Alias for `assets bake`                                   | Same bake commands and options                                      |
| `inspect-boat`                 | List registered mast and stay identities                  | Prints a table without modifying the installation                   |
| `generate-fallback-halyards`   | Find candidate winch positions in installed boat geometry | Emits C# definitions to stdout or a file                            |
| `install`                      | Rebuild and install the plugin                            | Replaces the DLL in the selected game installation                  |
| `uninstall`                    | Remove the base mod                                       | Deletes only `BepInEx/plugins/MoreSailwindSails`                    |
| `performance install`          | Rebuild and install the optional benchmark                | Installs the recorder independently of MSS                          |
| `performance compare`          | Compare repeated benchmark captures                       | Reads two directories and prints frame-time statistics              |
| `release`                      | Prepare and publish a release                             | Updates the checkout, builds, then confirms tagging and publication |
| `decompile`                    | Refresh a local game-code reference                       | Writes a decompiled snapshot outside the repository                 |
| `logs bepinex` / `logs player` | Follow game logs                                          | Displays recent lines and continues until Ctrl+C                    |

Commands that accept `--game-dir` default to
`~/.local/share/Steam/steamapps/common/Sailwind`. Options belong to the command
that declares them; there is no global `--game-dir` option.

## Asset baking

Blender runs in background mode with factory startup settings and automatic
script execution disabled. `assets --blender PATH` selects an alternative
Blender executable; it defaults to `blender` on `PATH`.

```sh
./mss assets bake flying_sail
./mss assets bake staysail --cut B --check
./mss assets bake stay
./mss assets bake loose_foot_spritsail --cut D --check
./mss assets --blender /path/to/blender bake staysail --cut C
```

### Editable assets

Edit the existing `.blend` files in Blender. The CLI provides baking only;
`assets generate` is no longer available. Baking reads the repository assets
below without saving changes to them. `--output FILE` changes the generated C#
destination, not the input asset.

| Asset                          | Bake input                                                                                                      |
| ------------------------------ | --------------------------------------------------------------------------------------------------------------- |
| `flying_sail`                  | `assets/flying_sail/fishermans_flying_sail.blend`                                                               |
| `staysail --cut A`             | `assets/staysails/fishermans_staysail_mka.blend`                                                                |
| `staysail --cut B`             | `assets/staysails/fishermans_staysail_mkb.blend`                                                                |
| `staysail --cut C`             | `assets/staysails/fishermans_staysail_mkc.blend`                                                                |
| `stay`                         | `assets/stays/stay.blend`                                                                                       |
| `loose_foot_spritsail --cut A` | `assets/loose_foot_spritsail/loose_foot_spritsail-mkA.blend` (substitute `B`, `C`, or `D` for the selected cut) |

Staysails accept `--cut A|B|C`; spritsails accept `--cut A|B|C|D`. Both options
default to `A` and are case-insensitive. Flying sails and stays have no `--cut`
option.

### Generated C#

`assets bake` supports all four asset types. Blender validates and extracts the
authored data into a temporary JSON file, then the repository's Python renderer
writes deterministic C#. The temporary handoff is removed afterward. Every bake
accepts `--output FILE` and `--check`.

The
[staysail collector](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/assets/staysail/collect.py)
also evaluates authored sheeting and reefing animation and exports the static
`WindCenter` marker's position and orientation. See the
[staysail asset requirements](Fishermans-Staysails#assets-and-ownership).

| Asset                  | Default bake output                                                                                                            |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| `flying_sail`          | `src/Sails/FishermansFlyingSail/FishermansFlyingSailBakedMesh.g.cs`                                                            |
| `staysail`             | `src/Sails/FishermansStaysail/MkA/FishermansStaysailMkABakedMesh.g.cs` (substitute `B` or `C` for the selected cut)            |
| `stay`                 | `src/Stays/FishermansStay/FishermansStayBakedMesh.g.cs`                                                                        |
| `loose_foot_spritsail` | `src/Sails/LooseFootedSpritsail/MkA/LooseFootedSpritsailMkABakedAsset.g.cs` (substitute `B`, `C`, or `D` for the selected cut) |

`--check` leaves the destination unchanged and fails if it is missing or differs
from the generated data. It still runs Blender extraction and uses temporary
files. `./mss bake staysail --cut A` is a shorter form of
`./mss assets bake staysail --cut A`; use the `assets` form when selecting
Blender with `--blender`.

The [Staysail](Fishermans-Staysails#assets-and-ownership),
[Flying Sail](Fishermans-Flying-Sails#assets-and-ownership), and
[Spritsail](Loose-Footed-Spritsails#implementation-and-asset-ownership) pages
describe their assets and runtime ownership. Spritsail bakes include cloth, reef
samples, hardware, attachments, and authored wind-center poses.

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
physical base masts for clear winch positions at or above each usable native
reef-winch template’s height. It emits `HalyardFallbackDefinition` entries
containing contact points, normals, offsets, and template indexes. It reads the
installation without modifying game assets or automatically editing the plugin's
configuration.

```sh
./mss generate-fallback-halyards brig --mast 5
./mss generate-fallback-halyards baghala --native-only --output /tmp/baghala-halyards.cs
```

Supported boat names are `brig`, `junk`, `jong`, `sanbuq`, `baghala`, `cog`,
`kakam`, and `dhow`; `large_dhow` is an alias for `baghala`. Names are
case-insensitive. Mod-added boats such as Shroud and the OldChronian boats are
outside this generator's supported profiles.

| Option                 | Behavior                                                                                  |
| ---------------------- | ----------------------------------------------------------------------------------------- |
| `--game-dir DIRECTORY` | Select the installed game to inspect                                                      |
| `--native-only`        | Exclude Shipyard Expansion parts and changes                                              |
| `--mast INTEGER`       | Restrict the search to one configured physical base mast ID                               |
| `--output FILE`        | Write definitions to this file instead of stdout                                          |
| `--clearance FLOAT`    | Minimum winch clearance in metres; default `0.01`                                         |
| `--height-range FLOAT` | Maximum distance upward along the mast from each reference winch in metres; default `2.0` |

The search begins at each reference height and moves upward along the mast in 2
cm steps. It tries the available native reef-winch templates until it finds a
clear seat. Clearance and height range must be finite and nonnegative. Without
`--mast`, the command evaluates configured base masts; native-only coverage
omits absent SE masts. Installed SE assets are included when present unless
`--native-only` is set. An incomplete SE installation can prevent inspection.

Definitions go to stdout and progress or failures go to stderr. Output includes
input asset hashes and comments identifying unresolved masts. If any requested
mast remains unresolved, the command returns failure **after retaining the valid
definitions** in its output. Review those definitions before adding them to boat
control configuration. [How winches are placed](How-winches-are-placed) explains
how the runtime allocator uses authored fallbacks.

Use [surface capture and model previews](Winch-placement-diagnostics) when
authoring measured positions or inspecting the resulting mounting frame.
Captured positions can supplement generator output; the runtime configuration
determines which definitions are used.

## Local installation

```sh
./mss install --game-dir /path/to/Sailwind
```

With Sailwind closed, `install` checks for `BepInEx/plugins`, rebuilds the
plugin in Release against the selected installation, and copies the DLL to:

```text
<Sailwind>/BepInEx/plugins/MoreSailwindSails/MoreSailwindSails.dll
```

It creates the plugin subdirectory if needed and replaces the destination DLL
without a confirmation prompt. The build uses
`dotnet build -c Release -t:Rebuild` and the selected `SailwindDir`. This
command does not run the check suites;
[Building and contributing](Building-and-contributing) lists them.

## Local uninstallation

```sh
./mss uninstall --game-dir /path/to/Sailwind
```

With Sailwind closed, `uninstall` deletes the entire
`BepInEx/plugins/MoreSailwindSails` directory without a confirmation prompt. It
leaves saves, configuration, other plugins, benchmark captures, and the
benchmark recorder intact. If MSS is already absent, the command reports
`Not installed` and succeeds. The selected game's `BepInEx/plugins` directory
must exist. A symlink at the mod directory is removed without deleting its
target.

## Benchmark installation and comparison

```sh
./mss performance install --game-dir /path/to/Sailwind
./mss performance compare --baseline /path/to/native-runs --candidate /path/to/mss-runs
```

`performance install` rebuilds the standalone recorder in Release against the
selected installation and replaces
`BepInEx/plugins/SailwindBenchmark/SailwindBenchmark.dll`. Close Sailwind first.
The command requires `BepInEx/plugins`, creates the recorder's subdirectory if
needed, and preserves benchmark configuration. It does not install MSS or run
the check suites.

`performance compare` requires `--baseline DIRECTORY` and
`--candidate DIRECTORY`; it reads captures offline and does not accept
`--game-dir`. Each directory must contain matching JSON/CSV pairs for one
scenario and variant. See [Benchmark](Benchmark) for configuration, capture
controls, comparison design, and interpretation.

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
3. Require `RELEASE_NOTES.md` at the repository root and authenticated GitHub
   CLI access, then rebuild the Release DLL using the default game location.
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

- [CLI registration](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/cli.py),
  [wrapper](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/mss),
  and
  [package entry point](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/pyproject.toml)
- [Boat inspection](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/inspection.py)
  and
  [profiles](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/halyards/profiles.py)
- [Spritsail cut catalog](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/assets/loose_foot_spritsail/authoring.py)
  and
  [C# rendering and comparison](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/assets/rendering.py)
- [Halyard search](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/halyards/search.py)
- [Paths and defaults](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/settings.py)
- [Asset commands](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/assets/cli.py)
  and
  [Blender runner](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/assets/runner.py)
- [Fallback options](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/halyards/cli.py)
  and
  [geometry evaluation](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/halyards/generate.py)
- [Base mod removal](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/maintenance/uninstall.py)
- [Benchmark installation](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/performance/install.py)
  and
  [comparison](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/performance/cli.py)
- [Local installation](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/maintenance/install.py)
  and
  [release workflow](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/maintenance/release.py)
- [Decompiled snapshots](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/maintenance/decompile.py)
  and
  [log following](https://github.com/sum-rock/MoreSailwindSails/blob/5d0dd0167e578f77deba2f9e2d3a2bce591fbb5a/tools/maintenance/logs.py)
