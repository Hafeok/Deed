# 0011. An option that only a command line needs is declared in the CLI layer

**Status:** Accepted by Emil, 2026-11-16. Closes issue #16.

**Context.** Some options correspond to nothing in the act: where an outcome is saved, how it is signalled, which syntax it is written in. Varve's `--format` is content negotiation, which REST does with `Accept` and MCP does not need. product-cli's `--format` selects an exporter whose output differs in standing and is recorded in provenance; that one is a modifier (`docs/findings/2026-11-15-draft-1-varve.md` F5, `docs/findings/2026-11-16-product-cli.md` P4).

**Decision.** `cli:SurfaceOption` declares an option that exists only because the surface is a command line, and `cli:surfaceOption` attaches it to an act's command. The test is whether another surface would want it: if an agent or an HTTP client would, it is a parameter of the act. A format is CLI-only when it is the same content in another syntax, and a modifier when it selects different content.

**Consequences.**
- MCP and REST never see these options.
- Failures that arise only from such an option are not act failures. Where they are declared is not settled.
- It is an escape hatch; the shape's question is the guard.

**Alternatives considered.**
- Format always a modifier. Rejected: it would appear in MCP schemas beside structured results.
- Format always CLI-only. Rejected by product-cli.
