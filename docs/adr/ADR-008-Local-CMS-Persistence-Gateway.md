# ADR 008: Local CMS Persistence Gateway
* **Status**: Decided
* **Context**: Blazor WASM cannot directly execute host `System.IO` calls due to browser sandbox restrictions.
* **Decision**: Implement a lightweight local ASP.NET Core Minimal API endpoint managed by .NET Aspire (compiled under `#if DEBUG`). The Blazor Admin UI posts content edits to this local endpoint, which writes raw YAML and Markdown directly to the repository's local `/content/` directory and triggers local aggregation.
* **Consequences**: Enables immediate local file-backed persistence. Leaves all Git operations (staging, committing, pushing) intentionally out of scope for the UI, keeping source control decisions manual or agent-driven.
