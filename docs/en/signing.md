# Windows builds and optional SignPath signing

## Status and evidence

MIT was selected by the project owner. GitHub Actions builds are configured; this does
not establish eligibility for SignPath Foundation. No enrollment, signing request,
certificate, sponsorship or signed release has been obtained by this change. Bilingual
UI and English documentation are project choices, not verified Foundation obligations.

On 2026-09-30 the official `SignPath/github-action-submit-signing-request` repository
was consulted at v3.0, commit `f6d04783b4569d051e0c80105fe66e82819d0092`.
Its action contract confirms the token, organization, project, policy and GitHub artifact
ID inputs, optional artifact configuration, synchronous waiting and downloaded output.
The input is an Actions artifact from the same workflow, not a path to a local EXE.

Sources to verify before enrollment/activation:

- https://signpath.org/ and https://signpath.org/terms (Foundation eligibility/terms)
- https://docs.signpath.io/trusted-build-systems/github (current CI integration)
- https://github.com/SignPath/github-action-submit-signing-request/tree/v3.0

Foundation pages were blocked by the development environment's proxy. Public hosting,
an open-source license and a GitHub build alone must not be presented as sufficient
for admission. Verify maintenance, public source, project identities, approval roles,
required policy text/attribution and service conditions with the Foundation. Do not add
a sponsorship badge before acceptance. No upstream addon is covered by our MIT license.

## Ordinary builds

`build.yml` runs tests on Linux and Windows. Windows publishes a self-contained x64
folder with license/privacy notices. No signing credential is needed. These preview
artifacts are unsigned and are not installed or automatically released.

## Activation checklist for maintainers

1. Apply to the Foundation using the real project and maintainer identities. Review the
   current terms and agree who can submit and approve requests. This repository owner
   is @Jo0kker; no additional approver identity is assumed.
2. Configure the official GitHub integration/trusted build origin in SignPath for this
   repository and workflow. Protect `main` and workflow changes from unreviewed edits.
3. Configure an artifact definition that preserves the published folder and signs only
   `Companion.Desktop.exe`, `Companion.Desktop.dll`, `Companion.Core.dll`,
   `Companion.Transport.dll` and `fr/Companion.Core.resources.dll`. Do not replace
   Microsoft's runtime signatures. Confirm the downloaded archive preserves this layout.
4. Create the GitHub `code-signing` environment. Restrict deployment branches to `main`
   and configure required reviewers (prevent self-review where the account supports it).
   Environment names in YAML do not themselves enforce reviewers: configure this in GitHub.
5. Store `SIGNPATH_API_TOKEN` as an environment secret with minimum submission rights.
   Set environment variables `SIGNPATH_ORGANIZATION_ID`, `SIGNPATH_PROJECT_SLUG`,
   `SIGNPATH_POLICY_SLUG`, `SIGNPATH_ARTIFACT_CONFIGURATION_SLUG`, and
   `SIGNPATH_CERTIFICATE_THUMBPRINT` (expected publisher certificate SHA-1 thumbprint).
   Update the pinned thumbprint through review on certificate rotation.
6. After those controls are ready, set the **repository** variable `SIGNPATH_ENABLED`
   to `true`. It must be repository-scoped because the job guard runs before entering
   the environment. Leave unset to disable signing.
7. Manually dispatch `sign-windows.yml` on `main`, approve the protected environment and
   SignPath request as configured. The job builds/tests the checked-out commit, uploads
   it, submits that exact artifact ID and verifies the returned project signatures.
   Missing files, a different publisher or an invalid signature fail the job.
8. Smoke-test the complete signed folder on Windows before distribution. Signing does
   not imply SmartScreen reputation, compatibility or release readiness. No GitHub
   Release is created by this workflow. Publish only after review.

This workflow is prepared against the official action contract but has not run against
an enrolled SignPath project. Service-side configuration, archive paths, certificate
trust and signing policy require an end-to-end trial. The workflow has a 30-minute job
limit; delayed approval can time out. Inspect the existing request before retrying to
avoid redundant submissions. The unsigned input artifact remains explicitly labelled.

An updater and installer are separate future work; see [roadmap](roadmap.md).
