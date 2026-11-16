# 0018. At most one setting per tool is bound as an operand; it comes first and has no environment fallback

**Status:** Accepted by Emil, 2026-11-16. Closes issue #20.

**Context.** ADR 0002 lets a tool bind a setting as an operand by declaration and left the details open. Varve binds one, `dataset`. product-cli binds none.

**Decision.** A tool binds at most one setting as an operand. It precedes the subjects. It has no environment variable fallback: an operand is given or the command is refused.

**Consequences.**
- Varve's `setup apply <setup> <dataset>` does not derive; the dataset comes first.
- The build tool refuses a second operand-bound setting, and `cli:env` on one.

**Alternatives considered.**
- Several operand-bound settings in declared order. Rejected: nothing needs it, and settings have no order.
- A fallback that makes the operand optional. Rejected: an optional leading operand is ambiguous with the first subject.
