# 0013. Outcome kinds are described as SHACL node shapes within a small profile

**Status:** Accepted by Emil, 2026-11-16. Closes issue #22.

**Context.** Every outcome names what it yields, and nothing says what that contains. A generator needs it for handler return types, MCP output schemas and REST responses. Parameters beyond a scalar or a document have the same gap.

**Decision.** An outcome kind is described in the act map by a SHACL node shape, within a profile the build tool can turn into types: a property's datatype, its cardinality, a nested node shape, and an enumeration. The tool writes the result into the model file. A shape outside the profile is reported by the tool, not silently ignored.

**Consequences.**
- Authors describe data with the language they already validate with.
- The profile is a real restriction: most of SHACL is not available for describing kinds.
- Input whose fields depend on a discriminator (issue #29) is to be solved inside this profile, as alternatives keyed on one parameter, once the profile is in use.
- Varve's and product-cli's kinds are still to be described (issue #22 stays open for that).

**Alternatives considered.**
- JSON Schema in the map. Rejected: a second schema language beside SHACL.
- C# types as the source. Rejected: code would declare what the vocabulary should.
