# Enterprise Service Bus Demo

An interactive demo of how an ESB can help your organization connect various systems in realtime.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![GitHub](https://img.shields.io/badge/GitHub-Repository-181717?logo=github)](https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo)
[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo/badge)](https://scorecard.dev/viewer/?uri=github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo)

---

## 🎬 What the demo shows

A Blazor Server app (`src/EsbDemo.Web`) with six simulated business systems - **CRM**, **ERP**, **eCommerce**, **WMS**, **Customer Portal** and **Data Warehouse** - connected through an in-memory message bus:

- Each system has its own tab and its own in-memory database. Creating, editing, deactivating or re-activating a customer (or placing an order) in one system **publishes** a message on a topic such as `account.create`, `account.updated` or `order.place`.
- Every system that **subscribes** to that topic receives the message and updates its own data, so a change made in one tab shows up in the others.
- Some systems **auto-respond**: the ERP answers `account.create` with `account.created` (minting the customer number) and `order.place` with `order.placed`. If no `account.created` arrives within 10 seconds, the request is flagged as timed out.
- The **app panel** on the left takes any system **offline**; messages for it are queued and delivered, in order, when it comes back online (shown dimmed in the log).
- The **message monitor** on the home page logs every send and receive with its correlation ID.

The bus selector offers only the **Mock In-Memory** bus today; Apache Kafka and RabbitMQ Streams are listed as coming soon.

---

## 🚀 Run the demo

Prerequisite: the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo.git
cd Enterprise-Service-Bus-Demo
dotnet run --project src/EsbDemo.Web
```

Then open the URL printed in the console (by default `http://localhost:5281`, or `https://localhost:7189` with `--launch-profile https`).

---

## ⚙️ Configuring apps and topics

The systems and messages are data, not code - edit the JSON files in `src/EsbDemo.Web/config/`:

| File | Defines |
|------|---------|
| `apps.json` | Each system: `name`, `displayOrder`, `defaultOnline`, the topics it `publishes` and `subscribes` to, and its `autoResponses` (`on` topic -> `publish` topic). |
| `topics.json` | Each topic: its `label`, the monitor colour (`colorFamily`, `colorShade`) and its `fields`, whose `default` values may use the placeholders `{yyyy-MM-dd}`, `{hh:mm}` (24-hour time), `{####}` and `{##}` (random numbers). |

---

## 🧱 Solution layout

| Project | Purpose |
|---------|---------|
| `src/EsbDemo.Core` (`netstandard2.0`) | Config model (`AppConfig`, `TopicConfig`) and the placeholder resolver. |
| `src/EsbDemo.Web` (`net10.0`) | The Blazor Server app: `MonitorService` (the in-memory bus), one connector and one EF Core in-memory database per system, and the UI. |
| `tests/EsbDemo.Core.Tests.Unit` | Unit tests for Core, run on every TFM from .NET Framework 4.6.2 to .NET 10. |
| `tests/EsbDemo.Web.Tests.Unit` | Unit and bUnit component tests for the web app. |
| `tests/EsbDemo.Tests.Fuzz` | Property-based (CsCheck) tests. |

Nothing in this repository is published as a NuGet package.

---

## 📄 License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for details.

---

## 📚 Documentation

- **GitHub Repository:** [https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo](https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo)
- **API Documentation:** https://Chris-Wolfgang.github.io/Enterprise-Service-Bus-Demo/
- **Formatting Guide:** [docs/README-FORMATTING.md](docs/README-FORMATTING.md)
- **Contributing Guide:** [CONTRIBUTING.md](CONTRIBUTING.md)

---

## 🔍 Code Quality & Static Analysis

This project enforces **strict code quality standards** through **7 specialized analyzers** and custom async-first rules:

### Analyzers in Use

1. **Microsoft.CodeAnalysis.NetAnalyzers** - Built-in .NET analyzers for correctness and performance
2. **Roslynator.Analyzers** - Advanced refactoring and code quality rules
3. **AsyncFixer** - Async/await best practices and anti-pattern detection
4. **Microsoft.VisualStudio.Threading.Analyzers** - Thread safety and async patterns
5. **Microsoft.CodeAnalysis.BannedApiAnalyzers** - Prevents usage of banned synchronous APIs
6. **Meziantou.Analyzer** - Comprehensive code quality rules
7. **SonarAnalyzer.CSharp** - Industry-standard code analysis

### Async-First Enforcement

This project uses **`BannedSymbols.txt`** to prohibit synchronous APIs and enforce async-first patterns:

**Blocked APIs Include:**
- ❌ `Task.Wait()`, `Task.Result` - Use `await` instead
- ❌ `Thread.Sleep()` - Use `await Task.Delay()` instead
- ❌ Synchronous file I/O (`File.ReadAllText`) - Use async versions
- ❌ Synchronous stream operations - Use `ReadAsync()`, `WriteAsync()`
- ❌ `Parallel.For/ForEach` - Use `Task.WhenAll()` or `Parallel.ForEachAsync()`
- ❌ Obsolete APIs (`WebClient`, `BinaryFormatter`)

**Why?** To ensure all code is **truly async** and **non-blocking** for optimal performance in async contexts.

---

## 🛠️ Building from Source

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (the Core tests also run on the older .NET and .NET Framework TFMs when their runtimes are installed)
- Optional: [PowerShell Core](https://github.com/PowerShell/PowerShell) for the scripts in `scripts/`

### Build Steps

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release

# Run the local PR gate (build, tests, coverage gate, DevSkim, gitleaks)
pwsh ./scripts/build-pr.ps1

# Run code formatting (PowerShell Core)
pwsh ./scripts/format.ps1
```

### Code Formatting

This project uses `.editorconfig` and `dotnet format`:

```bash
# Format code
dotnet format

# Verify formatting (as CI does)
dotnet format --verify-no-changes
```

See [docs/README-FORMATTING.md](docs/README-FORMATTING.md) for detailed formatting guidelines.

### Building Documentation

This project uses [DocFX](https://dotnet.github.io/docfx/) to generate API documentation:

```bash
# Install DocFX (one-time setup)
dotnet tool install -g docfx

# Generate API metadata and build documentation
cd docfx_project
docfx metadata  # Extract API metadata from source code
docfx build     # Build HTML documentation into docfx_project/_site
```

The documentation is automatically built and deployed to GitHub Pages when changes are pushed to the `main` branch.

**Local Preview:**
```bash
cd docfx_project
docfx build --serve

# Open http://localhost:8080 in your browser
```

**Documentation Structure:**
- `docfx_project/` - DocFX configuration and source files; the site is built into `docfx_project/_site`
- `docfx_project/index.md` - Main landing page content
- `docfx_project/docs/` - Additional documentation articles
- `docs/` - Hand-written repository guides (formatting, release workflow, workflow security, docs version picker)

---

## 🤝 Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for:
- Code quality standards
- Build and test instructions
- Pull request guidelines
- Analyzer configuration details
