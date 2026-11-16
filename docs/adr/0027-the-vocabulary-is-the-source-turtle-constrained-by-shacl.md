# 0027. The vocabulary is the source: Turtle constrained by SHACL

**Status:** Accepted by Emil before 2026-11-15, as one of the decisions the project started from. Recorded 2026-11-16 (issue #2).

**Context.** Applications repeatedly need a CLI and an MCP server with the same functionality. The usual sources of truth are code attributes or one surface's own definitions.

**Decision.** The authored source of truth is Turtle, constrained by SHACL. C# handlers implement interfaces generated from it. C# attributes are not the source.

**Consequences.**
- An act map can be written, validated and reviewed before any code exists.
- Authors need to read and write Turtle.

**Alternatives considered.**
- Attributes on C# methods, as Manifold does. Rejected: the code would declare what the vocabulary should, and semantics beyond a signature have nowhere to live.
