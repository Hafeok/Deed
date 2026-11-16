# 0002. The target is context; the CLI binds it as an operand only by declaration

**Status:** Accepted by Emil, 2026-11-15.

**Context.** The derivation rule makes context a global option with an environment fallback. Varve spells the dataset as an operand in 12 commands, first in 11. The dataset is fixed for a session and is not something the actor chooses per act, so in the act layer it is context: on MCP it is bound at server start and hidden from the schema (findings F1).

**Decision.** The target stays in the Context facet. The rule stands: a setting becomes an option. A tool may bind a setting as an operand by declaring `cli:bindAs cli:Operand` on it in the CLI binding layer. The act layer is unchanged by the binding. Validation reports each `cli:bindAs` at severity Info.

**Consequences.**
- MCP and REST are unaffected: they never read the CLI layer.
- Nine Varve acts derive their current spelling with this one declaration on `dataset`.
- An operand-bound setting comes before the subjects. `setup apply <setup> <dataset>` does not derive.
- Subjects of target-bound acts compete for the operand position. Varve's `--query`, `--update` and `--reader` are subjects bound as options, each its own declared override.
- Not settled: whether a tool may bind more than one setting as an operand, and whether an operand-bound setting keeps an environment fallback.

**Alternatives considered.**
- Change the rule so that a target always binds as the leading operand. Rejected: it needs a target kind of context in the act layer, which no other rule reads, and `--server` already follows the rule as written.
- Model the dataset as a subject parameter. Rejected: every MCP tool would then take the dataset, and the session-bound server of decision 10 would have nothing to bind.
- Require the option spelling and change Varve. Rejected: 12 commands change for no gain in the act layer.
