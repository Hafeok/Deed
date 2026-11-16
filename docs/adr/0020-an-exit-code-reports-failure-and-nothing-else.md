# 0020. An exit code reports failure and nothing else

**Status:** Accepted by Emil, 2026-11-16. Closes issue #19.

**Context.** Varve's `lease` exits 1 when the lock is held, after printing a complete answer; `apply --plan --exit-code` exits 2 on a non-empty plan. In both the actor has the outcome. On MCP the first would be an error on a successful read (`docs/findings/2026-11-15-draft-1-varve.md`, F8).

**Decision.** A generated command exits 0 when the act yields its outcome, 1 on a declared failure unless the failure assigns another code, and 2 for a usage error. A property of an outcome is never signalled by exit code. Deed's own tool follows the same rule with three codes: 0, 1 for a map with violations or one that cannot be derived, 2 for anything that prevents validating, including a file that does not parse and an engine that refuses the shapes.

**Consequences.**
- Varve's held lock is an outcome, not a failure, once its CLI is generated.
- A script that needs a signal reads the outcome.
- The validator's exit code 3 is folded into 2.

**Alternatives considered.**
- A CLI term mapping an outcome property to an exit code. Rejected until an application insists: it is the `--exit-code` flag generalised.
