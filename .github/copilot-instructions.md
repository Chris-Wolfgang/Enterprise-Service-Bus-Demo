# Copilot Coding Agent Instructions

## Repository Summary

This repository is the **Enterprise Service Bus Demo**: a Blazor Server application in which six simulated business systems (CRM, ERP, eCommerce, WMS, Customer Portal, Data Warehouse) exchange messages over an in-memory bus. It is an application, not a library - nothing is published to NuGet.

**Repository Type**: Working application (created from the fleet repo-template)
**Target Platforms**: `EsbDemo.Web` targets `net10.0`; `EsbDemo.Core` targets `netstandard2.0` and its tests run on .NET Framework 4.6.2-4.8.1, .NET Core 3.1 and .NET 5.0-10.0
**Primary Language**: C# (Razor components)

### Solution structure (`Enterprise-Service-Bus-Demo.slnx`)
- `src/EsbDemo.Core` - config model (`AppConfig`, `TopicConfig`, `FieldConfig`, `AutoResponse`) and `PlaceholderResolver`.
- `src/EsbDemo.Web` - the app: `Services/MonitorService.cs` (the in-memory bus, offline queues, account tracking, auto-responses), `Services/ConfigService.cs`, one connector per system in `Connectors/`, one EF Core in-memory `DbContext` per system in `Data/`, and the UI in `Components/`.
- `src/EsbDemo.Web/config/apps.json` and `topics.json` - the systems, their publish/subscribe topics and auto-responses, and each topic's fields.
- `tests/EsbDemo.Core.Tests.Unit`, `tests/EsbDemo.Web.Tests.Unit` (xunit + bUnit), `tests/EsbDemo.Tests.Fuzz` (CsCheck property tests).

## Build and Validation Instructions

### Prerequisites
- .NET 10 SDK (older runtimes only for the Core test TFMs)
- PowerShell 7 for `scripts/build-pr.ps1`

### Local gate
Run `pwsh ./scripts/build-pr.ps1` before pushing. It restores and builds (Release), runs every test project on every TFM, generates the coverage report and enforces the gates, and runs DevSkim and gitleaks. InspectCode, the changelog-fragment check and the solution-consistency check run only in CI.

Run the app with `dotnet run --project src/EsbDemo.Web`.

### Critical Build Requirements
- **Code Coverage**: at least 95% line coverage for production assemblies (`src/`) and 100% for test assemblies (`tests/`), enforced by `pr.yaml` and `scripts/build-pr.ps1`
- **Warnings as errors** in Release, with the analyzers configured in `Directory.Build.props`
- **Security Scanning**: DevSkim must pass with no errors
- **Changelog**: every PR that changes `src/` adds one fragment under `changelog/unreleased/` (see its README)

## Project Layout and Architecture

### Key Configuration Files
- **`.editorconfig`**: Code style rules (C# file-scoped namespaces, var preferences, analyzer severity)
- **`Directory.Build.props`** / **`BannedSymbols.txt`**: shared build settings, analyzers and banned (synchronous) APIs
- **`.gitignore`**: Comprehensive .NET gitignore (Visual Studio, build artifacts, packages)
- **`CONTRIBUTING.md`**: Contribution guidelines
- **`CODE_OF_CONDUCT.md`**: Standard Contributor Covenant v2.0

### GitHub Integration
- **Workflows**: `.github/workflows/pr.yaml` - Comprehensive CI/CD pipeline
- **Issue Templates**: Bug reports (YAML) and feature requests (Markdown)
- **PR Template**: Structured pull request template with checklists
- **CODEOWNERS**: Default owner `@Chris-Wolfgang`, update usernames as needed
- **Dependabot**: Configured for NuGet packages in all project directories

### Continuous Integration Pipeline (`.github/workflows/pr.yaml`)
The workflow runs on pull requests to `main` branch and includes:

1. **Environment**: Ubuntu Latest with .NET 5.0–10.0 (Windows adds .NET Framework 4.6.2–4.8.1)
2. **Build Steps**: Checkout → Setup .NET → Restore → Build → Test → Coverage → Security
3. **Artifacts**: Coverage reports and DevSkim results uploaded
4. **Branch Protection**: Configured to require this workflow to pass before merging

### Branch Protection Configuration
Branch protection rules are configured by running the local PowerShell script `scripts/Setup-BranchRuleset.ps1`. The script prompts you to choose repository settings during setup.

**Single-Developer Configuration (Default):**
- No PR approvals required (you can merge your own PRs)
- Allows solo developers to merge their own PRs while still enforcing CI/CD checks

**Multi-Developer Configuration:**
- Requires 1+ approval before merging
- Requires code owner review

**All Configurations Include:**
- Require status checks to pass before merging
- Require branches to be up to date
- Require conversation resolution before merging
- Restrict deletions and block force pushes
- Require code scanning (CodeQL High+ severity)

**Branch Protection Setup Instructions:**
1. Install GitHub CLI (gh) from https://cli.github.com/
2. Authenticate: `gh auth login`
3. From PowerShell 7+ (for example, using `pwsh`), run the branch protection setup script:
   ```powershell
   pwsh -File ./scripts/Setup-BranchRuleset.ps1
   ```
4. When prompted by the script, choose single-developer or multi-developer settings

## Key Files and Locations

### Root Directory Files
- `README.md` - What the demo shows, how to run and configure it
- `LICENSE` - MIT License
- `Enterprise-Service-Bus-Demo.slnx` - Solution file
- `.editorconfig` - Code style configuration
- `.gitignore` - .NET-specific gitignore

### GitHub Directory (`.github/`)
- `workflows/pr.yaml` - Main CI/CD pipeline
- `ISSUE_TEMPLATE/` - Bug report (YAML) and feature request templates
- `pull_request_template.md` - PR template with checklists
- `CODEOWNERS` - Code ownership rules
- `dependabot.yml` - Dependency update configuration

### Other Directories
- `src/`, `tests/` - see "Solution structure" above
- `benchmarks/`, `examples/` - empty placeholders
- `docs/` - hand-written repository guides; the API site is built from `docfx_project/` into `docfx_project/_site`

## Maintenance Framework

Every repo derived from this template uses a per-repo **Maintenance** tracking framework to organize ongoing improvement work (security, performance, testing, cleanup, docs, API, CI/CD). The framework has three pieces:

1. **One parent `Maintenance: <repo>` issue per repo** (labeled `maintenance`). Living "improvement menu" — stays open forever. Lists candidate work by category. Never close.
2. **Sub-issues labeled `maintenance-task` + a `maintenance - <category>` label.** These are the actual tracked work. Spawn lazily — create one when there's actionable work, close when done.
3. **A cross-repo GitHub Projects v2 board** that auto-aggregates every `maintenance-task` issue across all repos for filtered views (by repo, by category, by status).

### Categories (one per sub-issue)

| Label | Covers |
|---|---|
| `maintenance - security` | SAST/analyzer scans, finding fixes, dependency vulnerability audit |
| `maintenance - performance` | Profile, benchmark, optimize, validate gains |
| `maintenance - testing` | Coverage %, integration/smoke/mutation tests, fixtures, CI test-step additions |
| `maintenance - cleanup` | Refactor for reuse, quality, efficiency |
| `maintenance - docs` | XML doc coverage, README, CHANGELOG, samples |
| `maintenance - API` | Public/internal surface audit, breaking-change vigilance |
| `maintenance - CI/CD` | Docker, CI workflow, build/publish pipeline, packaging |

### For Copilot agents working in this repo

When you **discover work** that fits one of the categories — a security scanner finding, a slow hot path, missing test coverage, code smelling for refactor, outdated docs, an inadvertent breaking API change, a flaky CI step — **don't just comment or fix it silently.** Instead:

1. Open a `maintenance-task` sub-issue using the **"Maintenance task" issue template** (`.github/ISSUE_TEMPLATE/maintenance-task.yaml`).
2. Pick the matching category in the dropdown.
3. Fill in the Scope and Acceptance fields with concrete observable outcomes.
4. After creation, add the corresponding `maintenance - <category>` label (the form pre-fills only `maintenance-task`; the category must be added manually).
5. If you're submitting a PR that addresses the issue, include `Fixes #<sub-issue-number>` in the PR body so the project board auto-marks the item as Done.

This produces a paper trail that ties findings → tracked work → PRs, and rolls up into the cross-repo Maintenance view so unfinished improvements aren't forgotten when the agent session ends.

**Reserve plain (non-`maintenance - ` prefixed) issues for repo-specific decisions** that aren't part of the fleet-wide pattern — e.g., "Drop net6.0 from TFM matrix," "Add new feature X," one-off bug fixes.

### Provisioning Maintenance on a new repo

When a new repo is created from this template:
1. Run `scripts/Setup-Labels.ps1` to provision the 9 Maintenance labels.
2. Run `scripts/Setup-Maintenance.ps1 -MaintenanceProjectUrl '<url>'` to create the parent Maintenance issue. (Pass the cross-repo project URL — ask the user if you don't have it.)
3. The auto-add workflow in the Maintenance project will pick up new `maintenance-task` sub-issues automatically.

## Agent Guidelines

### Trust These Instructions
This information has been validated against the template structure and GitHub workflows. **Only search for additional information if these instructions are incomplete or found to be incorrect.**

### When Working in This Repository
1. **New systems or topics**: add them to `src/EsbDemo.Web/config/apps.json` / `topics.json`; a system that stores data also needs a connector and a `DbContext`
2. **Adding Dependencies**: Use `dotnet add package` commands
3. **Code Style**: Follow `.editorconfig` rules and the repository CLAUDE.md conventions
4. **Testing**: Test projects live in `tests/` and are named `*.Tests.Unit` / `*.Tests.Fuzz`
5. **Coverage**: 95% line coverage for `src/`, 100% for `tests/`
6. **Security**: Review DevSkim findings and address security concerns
7. **Maintenance framework**: When you find improvement work that fits a category, open a `maintenance-task` sub-issue rather than fixing silently (see "Maintenance Framework" section above)

### Validation Steps
Before submitting changes:
1. Run `pwsh ./scripts/build-pr.ps1` (build, tests, coverage gates, DevSkim, gitleaks)
2. Add a changelog fragment if `src/` changed
3. Ensure all GitHub Actions checks pass
