# 0037. Deed lives in the .NET ecosystem and is usable by any .NET application

**Status:** Accepted by Emil, 2026-11-16, revising the founding decision of the same number (11). Recorded for issue #2.

**Context.** The project began inside Varve's ecosystem: Varve's target framework, AOT constraints, dependency discipline and RDF packages. Emil revised that on 2026-11-16: the intent was the .NET ecosystem, not Varve's.

**Decision.** Deed is a .NET library usable by any .NET application. It is not tied to Varve. Varve is the first application modelled and the standing sufficiency test.

**Consequences.**
- What replaced the withdrawn parts: the build tool uses Varve's packages at build time only (ADR 0005); generated code has its own constraints (ADR 0014); the licence is Deed's own (ADR 0015).
- A second application, product-cli, was modelled to test the vocabulary away from Varve.

**Alternatives considered.**
- Stay in Varve's ecosystem. Withdrawn by Emil.
