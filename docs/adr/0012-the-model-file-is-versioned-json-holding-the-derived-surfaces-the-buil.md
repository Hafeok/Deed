# 0012. The model file is versioned JSON holding the derived surfaces; the build tool owns the derivation rules

**Status:** Accepted by Emil, 2026-11-16. Closes issue #34.

**Context.** ADR 0004 put a model file between the build tool and the source generator and left its form open. One place must own the derivation rules, or the tool's report and the generated code can disagree.

**Decision.** The model file is JSON with a version number. The build tool applies every derivation rule and writes the result: each act with its facets resolved; for each act its command path, operands, options, flags, short names, environment variables and exit codes; the read-only, destructive and idempotent hints; the hosts. The source generator only turns the model into code. The file is produced during the build by a target shipped in the package, and is not checked in.

**Consequences.**
- The generator is tested with hand-written model files and has no rules of its own.
- The model is a public format once builds consume it; a breaking change raises its version.
- `deed model` shows an author exactly what will be generated, before any code exists.
- How the target invokes the tool is settled in the generator slice.

**Alternatives considered.**
- Raw facts in the file and derivation in the generator. Rejected: two owners of the rules, one of them on netstandard2.0.
- A checked-in model. Rejected: what can be derived is never declared, and a stale file would be compiled silently.
