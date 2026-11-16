# 0008. A context is made of settings and owns the failures of everything that uses it

**Status:** Accepted by Emil, 2026-11-16. Closes issue #13.

**Context.** The Context facet was described as target and credentials. In Varve a credential is seven values, in two different sets, and its command guard handles the same failures for every command (`docs/findings/2026-11-15-draft-1-varve.md`, F3).

**Decision.** `deed:Context` has one or more `deed:Setting` and a failure facet of its own. A setting has a name and a datatype and no role. An act names its contexts and does not repeat their failures. An act's own failure facet may be `deed:Nothing`, except that an act that changes state names at least one failure of its own.

**Consequences.**
- A read such as Varve's `info` has no failure of its own, truthfully.
- A setting and a parameter cannot share a name.
- One context composes across acts and hosts.
- A context declares no changes (ADR 0006).

**Alternatives considered.**
- Settings as parameters with a third role. Rejected: a role says how the actor supplies a value, and the actor does not supply these.
- Failures only on acts. Rejected: 14 of Varve's acts would repeat the same six.
