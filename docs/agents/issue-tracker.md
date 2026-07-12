# Issue tracker: GitHub

Issues and PRDs for this repo live as GitHub issues. Prefer GitHub MCP tools for issue-tracker operations when they are available in the current agent environment. Fall back to the `gh` CLI for any operation the available GitHub MCP tools cannot perform.

## Conventions

- **Create an issue**: Prefer GitHub MCP issue-creation tools if the current environment exposes them. Otherwise use `gh issue create --title "..." --body "..."`. Use a heredoc for multi-line bodies.
- **Read an issue**: Prefer GitHub MCP issue-read and issue-comment tools if available. Otherwise use `gh issue view <number> --comments`, filtering comments by `jq` and also fetching labels.
- **List issues**: Prefer GitHub MCP list/search issue tools if available. Otherwise use `gh issue list --state open --json number,title,body,labels,comments --jq '[.[] | {number, title, body, labels: [.labels[].name], comments: [.comments[].body]}]'` with appropriate `--label` and `--state` filters.
- **Comment on an issue**: Prefer GitHub MCP issue-comment tools if available. Otherwise use `gh issue comment <number> --body "..."`
- **Apply / remove labels**: Prefer GitHub MCP issue-label mutation tools if available. Otherwise use `gh issue edit <number> --add-label "..."` / `--remove-label "..."`
- **Close**: Prefer GitHub MCP issue-close tools if available. Otherwise use `gh issue close <number> --comment "..."`

Infer the repo from `git remote -v` when using `gh` — it does this automatically when run inside a clone. GitHub MCP calls should use explicit owner/repo arguments.

## Pull requests as a triage surface

**PRs as a request surface: no.** External pull requests are not part of this repo's default triage queue.

When set to `yes`, PRs run through the same labels and states as issues, using the `gh pr` equivalents:

- **Read a PR**: `gh pr view <number> --comments` and `gh pr diff <number>` for the diff.
- **List external PRs for triage**: `gh pr list --state open --json number,title,body,labels,author,authorAssociation,comments` then keep only `authorAssociation` of `CONTRIBUTOR`, `FIRST_TIME_CONTRIBUTOR`, or `NONE` (drop `OWNER`/`MEMBER`/`COLLABORATOR`).
- **Comment / label / close**: `gh pr comment`, `gh pr edit --add-label`/`--remove-label`, `gh pr close`.

GitHub shares one number space across issues and PRs, so a bare `#42` may be either — prefer GitHub MCP PR/issue read tools when available, and otherwise resolve with `gh pr view 42` and fall back to `gh issue view 42`.

## When a skill says "publish to the issue tracker"

Create a GitHub issue, preferring GitHub MCP when it supports issue creation in the current environment and falling back to `gh` otherwise.

## When a skill says "fetch the relevant ticket"

Fetch the issue, preferring GitHub MCP read tools when available and falling back to `gh issue view <number> --comments`.

## Wayfinding operations

Used by `/wayfinder`. The **map** is a single issue with **child** issues as tickets. Prefer GitHub MCP for any supported issue-tracker operations in this flow, but use the `gh` commands below whenever the required mutation or dependency operation is not exposed by MCP.

- **Map**: a single issue labelled `wayfinder:map`, holding the Notes / Decisions-so-far / Fog body. `gh issue create --label wayfinder:map`.
- **Child ticket**: an issue linked to the map as a GitHub sub-issue (`gh api` on the sub-issues endpoint). Where sub-issues aren't enabled, add the child to a task list in the map body and put `Part of #<map>` at the top of the child body. Labels: `wayfinder:<type>` (`research`/`prototype`/`grilling`/`task`). Once claimed, the ticket is assigned to the driving dev.
- **Blocking**: GitHub's **native issue dependencies** — the canonical, UI-visible representation. Add an edge with `gh api --method POST repos/<owner>/<repo>/issues/<child>/dependencies/blocked_by -F issue_id=<blocker-db-id>`, where `<blocker-db-id>` is the blocker's numeric **database id** (`gh api repos/<owner>/<repo>/issues/<n> --jq .id`, _not_ the `#number` or `node_id`). GitHub reports `issue_dependencies_summary.blocked_by` (open blockers only — the live gate). Where dependencies aren't available, fall back to a `Blocked by: #<n>, #<n>` line at the top of the child body. A ticket is unblocked when every blocker is closed.
- **Frontier query**: list the map's open children (`gh issue list --state open`, scoped to the map's sub-issues / task list), drop any with an open blocker (`issue_dependencies_summary.blocked_by > 0`, or an open issue in the `Blocked by` line) or an assignee; first in map order wins.
- **Claim**: `gh issue edit <n> --add-assignee @me` — the session's first write.
- **Resolve**: `gh issue comment <n> --body "<answer>"`, then `gh issue close <n>`, then append a context pointer (gist + link) to the map's Decisions-so-far.
