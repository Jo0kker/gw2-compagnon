# Contributing

Use .NET 10 and Windows for desktop work. Core, transport and the scenario runner also
run on Linux. Follow the commands in README.md before submitting a pull request.
Keep contributions under the project's MIT license; do not copy addon code without
checking its license and preserving notices.

Keep game callbacks non-blocking. Treat unavailable values as unavailable, never as
zero. Preserve unknown integrations and existing files. Do not add upload or update
behavior without a visible user decision and tests for interruption/recovery.

UI strings belong in `src/Companion.Core/Localization/Strings.resx` (English) and
`Strings.fr.resx` (French), with identical keys and format placeholders. Use `T(key)`
in C# and `{local:Tr Key}` in WPF. Never translate stored IDs or user-entered names.
Language changes apply after restart. OS-provided dialog controls and low-level OS
exception details may use the operating system language.

Document changes in English. Include behavior, validation and remaining limitations
in pull requests. Never commit real combat logs, private report URLs, credentials or
signing tokens. Signing requires maintainer-controlled GitHub environment approval;
ordinary contributions must not need a signing secret.
