# 0014. Generated code targets net10.0 and is Native AOT and trim compatible

**Status:** Accepted by Emil, 2026-11-16. Closes issue #3.

**Context.** Deed is for any .NET application and is not tied to Varve (ADR 0037). What Deed requires of the code it generates was left open when that was decided. A consumer may publish with Native AOT.

**Decision.** Generated code and Deed's runtime library target net10.0. They are Native AOT and trim compatible and use no reflection. CI publishes a sample application with AOT to prove it. The build tool is not bound by this: it runs at build time (ADR 0004).

**Consequences.**
- An application on an earlier framework cannot use Deed.
- Every dependency of the runtime library must itself be AOT compatible, which constrains the choice of command-line and MCP libraries.
- Dependency direction with Varve is settled: nothing generated references Varve (ADR 0005).

**Alternatives considered.**
- Multi-target earlier frameworks. Rejected: no consumer asks for one, and each added target is tested or it rots.
- AOT as best effort. Rejected: it is lost the first time it is not tested.
