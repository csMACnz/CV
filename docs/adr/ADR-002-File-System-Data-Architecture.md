# ADR 002: File-System Data Architecture
* **Status**: Decided
* **Context**: The data architecture needs to support granular local visual editing via the .NET Aspire CMS, minimize git merge conflicts, and allow rich markdown narratives for project deep-dives.
* **Decision**: Adopt a file-system directory structure where each employer is a root folder containing a metadata file (YAML), alongside sub-folders or discrete files for individual projects containing their own metadata (dates, roles) and narrative content. The system will dynamically calculate the aggregate employment timeline boundaries based on these granular entries.
* **Consequences**: Promotes excellent git hygiene and isolated content edits. However, it requires a compilation or aggregation step so the Blazor WASM client isn't forced to make dozens of individual HTTP requests at runtime in production.
