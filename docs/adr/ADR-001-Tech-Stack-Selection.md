# ADR 001: Technical Stack Selection
* **Status**: Decided
* **Context**: The application needs to deliver an interactive frontend, remain hostable on static hosting (GitHub Pages), avoid Node.js build tools, and provide an administrative data modifier.
* **Decision**: Use Blazor C# WebAssembly (WASM) for the client application and .NET Aspire for local orchestration and backend file-system access. Styling will be authored in standard modern CSS without preprocessors.
* **Consequences**: Avoids JavaScript build pipelines entirely. Production builds result in static files. Local execution gains full .NET execution capabilities for file modification, but client-side architecture must cleanly decouple local write capabilities so they compile out or remain dormant in production.
