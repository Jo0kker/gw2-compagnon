# Delivery plan

Current: persistent dashboard prototype, English/French resources, replay, local
simulator transport, tests and GitHub Actions preview packaging. MIT licensing and
English contributor documentation are in place. Optional signing is prepared only.

Next gates:

1. Windows CI and visual smoke test; fix compilation/layout issues before user testing.
2. Verify official ArcDPS ABI and rules, implement native bridge, measure queue losses,
   overhead and disconnection behavior in a real game session.
3. Inventory and transactional addon operations; prove unknown files survive failure.
4. Index completed logs, sessions and WvW Insights manual publication; verify current
   API constraints and token protection, upload deduplication and recovery.
5. Restore window bounds safely across monitor changes and define resource budgets.
6. Ship an installer and updater after trusted release/signing infrastructure is ready.

The updater is a proposal, not part of this preview: use HTTPS metadata plus authenticated
release manifests, verify publisher/signatures and hashes before staging, keep settings
outside installation directories, recover interrupted replacement, and require user
consent to restart. Never execute an arbitrary update URL from a profile. Design rollback
without assuming old addons work with a newer GW2 build. Choose and test an installer
technology before implementing the update feed. SignPath signing alone is not an updater.
