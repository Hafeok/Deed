# 0001. A change states its manner, and effect is derived from manners

**Status:** Accepted by Emil, 2026-11-15. Amends decision 5.

**Context.** Decision 5 derives effect from Needs and Changes. Modelling Varve showed that "which state is written" yields read-only and nothing more. `update` and `storage readers forget` both write one state; one appends to a history and the other removes a record. MCP's destructive and idempotent hints and the HTTP method depend on the difference (F2).

**Decision.** Each value of `deed:changes` is a `deed:Change` with one `deed:state` and one `deed:manner`: `deed:Creates`, `deed:Appends`, `deed:Converges` or `deed:Removes`. Effect stays derived and is never declared: read-only when Changes is `deed:Nothing`; destructive when any change converges or removes; idempotent when every change converges or removes.

**Consequences.**
- Every write act answers "what is left of what was there before".
- The derived hints agree with what Varve's code and ADRs say of `setup apply`, `move-events` and takeover.
- Two derivations are doubtful and stay visible: `update` is non-destructive of the log and destructive of the current graph; `checkpoint` derives non-idempotent.
- The HTTP method rule is not settled by this: converges → PUT fails for all three converging acts Varve exposes over HTTP.

**Alternatives considered.**
- Declare the hints on the act. Rejected: it is decision 5 reversed, and the hints would drift from the changes.
- Derive from the verb. Rejected: it makes the controlled verb list carry semantics that belong to the change, and `apply` converges while `update` appends on the same resource.
- A finer set (replace, merge, move). Not needed by any Varve act.
