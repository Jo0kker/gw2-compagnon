# Privacy

The current desktop preview stores profiles and its language preference locally under
`%LOCALAPPDATA%/GW2Companion`. Profile export writes only its documented allowlisted
configuration fields. There are no API tokens in the current profile model.

The current application makes no external service requests, sends no telemetry and
uploads no logs. A current-user local Named Pipe receives fictional simulator data.
Diagnostic exception messages may contain local paths; review them before sharing.

Future WvW Insights publication will send selected combat logs to an external service
only after a clear user action. Token protection, retention and service-specific
privacy information must be implemented and documented before enabling that feature.
There is no updater in the current build.

GitHub Actions and optional SignPath signing process build artifacts outside the user's
computer. Their services have their own terms and privacy policies; signing inputs must
contain binaries and public project files only, never player logs or developer secrets.
