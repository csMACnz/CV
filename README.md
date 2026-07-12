# Interactive CV & Portfolio Web Application

An interactive, single-page CV and technical resume web application engineered for high-level scanning and deep technical evaluation. Hosted entirely as a static single-page application (SPA) on GitHub Pages, with a local-first content management system (CMS) powered by .NET Aspire.

---

## Key Features

- **Interactive Timeline & Depth Toggles**: Browse chronological roles and seamlessly toggle project views between concise summary bullets and deep narrative breakdowns.
- **Query-Param Deep Detail Blades**: Slide-over drawer interface for rich project deep dives (`?project=id`), fully compatible with static hosting routing and native browser history.
- **Emergent Skills Matrix**: Category-driven skills taxonomy automatically derived from project metadata, complete with bi-directional highlight filtering.
- **Client-Side Search**: In-memory, debounced C# search engine indexing roles, skill tokens, and project narratives in real-time.
- **Native Print & PDF Engine**: Custom print options modal paired with native browser `@media print` rules for clean, paginated PDF exports via `window.print()`.
- **Local File-Backed CMS**: Elevated local Admin Mode running via .NET Aspire that allows copyediting through a visual GUI while writing raw Markdown and YAML straight back to disk.

---

## Tech Stack & Architecture Principles

- **Framework**: Blazor WebAssembly (.NET) running client-side.
- **Styling**: Modern, framework-less CSS with zero bundlers, SASS, or npm dependencies.
- **Orchestration**: .NET Aspire (exclusive to local development for environment management and local CMS file persistence).
- **Data Architecture**: Human-readable Markdown and YAML content files compiled into a single static `resume.json` payload during release builds.
- **Hosting**: GitHub Pages (Static Site Assembly).

> **Zero-Node Pipeline**: No Node.js, npm, webpack, or JavaScript compilation tools are used in development or build pipelines. Minimal vanilla JavaScript is strictly reserved for native browser APIs (`window.print()`, `requestAnimationFrame`).

---

## Repository Structure

```text
├── .github/              # GitHub Actions workflows (CI/CD to GitHub Pages)
├── content/              # Source-of-truth Markdown and YAML files per employer/project
│   └── employment/
├── docs/                 # Project documentation
│   └── adr/              # Architectural Decision Records (ADR-001 through ADR-009)
├── src/                  # C# Blazor WASM frontend & Aspire AppHost projects
├── CONTEXT.md            # Domain glossary, system boundaries, and project context
└── README.md             # Project overview and developer guide
```

## Local Development Setup

### Prerequisites

- .NET 8.0 SDK (or later)
- .NET Aspire Workload (`dotnet workload install aspire`)
- Git

### Running Locally with Admin CMS

To launch the application locally with the elevated Admin Mode and dynamic local file persistence:

```bash
# Clone the repository
git clone https://github.com/csMACnz/CV.git
cd CV

# Run via .NET Aspire AppHost
dotnet run --project src/AppHost/AppHost.csproj
```

Open the Aspire Dashboard link displayed in your terminal to access the local web application and persistence gateway.

## Build & Static Deployment

Production builds automatically run a pre-compilation step that aggregates all modular `/content` files into `wwwroot/data/resume.json` and strips local Admin API paths via `#if DEBUG` guardrails.

```bash
# Aggregate content and compile static release artifact
dotnet publish src/CVApp/CVApp.csproj -c Release -o output
```

Deploy the resulting `output/wwwroot` folder directly to GitHub Pages.

## Architectural Decisions

Detailed rationale behind every technical choice made in this repository can be found in our Architecture Decision Records:

- **ADR-001: Technical Stack Selection**
- **ADR-002: File-System Data Architecture**
- **ADR-003: Static Build Aggregation Pipeline**
- **ADR-004: Local CMS Compilation & Execution Boundaries**
- **ADR-005: Native Print & PDF Rendering Engine**
- **ADR-006: Emergent Skills Architecture & CMS Normalization**
- **ADR-007: Client-Side Search and Filter Processing**
- **ADR-008: Local CMS Persistence Gateway**
- **ADR-009: Query-Param State & Overlapping Detail Blade Architecture**

Full summary matrix available at **docs/adr/SUMMARY-ARCHITECTURE.md**.
