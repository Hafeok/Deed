# 0028. Two layers: the act layer, and a binding layer per surface

**Status:** Accepted by Emil before 2026-11-15, as one of the decisions the project started from. Recorded 2026-11-16 (issue #2).

**Context.** A definition shared by several surfaces tends to take the shape of the first surface built.

**Decision.** The act layer says what an act is. A binding layer per surface says how it is spelled, and is derived by rule wherever possible. MCP and REST bind to the act layer directly and never inherit CLI terms.

**Consequences.**
- A CLI spelling can change without touching MCP or REST.
- Every departure from a rule is a declared override that can be counted.

**Alternatives considered.**
- One layer with per-surface annotations on the act. Rejected: surface syntax leaks into the act.
