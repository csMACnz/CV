# ADR 007: Client-Side Search and Filter Processing
* **Status**: Decided
* **Context**: Real-time global search across roles, text descriptions, and skill tokens must run fully client-side in Blazor WASM without external JS dependencies or inducing UI typing lag.
* **Decision**: Implement a pure C# in-memory tokenized search index coupled with a debouncing input wrapper (utilizing `System.Threading.CancellationTokenSource`). Search result sets will be intersected directly with the active skills matrix selection index in real-time.
* **Consequences**: Yields snappy, predictable filtering operations without UI thread freezing. Postpones advanced full-text indexing optimization until user testing reveals a definite scale bottleneck.
