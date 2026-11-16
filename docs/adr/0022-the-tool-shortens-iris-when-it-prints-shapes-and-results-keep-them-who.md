# 0022. The tool shortens IRIs when it prints; shapes and results keep them whole

**Status:** Accepted by Emil, 2026-11-16. Closes issue #38.

**Context.** The SHACL-SPARQL messages substitute variables and print full IRIs. The messages are design guidance and should read in the author's words. A state has no name or label in draft 1.

**Decision.** A validation result keeps the full IRI. When the tool prints a message it shortens IRIs using the prefixes the act map declares. No shape changes and no label property is required.

**Consequences.**
- Programs reading results get precise IRIs; people get `x:LeaseState`.
- An IRI with no declared prefix prints whole.
- The expected-results fixtures keep full IRIs.

**Alternatives considered.**
- A label selected in the SPARQL constraint. Rejected: it requires a property every term must then carry, and both act maps would need it on every state.
