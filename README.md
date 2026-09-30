# GW2 Companion (working name)

Standalone Windows x64 companion for Guild Wars 2, licensed under [MIT](LICENSE).
**Development preview: combat data is fictional; the native ArcDPS bridge is not implemented yet.**

[Français](README.fr.md) · [Architecture](docs/en/architecture.md) · [Signing](docs/en/signing.md)

## Current features

- Simple dark WPF dashboard with top navigation and no left sidebar.
- Independent profiles and pages; create, rename, duplicate, delete and persist layouts.
- Multiple damage widgets with independent subgroup filters; move, resize, hide and duplicate.
- Clearly labelled built-in replay and separate local simulator using real Named Pipes.
- Sequence/loss, disconnection and timeout detection; incomplete data remains visible.
- Validated portable profile import/export, limited to 1 MiB and explicitly allowed fields.
- English and French interface, including application errors and dialogs. Choose Settings → Language and restart. English is the default; existing user names are preserved.

Not implemented: native ArcDPS DLL, real game data, addon installation, log indexing,
WvW Insights uploads, automatic updates, monitor recovery or installers. No game files
are modified and no logs are uploaded. Unknown widgets keep their layout but opaque
third-party settings are not yet portable.

## Build and run

Install the .NET 10 SDK. Windows is required for the WPF desktop app:

```powershell
dotnet run --project src/Companion.Desktop
```

Cross-platform checks (the tests are a console runner, not `dotnet test`):

```sh
python scripts/check-localization.py
dotnet run --project tests/Companion.Core.Tests --configuration Release
dotnet build tools/Companion.BridgeSimulator --configuration Release
```

For the local connection, select **Use local simulator** in Integrations, then run:

```sh
dotnet run --project tools/Companion.BridgeSimulator
```

Use `--drop`, `--interrupt` or `--stall` to exercise failure states. All emitted players
and numbers are fictional. See [testing and acceptance checks](docs/en/testing.md).

GitHub Actions builds on Linux and Windows. After a successful Windows job, download
`gw2-companion-windows-x64-preview-…`, extract the entire ZIP and launch
`Companion.Desktop.exe`. The runtime is bundled. Preview artifacts are unsigned and
expire after 14 days. Signing has a separate, disabled-by-default manual workflow.
Windows builds and visual checks have not been verified in the Linux development
workspace; GitHub Actions results must be checked before distributing a build.

## Data and project status

Profiles: `%LOCALAPPDATA%/GW2Companion/profiles.json`, with a previous `.bak` copy.
Language: `language.txt` in the same directory, excluded from profile exports.
Invalid profile files are not silently replaced. [Privacy](PRIVACY.md).

[Contributing](CONTRIBUTING.md) · [Security](SECURITY.md) · [Third-party notices](THIRD_PARTY_NOTICES.md)

The [roadmap](docs/en/roadmap.md) distinguishes implemented work from proposals.
Detailed original design notes remain in French under `docs/01-…` through `docs/11-…`;
the English guides cover setup, architecture, testing and release preparation.
No integration has been validated inside GW2. This independent project is not affiliated
with ArenaNet, ArcDPS or SignPath Foundation.
