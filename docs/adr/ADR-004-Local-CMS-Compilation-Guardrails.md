# ADR 004: Local CMS Compilation & Execution Boundaries
* **Status**: Decided
* **Context**: Admin capabilities to modify local filesystem data must strictly exist in the local development environment and be completely absent from production GitHub Pages builds.
* **Decision**: Enforce environment boundaries using standard `#if DEBUG` preprocessor directives. Local file mutation endpoints and Admin UI controls are compiled exclusively in Debug mode (orchestrated by .NET Aspire). Production builds (`Release`) strip out all administrative code paths at compile time, resulting in an immutable, lean WASM bundle.
* **Consequences**: Guarantees zero administrative attack surface or code overhead in production. Requires developers to test full local workflow via Debug builds and verify production aggregation via Release builds.
