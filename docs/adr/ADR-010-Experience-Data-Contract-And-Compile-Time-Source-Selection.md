# ADR 010: Experience Data Contract & Compile-Time Source Selection
* **Status**: Decided
* **Context**: The project needed a durable contract for canonical payload naming and environment behavior so production publish output, local Aspire behavior, and validation expectations remain aligned over time.
* **Decision**: Standardize the canonical payload and route terminology on `experience` (`experience.json` and `/experience`) and enforce compile-time source selection by build configuration: Release uses compiled static payload behavior, while Debug/local Aspire uses API-backed behavior. Publish verification for release output must include a strict file-path check for `experience.json`.
* **Consequences**: Reduces naming drift and removes runtime ambiguity in data-source behavior. CI and tracer tests must continue validating both Debug and Release behavior and keep publish verification aligned with the `experience.json` artifact contract.
