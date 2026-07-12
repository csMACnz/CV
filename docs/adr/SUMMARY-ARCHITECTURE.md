# Architectural Decision Records Summary

| ADR ID | Title | Core Decision |
| :--- | :--- | :--- |
| **ADR-001** | Tech Stack Selection | Blazor WASM + .NET Aspire + Native CSS (Zero-Node pipeline). |
| **ADR-002** | File-System Architecture | Employer/Project markdown + YAML metadata folders for local git-friendly editing. |
| **ADR-003** | Build Aggregation | Pre-compiles local markdown/YAML into a single static `wwwroot/data/resume.json` payload during `dotnet publish`. |
| **ADR-004** | Local CMS Boundaries | Admin UI and save controllers isolated behind `#if DEBUG` preprocessor flags. |
| **ADR-005** | Native Print Engine | Blazor State + `@media print` rules, triggered via JS `requestAnimationFrame` before `window.print()`. |
| **ADR-006** | Emergent Skills Model | Skills defined at project level ("Name is ID") and aggregated automatically; local CMS handles normalization/autocomplete. |
| **ADR-007** | Search Execution | Pure C# in-memory tokenized index with debounced input processing. |
| **ADR-008** | CMS Persistence Gateway | Local ASP.NET Core Minimal API writes edited content straight back to repository files; git operations remain manual. |
| **ADR-009** | Detail Blade Architecture | Deep project view driven via query parameters (`?project=id`) rendering an AWS/Azure-style overlapping CSS slide-over blade. |
| **ADR-010** | Experience Data Contract & Source Selection | Canonical production payload uses `experience.json` and data source behavior is compile-time selected (Release static payload, Debug local API). |
