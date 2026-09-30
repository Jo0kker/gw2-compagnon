# Architecture and boundaries

## Implemented

`Companion.Core` contains profile models, validation, JSON persistence, portable
transfers, localization and fictional combat aggregation. `Companion.Transport` owns
the experimental length-prefixed JSON Named Pipe protocol and session state.
`Companion.Desktop` is the Windows WPF shell; its view model is also compiled into the
cross-platform scenario runner. `Companion.BridgeSimulator` emits synthetic events.

Profiles contain pages and widget instances. Each instance owns its ID, layout,
visibility and filters. Deep duplication creates new identities. Missing widget kinds
remain placeholders. Imports are validated before use; exports allowlist fields and
regenerate identities on import. Opaque unknown settings are deliberately not exported.
JSON replaces atomically with a previous backup. Language is a separate machine preference.

The pipe is scoped to the current user. Frames are bounded to 64 KiB; sessions track
sequence numbers and dropped events. Aggregation caps players at 512. A loss or an
interruption marks statistics incomplete. Stale data is labelled rather than reset to
zero. This experimental protocol is not the ArcDPS native ABI and is not a production
security boundary against other programs running as the same Windows user.

## Proposed

Keep WPF/.NET for the Windows shell, a small C++ x64 ArcDPS bridge and Named Pipes for
local transport. The native callback will copy valid data into a bounded queue; a worker
will transmit it independently of UI speed. Queue overflow must count lost events;
closing the app must never block the game. Protocol version/session identity and replay
fixtures are prerequisites to the native integration.

SQLite is planned for log/session indexing and recoverable installation transactions.
The addon manager must inventory existing files, preserve unknown DLLs, use staged
replacement and ownership records, and defer changes until GW2 exits. Nexus remains
optional with explicit ownership of shared components. No addon capability is inferred
merely from installing a DLL.

WvW Insights is part of the initial useful scope, with explicit manual upload, protected
tokens, deduplicated jobs and asynchronous status. API limits and endpoints must be
reverified before implementation. Multi-monitor bounds recovery precedes multi-window
support. No gameplay automation or replacement of ArcDPS collection is planned.
