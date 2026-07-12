# ADR 005: Native Print & PDF Rendering Engine
* **Status**: Decided
* **Context**: CV print/export must support customizable layouts (date filtering, detail toggles) while maintaining cross-browser compatibility across mobile (iOS Safari) and desktop browsers without external JS canvas or server-side PDF tools.
* **Decision**: Adopt a hybrid **Blazor State + `@media print`** approach. A print options modal modifies Blazor UI component state, triggering a DOM update. JS interop awaits `requestAnimationFrame` before calling native `window.print()`. Clean pagination and UI element hiding are governed strictly via standard CSS `@media print` rules (`break-inside: avoid`, `display: none`).
* **Consequences**: Avoids mobile pop-up/iframe blocking issues, eliminates third-party PDF library bloat, and provides deterministic page breaks.
