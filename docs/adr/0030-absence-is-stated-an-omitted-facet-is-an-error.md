# 0030. Absence is stated: an omitted facet is an error

**Status:** Accepted by Emil before 2026-11-15, as one of the decisions the project started from. Recorded 2026-11-16 (issue #2).

**Context.** A missing value cannot be told from a forgotten one.

**Decision.** A facet with nothing in it says `deed:Nothing`. An omitted facet is a validation error.

**Consequences.**
- Absence is always a decision.
- An act that changes state must still name a failure of its own (ADR 0008).

**Alternatives considered.**
- Treat a missing facet as empty. Rejected: it hides the undecided.
