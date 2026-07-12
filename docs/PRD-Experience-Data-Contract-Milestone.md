## Problem Statement

The current milestone decisions are spread across conversation context and need one implementation-ready specification that unifies the project’s language, production data contract, build-configuration behavior, publish verification expectations, and CI test visibility requirements. Without a consolidated spec, the Experience model can drift in naming, verification can become inconsistent, and compile-time behavior guarantees can be weakened over time.

## Solution

Define a single milestone spec that locks the canonical Experience language and its data contract, clarifies compile-time source selection behavior between local and production environments, requires strict publish verification checks, and sets clear CI testing visibility and tracer coverage expectations. The milestone also requires explicit review of glossary and ADR updates so durable architectural decisions are captured at the right level.

## User Stories

1. As a maintainer, I want one unified milestone spec, so that implementation work follows a single agreed source of truth.
2. As a maintainer, I want canonical Experience terminology, so that route, payload, and feature language remain consistent.
3. As a maintainer, I want Experience to be the canonical domain concept, so that future naming does not regress into ambiguous alternatives.
4. As a maintainer, I want the payload contract to be clearly named `experience.json`, so that published artifact expectations are deterministic.
5. As a maintainer, I want the canonical route to be `/experience`, so that navigation and API semantics match domain language.
6. As a maintainer, I want production data source behavior to be compile-time determined, so that runtime ambiguity is removed.
7. As a maintainer, I want local Aspire behavior and production behavior to be explicitly distinct, so that editing workflows stay safe and predictable.
8. As a maintainer, I want publish verification to require a strict file-path check to `experience.json`, so that build correctness can be proven objectively.
9. As a maintainer, I want CI to run test packs as separate visible steps/jobs, so that failures are quickly attributable.
10. As a maintainer, I want tracer tests to validate Debug behavior, so that local-mode data source expectations are continuously enforced.
11. As a maintainer, I want tracer tests to validate Release behavior, so that production-mode data source expectations are continuously enforced.
12. As a maintainer, I want both Debug and Release tracer checks represented in CI, so that environment-boundary guarantees are visible in pull requests.
13. As a reviewer, I want acceptance criteria tied to externally observable behavior, so that implementation can be validated consistently.
14. As a reviewer, I want glossary updates captured when new domain concepts are resolved, so that ubiquitous language remains current.
15. As an architect, I want ADR-worthiness assessed for hard-to-reverse decisions, so that long-lived trade-offs are discoverable.
16. As a contributor, I want non-architectural operational details left out of ADRs, so that ADR quality and signal remain high.
17. As a contributor, I want this milestone to define in-scope and out-of-scope boundaries, so that follow-on work is sequenced clearly.
18. As a test author, I want testing decisions to prioritize external behavior over internals, so that tests remain stable through refactors.
19. As a release owner, I want publish checks to be unambiguous and automated, so that deployments to static hosting are reliable.
20. As a maintainer, I want a ready-for-agent artifact, so that implementation can begin immediately.

## Implementation Decisions

- The canonical domain term for this scope is **Experience**.
- Canonical naming for this scope is:
  - payload artifact: `experience.json`
  - route: `/experience`
- Build behavior is configuration-driven at compile time:
  - production compilation resolves to compiled static JSON behavior
  - local Aspire/debug compilation resolves to API-backed local behavior
- Publish validation uses a strict artifact-path check for `experience.json` as the authoritative release verification signal.
- CI test packs should remain separated into distinct visible steps/jobs for fault isolation and review clarity.
- Tracer coverage is required for both Debug and Release compilation behavior to validate the environment-specific contract.
- Ubiquitous language updates are required wherever existing definitions do not reflect Experience terminology and canonical naming.
- ADR elevation criteria for this milestone:
  - create ADR entries for hard-to-reverse, architectural, trade-off decisions (for example, compile-time source-selection contract and production data-contract expectations)
  - keep operational/testing visibility preferences at PRD/workflow level unless they become architecture-shaping trade-offs.

## Testing Decisions

- Good tests in this milestone validate externally observable outcomes and contracts, not implementation internals.
- Primary seam: a **publish/build contract seam** that verifies compiled-mode behavior and artifact outputs from the outside.
- Supporting seam: an **environment-mode seam** that verifies Debug vs Release tracer outcomes as externally visible compilation behavior.
- Modules/areas to validate:
  - build/publish aggregation and artifact output behavior
  - compile-time environment boundary behavior
  - CI orchestration visibility for separated test packs
  - tracer assertions for Debug and Release behavior
- Prior-art expectations in this codebase:
  - architecture already distinguishes compile-time local-vs-production behavior
  - ADRs already capture build aggregation and local CMS compilation boundaries
  - milestone testing should extend this style by asserting contract behavior at the highest seam.

## Out of Scope

- Re-architecting the application beyond this milestone’s data-contract and verification boundaries.
- Introducing new naming concepts outside the Experience terminology decision.
- Expanding ADRs for routine operational preferences that are not hard-to-reverse architectural trade-offs.
- Unrelated UI/feature changes not needed to satisfy this milestone’s contracts.

## Further Notes

- During implementation, any newly resolved domain terms should be immediately reflected in the project glossary.
- ADR updates should be made only where decisions meet the project ADR-worthiness bar (hard to reverse, surprising without context, and chosen through trade-off).
