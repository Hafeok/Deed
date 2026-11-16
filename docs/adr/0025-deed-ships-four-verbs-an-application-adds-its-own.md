# 0025. Deed ships four verbs; an application adds its own

**Status:** Accepted by Emil, 2026-11-16. Closes issue #6.

**Context.** Varve uses 15 verbs and product-cli 14. They share `create`, `show`, `list` and `export`. `remove` appears in product-cli and is a common need.

**Decision.** Deed ships `create`, `show`, `list` and `remove`. An application defines any other verb once, as a `deed:Verb` with a name and a meaning that holds on every resource it is used with. `plan` is a verb, not a mode of `apply`.

**Consequences.**
- The list is controlled per application, by the same shape, not closed in Deed.
- An application spells a shipped verb differently with `cli:name` (product-cli's `new`).
- The four are added to the vocabulary and the maps are moved to them as a separate change, since it alters the expected results.

**Alternatives considered.**
- A closed list in Deed. Rejected: most verbs in both applications are about their domain.
- Ship `export` and `apply` too. Deferred: one or two applications is thin evidence for what they mean in general.
