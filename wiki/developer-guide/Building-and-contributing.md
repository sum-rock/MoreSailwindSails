# Building and contributing

The repository provides a pinned Nix development shell for x86_64 Linux with
.NET 8. The plugin targets `netstandard2.0` and references assemblies from a
local Sailwind installation with BepInEx 5. The plugin build does not require
Shipyard Expansion or a Unity editor installation.

## Set up a checkout

From the repository root:

```sh
nix develop
dotnet tool restore
dotnet restore
```

The initial environment and NuGet restore need network access. The game
directory defaults to `~/.local/share/Steam/steamapps/common/Sailwind`. The
project checks for its game, Unity, BepInEx, and Harmony assemblies there. Use
`-p:SailwindDir="/path/to/Sailwind"` on restore/build commands when your
installation is elsewhere.

## Build and check the plugin

Inside the development shell:

```sh
dotnet csharpier check .
dotnet build -c Release --no-restore
dotnet run --project tests/GeometryChecks -c Release --no-restore
dotnet run --project tests/AssemblyChecks -c Release --no-restore
```

The DLL is written to `src/bin/Release/netstandard2.0/MoreSailwindSails.dll`.
Building does not copy it to the installed game.

GeometryChecks covers calculations and authored configuration, including stay
endpoints, mount identities, fitting dependencies, and saved option behavior.
Control checks cover neutral fitted-span projection, fallback boundaries and
retention, donor selections and mounting depths, authored sheet/halyard
profiles, and preview transforms and position grouping. AssemblyChecks inspects
the installed game signatures and the compiled mod's integration structure,
including shared consumer wiring, native-binding handoff, physical mast array
preservation, and diagnostic preview structure. Its startup configuration checks
use temporary BepInEx files to check defaults, overrides, and isolation from
live reloads. These suites do not run a Unity scene.

For a different game location, pass the MSBuild property to both test projects
and pass the same directory as the AssemblyChecks runtime argument. For example:

```sh
dotnet restore -p:SailwindDir="/path/to/Sailwind"
dotnet build -c Release --no-restore -p:SailwindDir="/path/to/Sailwind"
dotnet run --project tests/GeometryChecks -c Release --no-restore -p:SailwindDir="/path/to/Sailwind"
dotnet run --project tests/AssemblyChecks -c Release --no-restore -p:SailwindDir="/path/to/Sailwind" -- "/path/to/Sailwind"
```

For Python tooling changes, use `uv sync --locked`, `uv run pytest`, and
`ruff format --check .` inside the same shell. Installation and release commands
are separate maintainer operations; use the build commands above to produce a
local DLL.

## Check documentation changes

Inside the same pinned development shell, format and check each changed Markdown
file. For example:

```sh
prettier --write wiki/controls/Controls.md
prettier --check wiki/controls/Controls.md
git diff --check
```

Review the diff, page links, heading anchors, and source paths at their cited
revisions. Wiki page links use page names such as `Controls`, even though the
local Markdown files are grouped in directories. Documentation-only changes
normally need these checks rather than a plugin build. Describe actual checks
and results in the change description.

## Code and documentation conventions

[AGENTS.md](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/AGENTS.md)
records the repository guidance at this revision; consult the checkout's current
file when contributing.

- Prefer named arguments and straightforward code paths. Keep save identities
  compatible with the last tagged release and avoid patching external
  dependencies.
- Prefer one class per file with a comment describing its responsibility. Keep
  request structs, enums, and interfaces with their primary consumer rather than
  scattering simple contracts across separate files.
- Group support files by responsibility beneath their parent module. Strongly
  avoid directory trees more than four levels deep within `src/`.
- Group module patches by the thing they patch. Other-mod patches belong in
  Compatibility unless they are inseparable from a feature.
- Ground wiki explanations in code and link the relevant sources. Focus on
  reusable modding knowledge and examples. README and wiki edits each require an
  explicit request; publishing the wiki is a separate operation.
- Write commit subjects in the present tense and use the body to explain why the
  change was needed and what it solves.

## Contributing changes

Submit changes through the
[repository](https://github.com/sum-rock/MoreSailwindSails). Explain the
behavior changed and the checks run. Bug reports in the
[issue tracker](https://github.com/sum-rock/MoreSailwindSails/issues) should
include the affected boat or sail, game/mod versions, and steps to reproduce.
Keep proprietary game assemblies and extracted assets out of contributions.

The [mss command](MSS-command) provides asset baking, geometry inspection, local
installation, release publication, decompilation, and log following.
Installation and publication are explicit operations; the build commands above
produce a local DLL without performing either.

## Source

- [Pinned development environment](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/flake.nix)
- [Plugin target and assembly references](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/src/MoreSailwindSails.csproj)
- [AssemblyChecks entry point](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/tests/AssemblyChecks/Program.cs)
- [Fitted-span cases](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/tests/GeometryChecks/Controls/SheetSpanChecks.cs)
- [Authored sheet profiles](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/tests/GeometryChecks/Controls/NativeSeatProfileChecks.cs)
  and
  [halyard profiles](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/tests/GeometryChecks/Controls/HalyardProfileChecks.cs)
- [Consumer interface checks](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/tests/AssemblyChecks/Controls/SailControlInterfaceChecks.cs)
- [Startup configuration checks](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/tests/AssemblyChecks/Controls/SheetFallbackConfigurationChecks.cs)
- [Diagnostic structure checks](https://github.com/sum-rock/MoreSailwindSails/blob/fcdf300dd1ad89943b26e0c9db97152d6e01d3d5/tests/AssemblyChecks/Utils/WinchDiagnosticChecks.cs)
- [Python tooling and test configuration](https://github.com/sum-rock/MoreSailwindSails/blob/43540e59195537d3f5504c983f4c48ee5a9317fb/pyproject.toml)
