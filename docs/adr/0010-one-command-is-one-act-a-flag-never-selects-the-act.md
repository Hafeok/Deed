# 0010. One command is one act; a flag never selects the act

**Status:** Accepted by Emil, 2026-11-16. Closes issue #15.

**Context.** Nine of Varve's 22 acts are reached through a flag of another command or an irregular path; `apply` is four acts and `lease` three. product-cli's definition does the same once, before anything is built (`domain export --list-formats`). Facets differ between such acts, and facets are never conditional (`docs/findings/2026-11-15-draft-1-varve.md` F6, `docs/findings/2026-11-16-product-cli.md` P5).

**Decision.** The generator emits exactly one command per act, at the derived path. `cli:spelledAs` records a current spelling that cannot be derived. Validation reports it as a warning. The build tool keeps such an act in the model, since MCP and REST bind to the act and not to the spelling, and derives no CLI command for it.

**Consequences.**
- Adopting Deed changes those spellings in the application, or those commands stay hand-written until it does.
- A parameter required only with another flag disappears: it is simply required on its own act.
- The warning count measures what is left to migrate: Varve 10, product-cli 2.

**Alternatives considered.**
- A term binding a flag of one command to another act. Rejected: it reproduces conditional facets on the surface.
- Aliases, so that an old spelling stays valid. Deferred until a migration needs them.
