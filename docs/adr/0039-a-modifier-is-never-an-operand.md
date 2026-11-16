# 0039. A modifier is never an operand

**Status:** Accepted by Emil, 2026-11-16. Closes issue #46.

**Context.** `cli:bindAs cli:Operand` could be put on a modifier. Where such an operand goes was read two ways: after the subjects, or in declared order. Neither act map has one, so nothing showed the difference and nothing tested it.

**Decision.** A modifier bound as an operand is refused. `cli:bindAs cli:Operand` is for the one target setting of a tool (ADR 0018). `cli:bindAs cli:Option` is for a subject. A shape reports the case with its question: if this must be given by position, is it a subject?

**Consequences.**
- The order of an act's input matters for subjects only, as the vocabulary says.
- An operand is always what the act is done to, or the tool's target.
- An application that wants a positional modifier changes its role, which is a statement about the act and not about spelling.

**Alternatives considered.**
- Allow it in declared order. Rejected: no act needs it, and a term waits for one that does.
- Allow it after the subjects. Rejected for the same reason.
