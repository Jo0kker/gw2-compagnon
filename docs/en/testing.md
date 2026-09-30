# Validation and first Windows test

Run the localization checker, console scenario runner and simulator build from README.
The runner covers profile independence, persistence, safe imports, aggregation, session
loss and actual Named Pipe exchanges, plus language resources/preferences. Local pipe
permissions are required. Passing synthetic tests does not validate ArcDPS integration.

On Windows, build and run the WPF project, or extract a successful Actions preview:

1. Confirm the demo label and fictional players are visible; no real data is implied.
2. Edit a profile; add two damage widgets with different subgroup filters. Resize,
   move, duplicate, hide and reopen the app. Verify independent persisted settings.
3. Switch Settings → Language, restart, and inspect English and French tabs, dialogs,
   empty states, error messages, tables and numeric formats. User names must remain unchanged.
4. Select the local simulator, run it, then repeat with loss/interruption/stall flags.
   Confirm incomplete/stale states and that closing the app does not hang the sender.
5. Export/import a profile twice and verify independent identities and filters.
6. Inspect keyboard navigation, text clipping, 100/150/200% scaling and second-screen use.
   Monitor recovery is not implemented yet; record limitations rather than assume support.

The cloud development environment cannot run WPF and has blocked NuGet Windows reference
restores. XML validation is not a substitute for compilation or visual acceptance.
Do not describe a Windows build or signed package as verified until its CI job succeeds
and a real Windows smoke test has been performed.

## Recorded checks — 2026-09-30

- .NET 10.0.401 Release builds of the test runner and simulator: zero warnings/errors.
- Console runner: 18/18 scenarios passed on Linux, including actual local pipe tests.
- Localization checker: 169 English/French pairs, matching placeholders and static XAML labels.
- Workflow YAML parsed and action revisions checked for full commit pinning.
- Desktop build attempt: blocked by NuGet proxy HTTP 403 (NU1301/NU1900).
- GitHub Windows job, PowerShell packaging, live SignPath signing and visual Windows
  checks: not executed/verified from this workspace.
