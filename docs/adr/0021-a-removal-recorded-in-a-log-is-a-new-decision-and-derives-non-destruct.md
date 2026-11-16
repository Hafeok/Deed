# 0021. A removal recorded in a log is a new decision and derives non-destructive

**Status:** Accepted by Emil, 2026-11-16. Closes issue #31.

**Context.** In an event-sourced application a removal is a new position; what was removed stays in the history. Varve's `update` and product-cli's `domain rm` are modelled as `deed:Appends` and derive non-destructive (`docs/findings/2026-11-16-product-cli.md`, P8).

**Decision.** Emil's ruling: by ADR 0006 a change is a new decision added to ground that stays. A removal recorded in a log is such a decision. Non-destructive is correct, and the two act maps stay as they are.

**Consequences.**
- The destructive hint means that something is lost, not that the current view shows less.
- An act whose manner is `deed:Removes` or `deed:Converges` on a state with no history stays destructive.
- Authors model the state that is written. They do not add a second state for the view in order to change the derivation.

**Alternatives considered.**
- Name both the log and the view so the derivation sees a removal. Recommended before the ruling and rejected by it.
