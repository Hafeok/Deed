# 0019. A host fails only through its contexts; what it does at startup that returns or writes is an act

**Status:** Accepted by Emil, 2026-11-16. Closes issue #21.

**Context.** ADR 0003 made long-running commands hosts with no facets and left their failures open. Varve's `serve` exits 2 on invalid configuration and hides two acts behind flags. product-cli declares its `mcp` host with an option of its own.

**Decision.** A host has no failures of its own: it fails through the failures of its contexts, with their exit codes. Anything a host does at startup that returns a value or writes state is split out as an act the host may perform (`cli:performs`). The generated `mcp` host is implied, and a tool declares it only to give it settings.

**Consequences.**
- `serve --print-config` is an act; so is recording bootstrap operators.
- An interactive command such as product-cli's `proposal review` is modelled as a host that performs acts. Whether interactive front ends are their own kind is not settled.
- A host that fails after startup reports through its own logging, outside the model.

**Alternatives considered.**
- A failure facet on hosts. Rejected: it gives hosts one facet of seven and invites the rest.
