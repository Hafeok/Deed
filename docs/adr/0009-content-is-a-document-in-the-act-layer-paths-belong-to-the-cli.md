# 0009. Content is a document in the act layer; paths belong to the CLI

**Status:** Accepted by Emil, 2026-11-16. Closes issue #14.

**Context.** Seven of Varve's inputs are file paths. A path means nothing to a remote MCP server or a REST client. `--query` and `--file` are two spellings of one input (`docs/findings/2026-11-15-draft-1-varve.md`, F4).

**Decision.** `deed:Document` is a value kind beside the XSD datatypes. A parameter whose value is the content of a file says `deed:Document`. The CLI binds a document as a path and reads it. A text parameter may declare `cli:fileAlternative`, the option that supplies it from a file; exactly one of the two is given, and the act layer sees one parameter.

**Consequences.**
- "Inline or file, not both" is a usage error found at parse time.
- How MCP and REST receive a document is for their bindings to say.
- A document's media type is not modelled.

**Alternatives considered.**
- A path datatype. Rejected: one surface's syntax in the act layer.
- Two parameters with an exclusion constraint. Rejected: it models a spelling.
