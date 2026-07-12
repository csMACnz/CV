# CONTEXT.md

## System Boundaries
- **Production Environment**: A fully static single-page application (SPA) deployed to GitHub Pages. All data fetching, search, and filtering occur entirely client-side.
- **Local Development Environment**: Managed by .NET Aspire, enabling an elevated Admin Mode with write access to the local file system.

## Domain Glossary
- **Timeline Entry**: A chronological record representing a specific employment period, containing role definitions, company context, and associated Projects.
- **Project**: A discrete work unit nested under a Timeline Entry, supporting dual-state visual depth (Summary vs. Detailed Narrative) and serving as the primary source for skill tags.
- **Emergent Skills**: Skill tags declared directly within project files using simplified string identifiers ("Name is ID").
- **Skills Matrix**: A dynamically computed taxonomy (Languages, Frameworks, Paradigms, Tooling) derived at build/load time by aggregating all project skill tags.
- **Bi-directional Linking**: The interactive mechanism matching UI selections between the derived Skills Matrix and corresponding Projects/Timeline Entries.
- **Content Directory**: The local filesystem layout (`/content/employment/...`) acting as the source of truth for the application's data.
- **Detail Blade**: An overlapping slide-over panel triggered by query parameters (`?project=id`) providing a full narrative view of a selected project.
