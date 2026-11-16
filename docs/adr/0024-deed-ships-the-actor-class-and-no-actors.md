# 0024. Deed ships the Actor class and no actors

**Status:** Accepted by Emil, 2026-11-16. Closes issue #7.

**Context.** Varve has five roles; product-cli has three; they share none.

**Decision.** Deed defines `deed:Actor` and its shape. Each application names its own roles. Deed defines no order among actors. On-behalf-of needs no term. Confirmation by another actor is not settled here; it is issue #27.

**Consequences.**
- An act that several roles may perform lists each.
- Scope derivation cannot assume that one role includes another.

**Alternatives considered.**
- A starter set (reader, writer, admin). Rejected: neither application's roles fit one.
- A partial order. Deferred until a derivation needs it.
