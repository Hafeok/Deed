# 0007. A command path is the resource path then the verb, with one default resource and one default verb

**Status:** Accepted by Emil, 2026-11-16. Closes issue #5.

**Context.** The CLI is derived from the act layer. Varve's commands needed two elisions to derive: six bare verbs (`create`, `query`) and four bare nouns (`info`, `lease`). product-cli's specified commands needed one default resource and no default verb (`docs/findings/2026-11-15-draft-1-varve.md`, `docs/findings/2026-11-16-product-cli.md`).

**Decision.** A command path is the act's resource path, outermost first, followed by its verb. A tool may declare one default resource (`cli:defaultResource`), whose name is left out, and one default verb (`cli:defaultVerb`), which is left out. There is no elision per resource. The build tool refuses a map in which two acts derive the same path.

**Consequences.**
- 13 of Varve's 22 paths and 30 of product-cli's 32 derive.
- Path uniqueness is checked by the tool when it derives, not by a SHACL shape.
- Varve's `apply` and `manage takeover` do not derive; they need a spelling of their own (ADR 0010).

**Alternatives considered.**
- Verb then resource. Rejected: every command in either application that has both is resource first.
- Elision per resource. Rejected: it makes more of Varve derive at the cost of a reader not knowing which noun a bare verb means.
- No elision. Rejected: six of Varve's commands and two of product-cli's would gain a noun nobody types.
