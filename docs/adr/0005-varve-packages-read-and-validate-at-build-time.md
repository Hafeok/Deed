# 0005. Varve's packages read the Turtle and run the shapes, as build-time dependencies

**Status:** Accepted by Emil, 2026-11-16. Settles the library part of issue #3.

**Context.** Deed is for any .NET application and is not tied to Varve's ecosystem (founding decision 11 as revised, issue #2). With ADR 0004 the RDF library is a dependency of the build tool only. The draft shapes use sequence paths, `sh:zeroOrMorePath`, `sh:or`, `sh:not` and three SHACL-SPARQL constraints with message substitution.

**Decision.** The build tool uses `Varve.Turtle`, `Varve.Shacl` and `Varve.Shacl.Sparql` from nuget.org, as ordinary package references. Nothing generated, and nothing a generated application references, depends on Varve.

**Consequences.**
- Deed's tool targets what those packages target (net10.0 today).
- The packages are previews (`0.1.0-preview.*` on 2026-11-16). Versions are pinned, and a breaking change in Varve is Deed's to absorb.
- If a shape needs something Varve.Shacl does not support, that is an issue in Varve or a change to the shape by its own issue, never a silent rewrite.
- The reference results in `tests/fixtures/*.expected.tsv` were produced with pySHACL. A difference between engines is a finding to explain.
- Still open in #3: which constraints Deed keeps for generated code (Native AOT, trimming, minimum target framework).

**Alternatives considered.**
- dotNetRDF. Independent of Varve and established. Not chosen: Emil's call for Varve's packages; no evaluation was made.
- An interface with the engine chosen later. Rejected as indirection before a second engine is wanted.
