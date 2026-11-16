# 0029. An act has seven facets, all required

**Status:** Accepted by Emil before 2026-11-15, as one of the decisions the project started from. Recorded 2026-11-16 (issue #2).

**Context.** An operation described only by its inputs and outputs says nothing about who may run it, what it touches or how it fails.

**Decision.** An act states seven facets: actor (a role, never an identity and never a surface), input (each parameter a subject or a modifier), context (what the environment supplies), needs (existing state the act reads), changes (state the act writes), outcome (single or stream), and failures (named and enumerated).

**Consequences.**
- Effect, authorization and error handling can be derived on every surface.
- Writing an act is a design review.

**Alternatives considered.**
- Optional facets with defaults. Rejected: see ADR 0030.
