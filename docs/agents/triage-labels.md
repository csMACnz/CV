# Triage Labels

The skills speak in terms of five canonical triage roles. This file maps those roles to the actual label strings used in this repo's issue tracker.

| Label in mattpocock/skills | Label in our tracker | Meaning                                  |
| -------------------------- | -------------------- | ---------------------------------------- |
| `needs-triage`             | `needs-triage`       | Maintainer needs to evaluate this issue  |
| `needs-info`               | `question`           | Waiting on reporter for more information |
| `ready-for-agent`          | `ready-for-agent`    | Fully specified, ready for an AFK agent  |
| `ready-for-human`          | `ready-for-human`    | Requires human implementation            |
| `wontfix`                  | `wontfix`            | Will not be actioned                     |

Category labels already present in GitHub Issues can remain `bug` and `enhancement`.

`question` is the intentional mapping for `needs-info`, because this repository already uses it for items that need reporter clarification.

- `question` and `wontfix` already exist in the repository.
- `question` maps to the canonical `needs-info` state to reuse the repo's existing “needs clarification” label and avoid creating a duplicate synonym for the same triage state.
- Create `needs-triage`, `ready-for-agent`, and `ready-for-human` in GitHub before using `/triage`.

When a skill mentions a role (e.g. "apply the AFK-ready triage label"), use the corresponding label string from this table.
