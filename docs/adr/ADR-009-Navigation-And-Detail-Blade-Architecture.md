# ADR 009: Query-Param State & Overlapping Detail Blade Architecture
* **Status**: Decided
* **Context**: Deep project narratives need a full-screen focus view without breaking static single-page app routing on GitHub Pages or losing inline context.
* **Decision**: Manage deep project detail views using URL query parameters (`?project=project-id`). Selecting a project slides open a fixed, overlapping "Blade" drawer over the main view. Closing the blade restores the exact scroll position and underlying state.
* **Consequences**: Guarantees compatibility with GitHub Pages static hosting (no `404.html` routing hacks required). Provides native browser history support (`Back` button closes the blade) and generates shareable URLs for individual projects.
