# 0033. The CLI follows POSIX utility syntax with GNU long options

**Status:** Accepted by Emil before 2026-11-15, as one of the decisions the project started from. Recorded 2026-11-16 (issue #2).

**Context.** A generated command line needs one convention.

**Decision.** The CLI binding follows the POSIX utility syntax guidelines plus GNU long options. Subjects become operands in declared order, modifiers become long options, boolean modifiers become flags. Short names are assigned explicitly, never derived.

**Consequences.**
- Command paths are ADR 0007; the one operand-bound setting is ADR 0018.
- A subject bound as an option, or a modifier as an operand, is a declared override.

**Alternatives considered.**
- Derive short names from first letters. Rejected: they collide and change when a parameter is added.
