# 0026. On MCP a stream is paged where it has a cursor, capped where it has none, and left out where it does not end

**Status:** Accepted by Emil, 2026-11-16. Closes issue #8.

**Context.** A stream outcome may be large or endless, and an MCP tool result is finite. Varve's `feed` pages by position and can follow without end; `query` and `export` have no cursor.

**Decision.** On MCP a stream outcome is paginated when the act has a cursor, capped with a truncation marker when it has none, and an act whose stream does not end is not offered.

**Consequences.**
- The act layer must be able to say that a stream has a cursor and that it does not end. Neither term exists; both are added in the MCP slice, against real acts.
- The cap is a setting of the MCP host.

**Alternatives considered.**
- Cap everything. Rejected: it discards a cursor the act already has.
- Offer endless streams with a time limit. Rejected: the result would be arbitrary.
