# Workflow Security

## Overview

This document describes the security measures in the GitHub Actions workflows of this repository,
mainly the PR validation workflow (`.github/workflows/pr.yaml`) and the protected-files guard
(`.github/workflows/protected-files.yaml`). The comments at the top of `pr.yaml` are the
authoritative description; this page summarises them.

## Security Architecture

### 1. PR validation runs on `pull_request`

`pr.yaml` runs on `pull_request` (and on `push` to `main`), not on `pull_request_target`:

```yaml
on:
  pull_request:
    branches:
      - main
  push:
    branches:
      - main
```

A `pull_request` run builds and tests the PR's own code with the PR's own copies of the workflow and of
every analyzer/config file. GitHub gives such a run a read-only `GITHUB_TOKEN` and no secrets for forks,
nothing in `pr.yaml` uses a secret, and `permissions: contents: read` applies to same-repository PRs
too, so untrusted code has nothing to reach.

### 2. Gate integrity: the protected-files guard

Because a `pull_request` run uses the PR's own workflow and configuration, a PR could try to weaken the
checks it is subject to. `protected-files.yaml` prevents that. It is a separate `pull_request_target`
workflow that **never checks out PR code**: it lists the PR's changed files through the API and fails
any PR that mixes a protected file with other changes. Protected files include:

- `.github/workflows/*` (including `pr.yaml` itself)
- `.editorconfig`, `*.globalconfig`, `*.ruleset`, `*.DotSettings`
- `Directory.Build.props`, `Directory.Build.targets` (at any depth)
- `.github/license-audit/*.json`, `.github/requirements/*`
- `BannedSymbols.txt`, `coverlet.runsettings`, `.gitleaks.toml`, `.config/dotnet-tools.json`
- the CI scripts the gates run (`scripts/build-pr.ps1`, `scripts/changelog.ps1`,
  `scripts/tfm-parity.ps1`, `scripts/third-party-notices.ps1`)

See the `case` list in `protected-files.yaml` for the exact set. A change to a protected file therefore
has to be a configuration-only PR that a maintainer reviews on its own.

### 3. Credential protection

Every checkout step sets `persist-credentials: false`, so the checkout token is not written to the git
config for later steps:

```yaml
- name: Checkout code
  uses: actions/checkout@<full commit SHA>  # vX.Y.Z
  with:
    persist-credentials: false
```

This keeps the token out of git config; it does not stop a step that explicitly references
`github.token` from using it.

### 4. Minimal permissions

`pr.yaml` defaults to:

```yaml
permissions:
  contents: read
```

The only write scope in the file is `security-events: write` on `inspectcode-upload`, a job that never
checks out PR code.

### 5. Pinned actions

Every third-party action is pinned to a full commit SHA with the version in a trailing comment
(`scripts/pin-actions.ps1` maintains the pins), so a moved tag cannot change what runs.

## Attack Scenarios

### Scenario 1: Malicious workflow modification
**Attack**: a PR edits `.github/workflows/pr.yaml` to skip or weaken checks.
**Prevention**: the protected-files guard fails any PR that changes a workflow together with other
files; a workflow-only PR is reviewed on its own.

### Scenario 2: Configuration tampering
**Attack**: a PR edits `.editorconfig`, `BannedSymbols.txt` or `Directory.Build.props` to disable
analyzers while adding code that would otherwise fail them.
**Prevention**: the same guard; the configuration change and the code change cannot land in one PR.

### Scenario 3: Credential theft
**Attack**: PR code tries to read GitHub credentials.
**Prevention**: `pull_request` runs get no secrets for forks and a read-only token; `persist-credentials: false`
keeps the token out of git config.

## Maintenance

### Changing a protected file

1. Open a PR that changes only the protected file(s); the guard reports it as a configuration-only PR.
2. Get it reviewed and merged to `main`.
3. Open the code change that depends on it as a separate PR.

### Adding a new protected file

1. Add the path to the `case` list in `protected-files.yaml`.
2. Update the list in the comments at the top of `pr.yaml` and in this document.

## References

- [GitHub Actions Security Hardening](https://docs.github.com/en/actions/security-guides/security-hardening-for-github-actions)
- [Keeping your GitHub Actions secure](https://docs.github.com/en/actions/security-guides/security-hardening-for-github-actions#using-third-party-actions)
- [Preventing pwn requests](https://securitylab.github.com/research/github-actions-preventing-pwn-requests/)
