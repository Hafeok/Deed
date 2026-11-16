# 0031. Effect and authorization scopes are derived, never declared

**Status:** Accepted by Emil before 2026-11-15, as one of the decisions the project started from. Recorded 2026-11-16 (issue #2).

**Context.** Declared hints drift from what an act does.

**Decision.** Whether an act is read-only, destructive or idempotent is derived from its Needs and Changes. Authorization scopes are derived the same way.

**Consequences.**
- Amended by ADR 0001: each change states its manner, and the hints follow from the manners.
- Read-only means the act makes no new decision (ADR 0006).

**Alternatives considered.**
- Declare the hints per act. Rejected: they would disagree with the changes.
