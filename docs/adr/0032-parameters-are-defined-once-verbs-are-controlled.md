# 0032. Parameters are defined once; verbs are controlled

**Status:** Accepted by Emil before 2026-11-15, as one of the decisions the project started from. Recorded 2026-11-16 (issue #2).

**Context.** The same option name meaning two things in one tool is a common defect; Varve has three.

**Decision.** A parameter is defined once for an application and reused, so a name means one thing across it. Verbs come from a controlled list.

**Consequences.**
- A shape reports two parameters or settings with one name.
- Which verbs Deed ships is ADR 0025.

**Alternatives considered.**
- Parameters declared per act. Rejected: nothing would hold a name to one meaning.
