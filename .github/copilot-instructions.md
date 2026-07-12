# GitHub Copilot repository instructions

## Agent skills

### Issue tracker

Use GitHub Issues for tickets and PRDs. External pull requests are not part of the default triage surface. See `docs/agents/issue-tracker.md`.

### Triage labels

Use the repository's configured triage label mapping, including `question` for the canonical `needs-info` role. See `docs/agents/triage-labels.md`.

### Domain docs

This repository already has `CONTEXT.md` at the root and `docs/adr/SUMMARY-ARCHITECTURE.md` as the ADR index. Read those first, then open the relevant ADRs under `docs/adr/`. See `docs/agents/domain.md`.

## Skills location

Repository-specific Matt Pocock skills live under `.github/skills/`. Prefer those skill definitions when they match the task.
