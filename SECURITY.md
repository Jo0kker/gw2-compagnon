# Security

This is an experimental pre-release; no stable version is supported yet.
Do not post credentials or private logs in public issues. For sensitive reports, use
GitHub's private vulnerability reporting if enabled on this repository. If unavailable,
ask the maintainer @Jo0kker for a private reporting channel without disclosing details.

Only maintainers should configure the `code-signing` environment and SignPath policies.
Protect `main`, require review for workflow changes, restrict the signing environment
to `main`, and require a reviewer before enabling signing. No secret belongs in a PR
workflow. Signing proves publisher identity and integrity, not in-game compatibility.
