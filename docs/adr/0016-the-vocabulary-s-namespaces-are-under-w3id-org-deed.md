# 0016. The vocabulary's namespaces are under w3id.org/deed

**Status:** Accepted by Emil, 2026-11-16. Closes issue #4.

**Context.** Draft 1 used working IRIs. Publishing a package makes them permanent.

**Decision.** The act vocabulary is `https://w3id.org/deed/ns#` with prefix `deed:`. The CLI binding vocabulary is `https://w3id.org/deed/cli#` with prefix `cli:`. The shapes are `https://w3id.org/deed/shapes#`. An application's acts live in the application's own namespace.

**Consequences.**
- A redirect must be registered at w3id.org before the first publish.
- The IRIs do not change after publication; a breaking vocabulary is a new namespace.
- The placeholder namespaces in the two act maps are the applications' to replace.

**Alternatives considered.**
- A domain Deed owns. Rejected: w3id outlives a domain registration.
- Slash namespaces. Rejected: the vocabulary is small and fetched whole.
