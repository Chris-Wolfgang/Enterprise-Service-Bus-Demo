# Release Workflow Setup Guide

This guide describes how `.github/workflows/release.yaml` ships this repository. The repository is an
application (the `EsbDemo.Web` Blazor Server demo), not a NuGet library: a release builds self-contained
zips of the web app and attaches them to the GitHub Release. No project is packable, so the NuGet jobs
that the workflow inherits from the fleet repo-template skip themselves.

## Overview

The workflow triggers when you **publish a GitHub Release**. It:
- Runs every test project on every target framework on Windows and enforces the coverage gates:
  95% line coverage for each `src/` assembly and 100% for each `tests/` assembly (the same
  `CODECOV_MINIMUM` / `CODECOV_TEST_MINIMUM` gates as `pr.yaml`)
- Publishes `EsbDemo.Web` as a self-contained build for `win-x64`, `linux-x64` and `osx-arm64`,
  zips each one and attaches the zips to the release
- Records a SLSA build-provenance attestation for the zips and attaches the provenance bundle too
- Builds and deploys the DocFX documentation site for the release tag

## Required Configuration

No repository secrets are needed. The provenance attestation uses the workflow's OIDC token
(`id-token: write`), and the release upload uses the built-in `GITHUB_TOKEN`
(`contents: write` on the jobs that upload). If a NuGet package is ever added, the inherited
`publish-nuget` job publishes it with NuGet **trusted publishing (OIDC)** through `NuGet/login`;
configure a trusted-publishing policy on NuGet.org for this repository and `release.yaml`. There is no
`NUGET_API_KEY` secret to add.

### Verify Branch Protection Rules

**Location:** Settings → Rules → Rulesets

> **Note:** `scripts/Setup-BranchRuleset.ps1` configures the `main` ruleset interactively (option `[1]` for
> single-developer mode, `[2]` for multi-developer mode).

Ensure the ruleset:

- ✅ **Requires a pull request before merging**
- ✅ **Requires status checks to pass before merging**: the `pr.yaml` stages (Linux, Windows and macOS
  tests with the coverage gate) and the security scans
- ✅ **Requires branches to be up to date before merging**
- ✅ **Requires conversation resolution before merging**
- ✅ **Restricts deletions**
- ✅ **Requires linear history** (optional but recommended)

**What this does:** Ensures all code merged to `main` has passed the same validation that a release runs.

## Testing the Release Workflow

1. Go to the repository's **Releases** page
2. Click **"Draft a new release"**
3. Choose or create a tag (e.g., `v0.0.1-test`)
4. Check **"Set as a pre-release"** for test releases
5. Click **"Publish release"**

### Expected Workflow Behavior

1. **validate-release** (Windows): restores, builds and tests every target framework, then enforces the
   95% / 100% line-coverage gates and uploads the coverage report
2. **pack-and-validate** (Windows): runs `dotnet pack`; with no packable project it reports
   `has-packages=false`, so `publish-nuget` and `update-release-artifacts` are skipped
3. **verify-docs-build** (Windows): checks that the DocFX site builds
4. **publish-app** (Ubuntu, one job per runtime): `dotnet publish src/EsbDemo.Web/EsbDemo.Web.csproj
   --runtime <rid> --self-contained true` and zips the output as `EsbDemo.Web-<tag>-<rid>.zip`
5. **attach-app** (Ubuntu): attests build provenance for the zips and uploads the zips plus the
   `.intoto.jsonl` provenance bundle to the release
6. **trigger-docs**: builds and deploys the documentation for the release tag to `gh-pages`

### Monitoring the Workflow

- **Actions tab:** shows the workflow progress
- **Artifacts:** the coverage report and the per-runtime app zips
- **Releases:** the zips and the provenance bundle appear on the release when `attach-app` finishes

## Troubleshooting

### Tests Fail on a Specific Framework

1. Check the test logs for framework-specific issues
2. Test locally: `dotnet test --framework net462`
3. Push the fix, then re-publish the release (or re-run the workflow from the Actions tab)

### Coverage Below the Gate

1. Review the `CoverageReport/Summary.txt` artifact; the step prints every assembly below its gate
2. Add tests for the uncovered lines (or delete dead code)
3. Run `pwsh scripts/build-pr.ps1` locally; it applies the same gates
4. Push the fix, then re-publish the release (or re-run the workflow from the Actions tab)

### App Publish Fails for One Runtime

1. Reproduce locally: `dotnet publish src/EsbDemo.Web/EsbDemo.Web.csproj -c Release --runtime <rid> --self-contained true`
2. Fix the build, then re-run the failed jobs from the Actions tab

## Production Release Checklist

- [ ] All `pr.yaml` checks pass on `main`
- [ ] `scripts/build-pr.ps1` passes locally (build, tests, 95% / 100% coverage gates)
- [ ] `<Version>` updated in `src/EsbDemo.Web/EsbDemo.Web.csproj`
- [ ] `CHANGELOG.md` updated (`scripts/changelog.ps1` folds the `changelog/unreleased/` fragments)
- [ ] All PRs for the release merged to `main`

**Create a production release:**
1. Go to the repository's **Releases** page
2. Click **"Draft a new release"**
3. Choose or create the version tag (e.g., `v1.0.0`) targeting `main`
4. Add a title and release notes
5. Click **"Publish release"**

**After the workflow completes:**
- [ ] The three app zips and the provenance bundle are attached to the release
- [ ] `gh attestation verify <zip> --repo Chris-Wolfgang/Enterprise-Service-Bus-Demo` succeeds
- [ ] The documentation site shows the new version

## Workflow Architecture

```
Published GitHub Release
        │
        ▼
validate-release (Windows): build, test all TFMs, 95% src / 100% tests coverage gates
        │
        ├──► pack-and-validate ──► verify-docs-build
        │         │ has-packages == 'true' only
        │         └──► publish-nuget ──► update-release-artifacts   (skipped: nothing packable)
        │
        ├──► publish-app (win-x64, linux-x64, osx-arm64) ──► attach-app (attest + upload zips)
        │
        └──► trigger-docs (docfx.yaml → gh-pages)
```

## Support

If you encounter issues not covered in this guide:

1. Check the Actions tab of this repository on GitHub for detailed logs
2. Review the artifacts uploaded by failed jobs
3. Consult the [GitHub Actions documentation](https://docs.github.com/en/actions)
4. Open an issue in this repository with the workflow run URL and the error
