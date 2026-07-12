# ADR 006: Emergent Skills Architecture & CMS Normalization
* **Status**: Decided
* **Context**: Skills could either be managed as a rigid centralized taxonomy or declared organically where they are applied.
* **Decision**: Define skills directly inside individual project metadata files as string tags ("Name is ID"). The global Skills Matrix is derived dynamically during data aggregation. The local .NET Aspire Admin CMS is responsible for normalization (autosuggestion, case sensitivity checks, deduplication) to prevent subtle typos or synonyms across projects.
* **Consequences**: Minimizes content duplication in storage. Adding a project automatically populates the Skills Matrix. Requires the local CMS to scan existing skill tags across all projects to offer auto-complete and consistency enforcement during content creation.
