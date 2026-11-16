# 0017. Deed is published on nuget.org, as previews from the validator onward

**Status:** Accepted by Emil, 2026-11-16. Closes issue #35.

**Context.** Emil decided on 2026-11-16 that Deed is published on nuget.org. `Deed` and `Deed.*` were unregistered on that date.

**Decision.** The `Deed.` ID prefix is reserved before the first publish. `Deed.Tool` is published as a preview once the namespace redirect exists (ADR 0016). Versions stay `0.1.0-preview.N` until a generator ships. A release is cut on a tag by a workflow using nuget.org trusted publishing; no API key is stored.

**Consequences.**
- The validator is usable by others before any generator exists.
- The owner account, the prefix reservation and the trusted-publishing policy are set up by the account holder.
- A published tool carries Varve's preview packages as dependencies (ADR 0005); the `Deed` package must not.

**Alternatives considered.**
- Wait for the generator. Rejected: the validator is useful alone for authoring act maps.
- An API key in repository secrets. Rejected: a long-lived secret where none is needed.
