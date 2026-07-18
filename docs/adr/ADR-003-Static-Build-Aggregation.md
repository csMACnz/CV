# ADR 003: Static Build Aggregation Pipeline
* **Status**: Decided
* **Context**: GitHub Pages requires fast, static delivery, while local editing relies on a modular folder structure of YAML/Markdown files.
* **Decision**: Implement a .NET build-time aggregation step triggered during `dotnet publish` (and hooked into local Aspire watch workflows). This step compiles all folder-based metadata and Markdown content into a single `experience.json` artifact served from `wwwroot/data/`.
* **Consequences**: Blazor WASM executes only one HTTP GET request for initial hydration in production. Local development stays cleanly decoupled, writing back to individual raw content files without compromising client performance.
