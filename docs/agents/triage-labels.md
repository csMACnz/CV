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

`question` and `wontfix` already exist in the repository. `question` is the closest existing fit for the canonical `needs-info` state because it already marks items that need reporter clarification. Create `needs-triage`, `ready-for-agent`, and `ready-for-human` in GitHub before using `/triage`.

When a skill mentions a role (e.g. "apply the AFK-ready triage label"), use the corresponding label string from this table.
