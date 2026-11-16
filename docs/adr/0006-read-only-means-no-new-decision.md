# 0006. Read-only means the act makes no new decision; what a context writes while reading is not a change

**Status:** Accepted by Emil, 2026-11-16. Closes issue #18.

**Context.** Effect is derived: an act whose Changes facet is `deed:Nothing` is read-only (ADR 0001). Modelling Varve showed reads whose context writes while they run. A read of an object-store dataset writes a reader record and deletes it on exit; a remote command stores a refresh token. Neither is in the act's Changes, so `query` derives read-only. The question was whether the model should say that a context writes, which would make such acts no longer read-only.

**Decision.** Emil's ruling: acting on ground is always read-only; changing something is the generation of new decisions, and that is not ground. Read-only is therefore correct for these acts.

In the vocabulary's terms: Needs names the ground an act stands on, the state that exists before it. Changes names the decisions the act adds. An act that adds no decision is read-only, whatever its context does in order to read. The Changes facet is not an inventory of bytes written, and a context declares no changes.

**Consequences.**
- No term is added. `deed:changes` stays on acts only.
- Varve's `query`, `export`, `feed` and `info` are read-only on every surface, including against an object store.
- The read-only hint an agent sees means "this adds no decision", which is narrower than "this writes nothing anywhere". A crashed object-store read can leave a reader record that holds back deletion until `storage readers forget` clears it. That is an operational fact of the context and belongs in its description, not in the act's effect.
- The same reading bears on issue #31 (removal in a log derives non-destructive): a removal there is also a new decision added to ground that stays. #31 is not decided by this ADR.

**Alternatives considered.**
- A changes facet on contexts. Rejected: it would call reads writes because of bookkeeping, and it mistakes what Changes is for.
- Have each act declare what its context writes. Rejected for the same reason, and it would repeat one fact on every act.
