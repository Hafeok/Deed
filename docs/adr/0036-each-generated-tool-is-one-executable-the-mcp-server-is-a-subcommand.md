# 0036. Each generated tool is one executable; the MCP server is a subcommand

**Status:** Accepted by Emil before 2026-11-15, as one of the decisions the project started from. Recorded 2026-11-16 (issue #2).

**Context.** Shipping a CLI and an MCP server as two programs doubles distribution and lets them drift.

**Decision.** Each generated tool is one executable. The MCP server is a subcommand of it, over stdio first.

**Consequences.**
- The MCP subcommand is a host (ADR 0003, ADR 0019).
- HTTP transport comes later.

**Alternatives considered.**
- A separate MCP server binary. Rejected: two artefacts for one act map.
