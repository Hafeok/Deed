# 0015. Apache-2.0, and two published packages: Deed.Tool and Deed

**Status:** Accepted by Emil, 2026-11-16. Closes issue #10.

**Context.** The repository was created with the Apache License 2.0. The working package split was three libraries. With ADR 0004 there is a build tool and a generator with a runtime.

**Decision.** Deed is licensed under Apache-2.0. Two packages are published: `Deed.Tool`, a .NET tool with command `deed` that validates act maps and writes the model file, and `Deed`, the one package an application references, carrying the source generator, the build target that runs the tool, and the runtime library. The vocabulary library stays internal until a consumer needs it.

**Consequences.**
- An application adds one package reference.
- `Deed` carries a net10.0 tool inside the package applications reference. If that proves awkward in a build, the split into separate packages returns.
- Repository layout follows: `src/` per package, `vocabulary/`, `maps/`, `docs/adr/`, `tests/`.

**Alternatives considered.**
- Three or more packages (vocabulary, generator, hosting). Rejected for now: more for a user to assemble, no consumer for the parts.
- MPL 2.0 as Varve. Rejected: Deed is independent of Varve, and generated code must carry no obligation.
