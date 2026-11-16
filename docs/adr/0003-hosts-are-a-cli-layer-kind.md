# 0003. Long-running commands are hosts: a kind of the CLI binding layer, outside the act layer

**Status:** Accepted by Emil, 2026-11-15.

**Context.** `serve` and `manage watch-kubernetes` run until stopped. They have no outcome and no enumerable failures, and no MCP tool or REST route can offer them. Decision 10 makes the generated MCP server a subcommand, which is the same kind of command (findings Q5).

**Decision.** `cli:Host` in the CLI binding layer declares a command that runs until stopped. A host has a name, takes contexts, and may name the acts it performs with `cli:performs`. A host is not a `deed:Act`, states no facets, and is invisible to MCP and REST.

**Consequences.**
- The act layer keeps one kind. Every act has an outcome.
- Anything a host returns on success is an act and is modelled as one: `serve --print-config` is `configuration show`.
- `feed --follow` is not a host. It is an act whose stream does not end.
- Varve's two hosts are covered: `serve`, and `watch-kubernetes`, which performs `writer takeover`.
- Not settled: how a host's own startup failures and exit codes are declared; whether `serve --record-bootstrap` is split out as an act.

**Alternatives considered.**
- Hosts as acts with an unbounded stream outcome. Rejected: there is nothing in the stream, and the act would be offered on surfaces that cannot run it.
- Leave hosts out of the vocabulary and hand-write them. Rejected: the root command would then be partly hand-written, against the project's premise.
- A separate kind in the act layer. Rejected: no other surface reads it.
