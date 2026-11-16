# 0023. An MCP session is bound to one actor and offers the acts that actor may perform

**Status:** Accepted by Emil, 2026-11-16. Closes issue #28.

**Context.** product-cli requires that the agent surface has no adopt and no reject. That follows without a surface-specific declaration if a session offers exactly the acts whose Actor facet includes the session's role: 19 of its 32 (`docs/findings/2026-11-16-product-cli.md`, P2).

**Decision.** A generated MCP server is started for one actor. It offers the acts that actor may perform and no others. The CLI is not bound to an actor and offers every act.

**Consequences.**
- The Actor facet decides what an agent can see.
- How the session's actor is established (a setting of the host, a credential) is settled in the MCP slice.
- An act only one actor performs still derives a CLI command. Issue #27 covers the case where that is unwanted.

**Alternatives considered.**
- Per-act surface exclusions. Rejected: a surface term in the act layer.
- Bind the CLI to an actor too. Rejected: a person at a terminal holds whatever roles the application grants; the application authorises.
