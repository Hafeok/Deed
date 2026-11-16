# Second act map: product-cli, from its product definition

2026-11-16. Source: `mindovermachine-dev/decision-driven-design` at `84aa466`, `applications/sdlc/product-cli/product-definition.md` (Draft 4), chapters 1 to 6. Vocabulary: draft 1, unchanged. Map: `maps/product-cli/product-acts.ttl`. Tracks #24.

This is a different test from Varve. The tool has no verbs yet (`mindovermachine-dev/product-cli` at `528a1f8` answers only `--version`), so the map was written forward from a specification. It tests the shapes as a design instrument and the rules against a specified command table, not against code.

No vocabulary term was added or changed. Section 3 lists what the map could not say.

## 1. Result

The definition's 16 table rows are 32 acts and 3 host commands. All validate with no violations: 2 warnings, 4 infos.

| How the specified spelling is reached | Acts |
|---|---|
| Path derived by rule alone | 20 |
| Path derived with a name override | 10 |
| Not derivable | 1 (`domain export --list-formats`) |
| No spelling in the definition | 1 (the agent's proposal, see P1) |

Compared with Varve (1 by rule, 9 with overrides, 12 not): a command line designed noun-then-verb from the start derives almost entirely.

- One default resource (`draft`: `init`, `diff`). No default verb was needed.
- `--product` is context as an option, by the rule, with no operand binding. ADR 0002's exception was not used.
- Overrides: `domain` names two resources (the What, and a node of it); `create` is spelled `new`; `remove` is spelled `rm`.
- Every operand the table states is a subject in declared order, every option a modifier, every flag a boolean modifier. No subject is bound as an option.

## 2. What the shapes exposed in the definition

Writing each act forced an answer for every facet. Where the definition gave none, the map guesses and marks the comment "Assumed:". Each is a question for the definition.

| Kind | Total | Assumed |
|---|---|---|
| Failures | 22 | 13 |
| Parameters | 19 | 5 |
| Settings | 3 | 1 |

- **Failures of writes.** The definition states what is refused by guarantee (W1, W2, W5, W9, a stale proposal, an unknown format). It names no failure for `init` on an initialised repository, for a product that already exists, for an unknown node, proposal or position, or for an answered question. Four writes (`init`, `decider derive`, `projector derive`, `question answer`) have only assumed failures.
- **`domain rm` against W1.** Removal takes a node and its links; W1 refuses a change set with a violation. The definition does not say which wins when removing leaves a violation.
- **`why` on direct writes.** Every record has a non-empty `why` (W11). The table shows `--why` on `adopt` only. What an author's direct write records as its reason is not stated.
- **`export` writes.** By D1.17 each export is a new What-version that supersedes the last and records who exported and why. So `export` changes state and takes a reason. The table shows neither.
- **`--product` on verbs that have no product yet.** "Every verb takes `--product`" cannot hold for `init`, and is odd for `product new` and `product list`.
- **Operands.** `domain show|list|rm`, `scenario new|list|run`, `question new|answer` and `proposal show|reject` have no stated operands. The fields of `domain new <kind> <id> …` are an ellipsis.
- **Acts the guarantees imply and no verb performs.** Import (W8 round-trips an import), undo (named with `diff` as a read over the log), and advancing a session's phase (open questions gate it).
- **`--canonical`** is the default restated as a flag beside `--format`.
- **Exit codes** are not specified for any verb. `domain validate` in particular: whether violations are a failure or an outcome is the same question as Varve's `lease` (#19).

## 3. Findings about the vocabulary

**P1. The same verb is a different act depending on who performs it.** The definition's central rule: an author's write is a decision at the head; the same verb called by an agent creates a proposal, held beside the head, requiring a reason, and a different actor must adopt it (W12). Facets cannot vary by actor, so the map has the author's write acts (author only) and one separate act, `proposal create`, standing for every agent write. That loses what the definition asks for: the agent calls `domain new`, not a generic submit. The vocabulary has no way to say "this actor's writes are proposals that another actor confirms". This is the confirmation and on-behalf-of question (#7), which Varve gave no reason to answer. Now there is one. Candidates, none adopted:
- a declaration on an actor that its writes are mediated: each write act is offered to it as a proposal of that act, with a named act by another actor that confirms;
- two acts per verb, with a way to give both one name on different surfaces;
- leave it to the application, and accept that MCP tools differ from CLI commands.

**P2. A surface session is bound to an actor.** "There is no adopt on the agent surface" follows if an MCP session is bound to the agent role and offers exactly the acts that role may perform: 19 of 32 here, and `adopt` and `reject` are not among them. That is a derivation rule worth stating, and it makes the Actor facet do real work on MCP. It also needs the converse, which does not exist: `proposal create` derives a CLI command the definition does not have.

**P3. Input that depends on a discriminator.** `domain new <kind> <id> …` takes fields that differ by kind, for 17 kinds. The shapes' own advice is that a parameter required only with another means two acts; here that is 17 acts and 17 resources, spelled `domain new <kind>`, verb before kind. Modelled as one act with an opaque repeatable `property`. An MCP schema for it is a discriminated union, which the vocabulary cannot describe. Connected to describing outcome kinds (#22).

**P4. Format is not always surface syntax.** Proposed ADR P5 (#16) puts `--format` in the CLI layer, on Varve's evidence that it is content negotiation. In product-cli the format selects an exporter, only the canonical one is landable, and the choice is recorded in the export's provenance. It is a modifier here and is modelled as one. The test in #16's shape still works ("would an agent want it too?": yes), so the proposal stands, but its wording should not claim format as the example.

**P5. A flag selects the act, in a specification.** `domain export --list-formats` is `format list` behind a flag: proposed ADR P4 (#15) applies to a design that is not yet built.

**P6. Interactive commands.** `proposal review` steps through proposals with single keys until quit. It has no outcome and cannot be an MCP tool; it performs `adopt` and `reject`. `cli:Host` fits ("runs until stopped", `cli:performs`) but was defined for servers. Either the term widens to interactive front ends or there are two kinds (#21).

**P7. Acts that reach outside the tool.** `conformance run <runner>` starts an external program. It changes no state the tool owns, so it derives read-only, and nothing derives MCP's open-world hint. Varve's `SERVICE` and `LOAD` have the same property. No facet says "reaches something the tool does not own".

**P8. Removal in a log is an append.** `domain rm` writes a new position; the node stays in the history. It derives non-destructive, the same doubt ADR 0001 recorded for Varve's `update`. Two event-sourced applications now show it: the manner describes the log, and "destructive" is a claim about the current view.

**P9. An implicit context.** The repository is the working directory and has no setting. A context must have at least one setting, so it is folded into the product target's description.

## 4. Evidence for open issues

- **#5 ordering and elision.** Resource then verb holds for all 30 specified paths. One default resource; no default verb. Per-resource elision was not needed.
- **#6 verbs.** 14 verbs: `init`, `create`, `show`, `list`, `remove`, `validate`, `export`, `derive`, `simulate`, `run`, `answer`, `diff`, `adopt`, `reject`. Shared with Varve: `create`, `show`, `list`, `export`. `apply` is not used; `remove` and `validate` are. `new` is not a verb, and it is this application's consistent spelling of `create`.
- **#7 actors.** Three roles, none Varve's: author, agent, realiser. Supports shipping no actors. Contradicts "no term for confirmation or on-behalf-of" (P1): the agent's record is `by` the agent and `attributedTo` the author.
- **#8 streams.** 18 stream outcomes, all finite. Nothing new.
- **#20 operand-bound settings.** Not used: the target is an option.
- **#21 hosts.** Three, including a declared `mcp` with its own option (`--http`), so the generated MCP host must be declarable, not only implied.

## 5. Validation

pySHACL 0.40.2, as for draft 1 (#12 still applies). Data: `deed.ttl` + `deed-cli.ttl` + `product-acts.ttl`. Counts: 32 acts, 12 resources, 14 verbs, 3 actors, 19 parameters, 3 settings, 3 contexts, 10 states, 22 failures.

| Severity | Count | What |
|---|---|---|
| Violation | 0 | |
| Warning | 2 | `cli:spelledAs` on `ListFormats` and on the `review` host |
| Info | 4 | `cli:name` on `What`, `Node`, `create`, `remove` |

The pass is clean because the assumed values were written to make it so. Without them, 4 acts would violate "this act changes state but declares no failure of its own". The count of assumed values in section 2 is the honest measure.

Paths and effects were computed from the Turtle by a throwaway script and compared with the definition's command table by hand.
