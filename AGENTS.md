# Working on MoreSailwindSails

Read [README.md](README.md), [src/Plugin.cs](src/Plugin.cs), relevant code and
the applicable pages in the [local wiki](wiki/Home.md) before editing. Check
`git status --short` and preserve user changes. Current user instructions take
precedence over historical design choices.

## Scope and workflow

- Use named arguments rather than positional arguments whenever possible to
  improve readability.
- Prefer one class per file as a rule and describe the responsibility of the
  class within a code comment at the top of the file. Stay/sail feature patches
  are an exception: group their top-level patch classes by integration
  responsibility in one file per integration within each feature's `Patches/`
  directory. Include a file responsibility comment and class responsibility
  comments.
- Update `README.md` only when the user explicitly requests a README change.
  Feature, bug-fix, release and general documentation work do not imply
  permission to edit it. Put useful modding reference updates in `wiki/` and
  report verification results in the change description or handoff.
- Implementation requests authorize editing, building and checking. Do not
  commit, push, alter saves or replace installed game files as part of a build.
  `mss install-local`, `mss release`, `mss decompile` and `mss logs` are manual
  maintainer workflows. Do not run them against real installations, repositories
  or services without an explicit user request for that operation. Test them
  with temporary fixtures and mocked external operations; migration is not
  execution authorization.
- Use installed assemblies/assets as the behavioral reference; upstream source
  may differ. Never commit proprietary assemblies or extracted game assets.

## Guidelines

- Always drive to simplicity. Choices should be made preserve straight forward
  code paths with minimal complexity.
- Avoid patching external dependencies.
- Ask for clarification from the user if directives appear to require complex
  implementation.
- Resist changes that would break save compatability relative to the last tagged
  release.
- Document reusable modding knowledge in the [wiki](wiki/Home.md), with source
  links and inspected versions for asset-dependent claims. Keep implementation
  history and validation logs in change descriptions, not reference pages.
  Update an existing topic instead of duplicating it. `docs/DEVELOPMENT.md` is
  only a navigation pointer.
- Maintain wiki pages locally in `wiki/`. Publishing to GitHub Wiki is a
  separate operation requiring an explicit request; local documentation work
  does not authorize a push.

## Verification and handoff

Use the pinned Nix formatting environment and the
[build and check guide](wiki/developer-guide/Building-and-contributing.md).
Always build Release for plugin-affecting changes and run both check suites. Use
CSharpier `check` for read-only work and `format` to fix formatting. For
Markdown, use Prettier `--check` to verify and `--write` to fix formatting.
Documentation-only changes normally need diff/link/path review, not a build. Do
not change dependencies to bypass first-time restore/network failures.

In-game validation is implied and does not need to be written in documentation.
Do not add in-game validation requirements, pending-validation reminders or
runtime checklists to the docs. Record concrete observed behavior and automated
check results separately when relevant.

At handoff, report the version, changes, checks actually run and built DLL path
when applicable.

### Local commands

From the repository root, enter `nix develop`. For a fresh checkout, run
`dotnet tool restore` and `dotnet restore`. Then use:

```sh
dotnet csharpier check .
dotnet build -c Release --no-restore
dotnet run --project tests/GeometryChecks -c Release --no-restore
dotnet run --project tests/AssemblyChecks -c Release --no-restore
prettier --check "**/*.md"
ruff format --check .
git diff --check
```

For Python tooling changes, also run `uv sync --locked` and `uv run pytest`. For
documentation-only changes, format/check the changed Markdown files and review
links and paths. The Release DLL is
`src/bin/Release/netstandard2.0/MoreSailwindSails.dll`.
